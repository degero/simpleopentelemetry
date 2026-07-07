variable "gcp_project_id" {
  type = string
}

variable "gcp_region" {
  type    = string
  default = "us-east1"
}

variable "gcp_service_name" {
  type    = string
  default = "soteltestgcp"
}

variable "gcp_log_name" {
  type    = string
  default = "otlp"
}

variable "use_otel_sidecar" {
  type    = bool
  default = true
}

variable "enable_otel_event_listeners" {
  type    = bool
  default = false
}

variable "ignore_cloudrun_trace_sampling" {
  type    = bool
  default = false
}

variable "app_image" {
  type = string
}

variable "otel_image" {
  type    = string
  default = "us-docker.pkg.dev/cloud-ops-agents-artifacts/google-cloud-opentelemetry-collector/otelcol-google:0.151.0"
}

variable "otel_endpoint" {
  type    = string
  default = "http://localhost:4317"
}

variable "otel_protocol" {
  type    = string
  default = "http/protobuf"
}

variable "otel_resource_attributes" {
  type    = string
}

variable "exclude_cloudrun_logs" {
  type   = bool
  default  = true
}

variable "app_cpu" {
  type    = string
  default = "0.5"
}

variable "app_memory" {
  type    = string
  default = "512Mi"
}

variable "sidecar_cpu" {
  type    = string
  default = "0.25"
}

variable "sidecar_memory" {
  type    = string
  default = "256Mi"
}
