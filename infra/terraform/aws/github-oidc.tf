# Deploy pelo GitHub Actions sem chave de acesso guardada (ADR-31 e ADR-38).
#
# Fluxo do OIDC:
#   1. O GitHub Actions emite um token assinado dizendo "sou o repositório X, na branch Y".
#   2. O pipeline troca esse token por credenciais temporárias da AWS (sts:AssumeRoleWithWebIdentity).
#   3. A AWS só aceita se o token vier do provedor confiável e as condições baterem.
#   As credenciais valem por 1 hora. Não existe segredo da AWS no GitHub para vazar.

# O provedor OIDC do GitHub é um só por conta e já existe (criado no FleetExecutive).
# Por isso é lido (data source), não criado: dois projetos o compartilham, e nenhum
# deles deve apagá-lo num destroy.
data "aws_iam_openid_connect_provider" "github" {
  url = "https://token.actions.githubusercontent.com"
}

data "aws_iam_policy_document" "github_trust" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [data.aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # StringEquals, não StringLike com curinga: só este repositório, só esta branch.
    # Um fork ou outra branch recebe "acesso negado".
    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:sub"
      values   = ["repo:${var.github_repository}:ref:refs/heads/${var.github_deploy_branch}"]
    }
  }
}

resource "aws_iam_role" "github_deploy" {
  name                 = "gha-${var.project}-deploy"
  description          = "Assumida via OIDC pelo GitHub Actions para publicar o store-microservices."
  assume_role_policy   = data.aws_iam_policy_document.github_trust.json
  max_session_duration = 3600
}

# Menor privilégio: por enquanto, só enviar imagens para os repositórios do projeto.
# As permissões de ECS (atualizar serviço) e S3/CloudFront (publicar o front) entram
# junto com esses recursos.
data "aws_iam_policy_document" "github_deploy" {
  # O login no ECR não aceita restrição por recurso.
  statement {
    sid       = "EcrLogin"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid = "EcrPush"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:BatchGetImage",
      "ecr:CompleteLayerUpload",
      "ecr:GetDownloadUrlForLayer",
      "ecr:InitiateLayerUpload",
      "ecr:PutImage",
      "ecr:UploadLayerPart",
    ]
    resources = [for repo in aws_ecr_repository.service : repo.arn]
  }
}

resource "aws_iam_role_policy" "github_deploy" {
  name   = "deploy"
  role   = aws_iam_role.github_deploy.id
  policy = data.aws_iam_policy_document.github_deploy.json
}
