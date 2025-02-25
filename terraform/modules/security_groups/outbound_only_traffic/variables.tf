variable "vpc_id" {
  description = "Id of VPC that's within AWS account being deployed to."
  type = string
}

variable "user_resource_name" {
  description = "Name of the resource that's going to use the security group."
  type = string
}

variable "environment_name" {
  description = "development/staging/production"
  type = string
}
