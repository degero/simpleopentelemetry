terraform {
  required_version = ">= 1.6.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 6.0"
    }
  }
}

provider "google" {
  project = var.gcp_project_id
  region  = var.gcp_region
}

locals {
  telemetry_apis = [
    "run.googleapis.com",
    "telemetry.googleapis.com",
    "secretmanager.googleapis.com",
    "logging.googleapis.com",
    "monitoring.googleapis.com",
    "cloudtrace.googleapis.com",
    "iam.googleapis.com",
    "serviceusage.googleapis.com",
  ]
}

resource "google_project_service" "required" {
  for_each           = toset(local.telemetry_apis)
  project            = var.gcp_project_id
  service            = each.value
  disable_on_destroy = false
}

resource "google_service_account" "cloud_run" {
  project      = var.gcp_project_id
  account_id   = "${var.gcp_service_name}-sa"
  display_name = "${var.gcp_service_name} Cloud Run service account"
}

resource "google_project_iam_member" "log_writer" {
  project = var.gcp_project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Legacy metric export role
resource "google_project_iam_member" "monitoring_metric_writer" {
  project = var.gcp_project_id
  role    = "roles/monitoring.metricWriter"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Otlp endpoint metric export role
resource "google_project_iam_member" "telemetry_metrics_writer" {
  project = var.gcp_project_id
  role    = "roles/telemetry.metricsWriter"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Legacy trace export role
resource "google_project_iam_member" "trace_agent" {
  project = var.gcp_project_id
  role    = "roles/cloudtrace.agent"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_project_iam_member" "telemetry_traces_writer" {
  project = var.gcp_project_id
  role    = "roles/telemetry.tracesWriter"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_project_iam_member" "secret_accessor" {
  project = var.gcp_project_id
  role    = "roles/secretmanager.secretAccessor"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Reduce cloud run logging /traces as we have this in app otel telemetry
resource "google_logging_project_exclusion" "cloud_run_request_logs" {
  count       = var.exclude_cloudrun_logs ? 1 : 0
  name        = "exclude-cloud-run-request-logs"
  description = "Cloud Run's auto-generated request logs; app sends its own request telemetry via OTLP"
  project     = var.gcp_project_id

  filter = "resource.type=\"cloud_run_revision\" AND logName=\"projects/${var.gcp_project_id}/logs/run.googleapis.com%2Frequests\""
}

# sleep so container doesnt start until iam rules propagate
resource "time_sleep" "wait_for_iam" {
  depends_on      = [
    google_project_iam_member.log_writer,
    google_project_iam_member.monitoring_metric_writer,
    google_project_iam_member.telemetry_metrics_writer,
    google_project_iam_member.trace_agent,
    google_project_iam_member.telemetry_traces_writer,
    google_project_iam_member.secret_accessor
  ]
  create_duration = "30s"
}

resource "google_cloud_run_v2_service" "app" {
  name                = var.gcp_service_name
  location            = var.gcp_region
  ingress             = "INGRESS_TRAFFIC_ALL"
  deletion_protection = false

  template {
    service_account = google_service_account.cloud_run.email

    scaling {
      min_instance_count = 0
      max_instance_count = 1
    }

    dynamic "volumes" {
      for_each = var.use_otel_sidecar ? [1] : []
      content {
        name = "otel-config"
        secret {
          secret = google_secret_manager_secret.collector_config[0].secret_id
          items {
            version = "latest"
            path    = "otel-collector-config.yaml"
          }
        }
      }
    }

    containers {
      name  = "app"
      image = var.app_image
      depends_on = var.use_otel_sidecar ? ["otel"] : []
      ports {
        container_port = 80
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:80"
      }
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "OTEL_EXPORTER_OTLP_ENDPOINT"
        value = var.otel_endpoint
      }
      env {
        name  = "OTEL_EXPORTER_OTLP_PROTOCOL"
        value = var.otel_protocol
      }
      env {
        name  = "OTEL_SERVICE_NAME"
        value = var.gcp_service_name
      }
      env {
        name  = "GOOGLE_CLOUD_PROJECT"
        value = var.gcp_project_id
      }
      env {
        name  = "OTEL_RESOURCE_ATTRIBUTES"
        value = var.otel_resource_attributes
      }
      env {
        name  = "GOOGLE_CLOUD_LOG_NAME"
        value = var.gcp_log_name
      }
      env {
        name  = "EnableOtelEventListeners"
        value = "${var.enable_otel_event_listeners}"
      }
      env {
        name  = "IgnoreInboundTraceRules"
        value = "${var.ignore_cloudrun_trace_sampling}"
      }

      resources {
        cpu_idle = true
        limits = {
          cpu    = var.app_cpu
          memory = var.app_memory
        }
      }

      startup_probe {
        initial_delay_seconds = 5
        timeout_seconds       = 30
        period_seconds        = 30
        # failure_threshold     = 3
        http_get {
          path = "/health"
          port = 80
        }
      }

      liveness_probe {
        timeout_seconds   = 30
        period_seconds    = 30
        # failure_threshold = 3
        http_get {
          path = "/health"
          port = 80
        }
      }

    }

    dynamic "containers" {
      for_each = var.use_otel_sidecar ? [1] : []
      content {
        name  = "otel"
        image = var.otel_image
        args  = ["--config=/etc/otel/otel-collector-config.yaml"]

        env {
          name  = "GOOGLE_CLOUD_PROJECT"
          value = var.gcp_project_id
        }
        env {
          name  = "GOOGLE_CLOUD_LOG_NAME"
          value = var.gcp_log_name
        }
        volume_mounts {
          name       = "otel-config"
          mount_path = "/etc/otel"
        }

        resources {
          cpu_idle = true
          limits = {
            cpu    = var.sidecar_cpu
            memory = var.sidecar_memory
          }
        }

        startup_probe {
          initial_delay_seconds = 5
          timeout_seconds       = 30
          period_seconds        = 10
          # failure_threshold     = 3
          http_get {
            path = "/"
            port = 13133
          }
        }

        liveness_probe {
          timeout_seconds   = 30
          period_seconds    = 30
          failure_threshold = 3
          http_get {
            path = "/"
            port = 13133
          }
        }
      }
    }
  }

  depends_on = [
    time_sleep.wait_for_iam,
    google_project_service.required,
    google_secret_manager_secret_version.collector_config
  ]
}

resource "google_cloud_run_v2_service_iam_member" "public_invoker" {
  location = google_cloud_run_v2_service.app.location
  name     = google_cloud_run_v2_service.app.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}

resource "google_secret_manager_secret" "collector_config" {
  count     = var.use_otel_sidecar ? 1 : 0
  project   = var.gcp_project_id
  secret_id = "${var.gcp_service_name}-collector-config"

  replication {
    auto {}
  }

  depends_on = [google_project_service.required]
}

resource "google_secret_manager_secret_version" "collector_config" {
  count       = var.use_otel_sidecar ? 1 : 0
  secret      = google_secret_manager_secret.collector_config[0].id
  secret_data = file("${path.module}/otel-collector-config.yaml")
}
