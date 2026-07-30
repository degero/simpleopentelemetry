variable "region" {}
variable "image" {}
variable "enable_client_xray_sampler" {}
variable "enable_otel_event_listeners" {}

variable "app_name" {
  default = "soteltestaws"
}

variable "sidecar_container_name" {
  default = "otel-collector"
}

variable "sidecar_image" {
  default = "amazon/aws-otel-collector:latest"
}

variable "log_retention_days" {
  type    = number
  default = 1
}

variable "metric_retention_days" {
  type    = number
  default = 1
}
variable "app_cpu" {
  type        = number
  default     = 512
}

variable "app_memory_mb" {
  type        = number
  default     = 640
}

variable "app_memory_reserve_mb" {
  type        = number
  default     = 512
}

variable "sidecar_cpu" {
  type        = number
  default     = 256
}

variable "sidecar_memory_mb" {
  type        = number
  default     = 256
}

variable "sidecar_memory_reserve_mb" {
  type        = number
  default     = 128
}


variable "collector_go_memory_limit" {
  default     = "112MiB"
}

variable "collector_memory_mb" {
  type        = number
  default     = 140
}

variable "collector_spike_memory_mb" {
  type        = number
  default     = 28
}