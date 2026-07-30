output "public_ip" {
  value = "http://${aws_instance.ecs_host.public_ip}"
}