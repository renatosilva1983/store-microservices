# Um repositório de imagens por microsserviço (ADR-26 e ADR-37): cada serviço é publicado
# sozinho, com versão própria.

resource "aws_ecr_repository" "service" {
  for_each = var.services

  name = "${var.project}/${each.key}"

  # Tag imutável: a tag é o SHA do commit e nunca é sobrescrita. Sabe-se exatamente
  # qual código está rodando, e o rollback é apontar para a tag anterior.
  image_tag_mutability = "IMMUTABLE"

  # Procura vulnerabilidades conhecidas a cada push (scan básico, sem custo).
  image_scanning_configuration {
    scan_on_push = true
  }

  # Ambiente de demonstração: o destroy apaga o repositório mesmo com imagens.
  # Em produção ficaria false, para não perder imagens por engano.
  force_delete = true
}

# Limpeza automática: o ECR cobra por GB armazenado.
resource "aws_ecr_lifecycle_policy" "service" {
  for_each = aws_ecr_repository.service

  repository = each.value.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Imagens sem tag expiram em 1 dia"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = 1
        }
        action = { type = "expire" }
      },
      {
        rulePriority = 2
        description  = "Mantém só as 10 imagens mais recentes"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = 10
        }
        action = { type = "expire" }
      }
    ]
  })
}
