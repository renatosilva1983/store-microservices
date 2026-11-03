variable "region" {
  description = "Região da AWS. us-east-1: mais barata e exigida pelo certificado do CloudFront."
  type        = string
  default     = "us-east-1"
}
