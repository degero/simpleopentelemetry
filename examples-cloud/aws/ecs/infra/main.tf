provider "aws" {
  region = var.region
}

# --- AMI ---
data "aws_ssm_parameter" "ecs_ami" {
  name = "/aws/service/ecs/optimized-ami/amazon-linux-2023/recommended/image_id"
}

# --- Networking ---
resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = { Name = "${var.app_name}-vpc" }
}

resource "aws_internet_gateway" "igw" {
  vpc_id = aws_vpc.main.id
  tags   = { Name = "${var.app_name}-igw" }
}

resource "aws_subnet" "public" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.1.0/24"
  map_public_ip_on_launch = true
  availability_zone       = "${var.region}a"

  tags = { Name = "${var.app_name}-public-subnet" }
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.igw.id
  }

  tags = { Name = "${var.app_name}-public-rt" }
}

resource "aws_route_table_association" "public" {
  subnet_id      = aws_subnet.public.id
  route_table_id = aws_route_table.public.id
}

resource "aws_security_group" "app-sg" {
  name   = "${var.app_name}-sg"
  vpc_id = resource.aws_vpc.main.id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  ingress {
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# --- IAM ---
resource "aws_iam_role" "ecs_instance" {
  name = "${var.app_name}-ecs-instance-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })
  
}

resource "aws_iam_role_policy" "ecs_task_cloudwatch_otel_policy" {
  name   = "${var.app_name}-ecs-task-cloudwatch-otel-policy"
  role   = aws_iam_role.ecs_instance.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "cloudwatch:PutMetricData",
          "logs:PutLogEvents",
          "logs:CreateLogStream",
          "logs:CreateLogGroup",
          "xray:PutTraceSegments",
          "xray:PutTelemetryRecords",
          "xray:GetSamplingRules",
          "xray:GetSamplingTargets",
          "xray:GetSamplingStatisticSummaries"
        ]
        Resource = "*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_instance" {
  role       = aws_iam_role.ecs_instance.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonEC2ContainerServiceforEC2Role"
}

resource "aws_iam_instance_profile" "ecs_instance" {
  name = "${var.app_name}-ecs-instance-profile"
  role = aws_iam_role.ecs_instance.name
}

# --- ECS Cluster ---
resource "aws_ecs_cluster" "cluster" {
  name = "${var.app_name}-cluster"
}

# resource "aws_subnet" "public" {
#   vpc_id                  = aws_vpc.main.id
#   cidr_block              = "10.0.1.0/24"
#   availability_zone       = "${var.region}a"
#   map_public_ip_on_launch = true

#   tags = {
#     Name = "${var.project_name}-public-subnet-1"
#   }
# }

# --- Find subnet in ap-southeast-1b ---
# data "aws_subnet" "selected" {
#   filter {
#     name   = "availabilityZone"
#     values = ["$regionb"]
#   }
#   filter {
#     name   = "vpc-id"
#     values = [data.aws_vpc.default.id]
#   }
# }

# --- EC2 Instance
resource "aws_instance" "ecs_host" {
  ami                         = data.aws_ssm_parameter.ecs_ami.value
  instance_type               = "t3.micro"
  iam_instance_profile        = aws_iam_instance_profile.ecs_instance.name
  vpc_security_group_ids      = [aws_security_group.app-sg.id]
  subnet_id                   = resource.aws_subnet.public.id 
  associate_public_ip_address = true

  # This is in place due to SPOT restrictions
  user_data_replace_on_change = true

  instance_market_options {
    market_type = "spot"

    spot_options {
      max_price          = "0.008" # regular is approx ~0.01/hr
      spot_instance_type = "one-time"
    }
  }

  user_data_base64 = base64encode(<<-EOF
    #!/bin/bash
    echo ECS_CLUSTER=${aws_ecs_cluster.cluster.name} >> /etc/ecs/ecs.config
    echo ECS_ENABLE_CONTAINER_METADATA=true >> /etc/ecs/ecs.config
  
    mkdir -p /opt/otel
    cat > /opt/otel/config.yaml << 'OTELEOF'
    ${file("./adotcollector-config.yml")}
    OTELEOF

  EOF
  )

  tags = { Name = "${var.app_name}-ecs-host" }
}

resource "aws_cloudwatch_log_group" "app" {
  name              = "/app/${var.app_name}/dev"
  retention_in_days = var.log_retention_days
  tags = {
    "cw:datasource:name" = "${var.app_name}"
    "cw:datasource:type" = "application_logs"
  }
}

resource "aws_cloudwatch_log_stream" "app" {
  name           = "app"
  log_group_name = aws_cloudwatch_log_group.app.name
}

locals {
  container_defs = [
    {
      name      = "${var.app_name}-aspnetcore"
      image     = var.image
      essential = true
      portMappings = [{
        containerPort = 80
        hostPort      = 80
        protocol      = "tcp"
      }]
      environment = [
        { name = "UseXraySampler",                value = "${var.enable_client_xray_sampler}" },
        { name = "EnableOtelEventListeners",      value = "${var.enable_otel_event_listeners}" },
        { name = "OTEL_EXPORTER_OTLP_ENDPOINT",   value = "http://localhost:4317"},
        { name = "AWS_LOG_STREAM_NAME",           value = "app" },
        { name = "ASPNETCORE_URLS",               value = "http://+:80" },
        { name = "ASPNETCORE_ENVIRONMENT",        value = "Production" },
        { name = "ECS_ENABLE_CONTAINER_METADATA", value = "true" }
      ]
      logConfiguration = { # Remove if you only want logs from the app code not ecs host
          logDriver = "awslogs"
          options = {
            "awslogs-group"         = aws_cloudwatch_log_group.app.name
            "awslogs-region"        = var.region
            "awslogs-stream-prefix" = "ecs"
            "awslogs-create-group"  = "True"
          }
        }
      cpu                = var.app_cpu
      memory             = var.app_memory_mb
      memoryReservation  = var.app_memory_reserve_mb
      healthCheck = {
        Command: ["CMD-SHELL", "curl -f http://localhost:80/health || exit 1"]
        Interval: 5
        Retries: 5
        Timeout: 3
        startPeriod: 20
      }
    },
    {
      name      = var.sidecar_container_name
      image     = var.sidecar_image
      essential = true
      portMappings = [
        {
          containerPort = 4317
          hostPort      = 4317
          protocol      = "tcp"
        },
        {
          containerPort = 2000
          hostPort      = 2000
          protocol      = "tcp"
        }
      ]
      environment = [
        { name = "AWS_REGION",        value = var.region },
        { name = "COLLECTOR_MEM_LIMIT_MB", value = tostring(var.collector_memory_mb) },
        { name = "COLLECTOR_SPIKE_LIMIT_MB", value = tostring(var.collector_spike_memory_mb) },
        { name = "GOMEMLIMIT"         , value = var.collector_go_memory_limit },
        { name = "LOG_GROUP_NAME",    value = aws_cloudwatch_log_group.app.name },
        { name = "LOG_GROUP_STREAM",  value = aws_cloudwatch_log_stream.app.name },
        { name = "LOG_RETENTION_DAYS",  value = tostring(var.log_retention_days) },
        { name = "METRIC_RETENTION_DAYS",  value = tostring(var.metric_retention_days) }
      ]
      logConfiguration = { # Remove if you dont want adot collector logs 
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.app.name
          "awslogs-region"        = var.region
          "awslogs-stream-prefix" = "ecs-otel"
          "awslogs-create-group"  = "True"
        }
      }
      command   = ["--config", "/etc/otel/config.yaml"]
      mountPoints = [{
        sourceVolume  = "otel-config"
        containerPath = "/etc/otel"
        readOnly      = true
      }]
      cpu                = var.sidecar_cpu
      memory             = var.sidecar_memory_mb
      memoryReservation  = var.sidecar_memory_reserve_mb
      healthCheck = {
        Command: ["CMD", "/healthcheck"]
        Interval: 5
        Retries: 5
        Timeout: 3
        startPeriod: 10
      }
    }
   ]
}

# --- ECS Task Definition ---
resource "aws_ecs_task_definition" "app" {
  family                   = "${var.app_name}-task"
  network_mode             =  "host"
  requires_compatibilities = ["EC2"]

  # cpu                      = "1024"
  # memory                   = "768"

  volume {
    name = "otel-config"
    host_path = "/opt/otel"
  }

  container_definitions = jsonencode(local.container_defs)

  # container_definitions = jsonencode([{
  #   name      = "${var.app_name}-aspnetcore"
  #   image     = var.image
  #   essential = true
  #   portMappings = [{
  #     containerPort = 80
  #     hostPort      = 80
  #     protocol      = "tcp"
  #   }]
  #   environment = [
  #     { name = "AWS_LOG_STREAM_NAME",          value = "app" },
  #     { name = "ASPNETCORE_URLS",              value = "http://+:80" },
  #     { name = "ASPNETCORE_ENVIRONMENT",       value = "Production" },
  #     { name = "ECS_ENABLE_CONTAINER_METADATA", value = "true" }
  #   ]
  #   logConfiguration = {
  #       logDriver = "awslogs"
  #       options = {
  #         "awslogs-group"         = aws_cloudwatch_log_group.app.name
  #         "awslogs-region"        = var.region
  #         "awslogs-stream-prefix" = "ecs"
  #       }
  #     }

  # }])
}

# --- ECS Service ---
resource "aws_ecs_service" "ecs-service" {
  name            = "${var.app_name}-service"
  cluster         = aws_ecs_cluster.cluster.id
  task_definition = aws_ecs_task_definition.app.arn
  desired_count   = 1
  launch_type     = "EC2"

  # this is DEFINITELY NOT recommended for PRODUCTION - it is just a cost saving to only have one t3.micro instance
  # on updated task definition deployment
  deployment_minimum_healthy_percent = 0    # allow 0 running tasks momentarily
  deployment_maximum_percent         = 100  # never try to run 2 copies at once
  availability_zone_rebalancing      = "DISABLED"
  # END
  
  depends_on = [aws_instance.ecs_host]
}

output "instance_public_ip" {
  description = "Public IP of the ECS host – reach your container on this address"
  value       = aws_instance.ecs_host.public_ip
}

output "app_url" {
  description = "App endpoint"
  value       = "http://${aws_instance.ecs_host.public_ip}:80"
}