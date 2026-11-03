locals {
  name = "${var.project}-${var.environment}"

  # Duas zonas de disponibilidade: o ALB e o grupo de subnets do RDS exigem no mínimo duas.
  azs = slice(data.aws_availability_zones.available.names, 0, 2)
}

data "aws_availability_zones" "available" {
  state = "available"
}

data "aws_caller_identity" "current" {}
