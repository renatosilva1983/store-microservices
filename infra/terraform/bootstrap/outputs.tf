output "state_bucket" {
  description = "Bucket do state remoto. Use em infra/terraform/aws/backend.tf."
  value       = aws_s3_bucket.state.bucket
}
