resource "aws_security_group" "outbound_traffic_sg" {
  vpc_id = var.vpc_id
  name_prefix = "${replace(var.user_resource_name, "/\\s+|-/", "_")}_outgoing_traffic"
  description = "SG used to hook ${replace(var.user_resource_name, "/_|-/", " ")} lambda into VPC. No incoming traffic allowed, all outgoing traffic allowed."

  egress {
    description = "allow outbound traffic"
    from_port = 0
    to_port   = 0
    protocol  = "-1"

    cidr_blocks = ["0.0.0.0/0"]
  }

  # No ingress - listener does not listen to incoming traffic

  tags = {
    Name = "${replace(var.user_resource_name, "/\\s+|-/", "_")}-${var.environment_name}"
  }
}
