# Segredos do ambiente (ADR-30 e ADR-39)
#
# O Terraform gera os valores e guarda num único secret do Secrets Manager, em JSON.
# Na etapa do ECS, a task recebe cada chave como variável de ambiente
# (ex.: Database__Password ← PostgresPassword), lida no momento em que a task sobe.
# Nenhum segredo fica no repositório, na imagem Docker ou na definição da task.
#
# Os valores também ficam no state do Terraform. Por isso o bucket do state é
# criptografado e privado (ADR-35).

# O RDS não aceita / @ " nem espaço na senha do usuário master.
resource "random_password" "postgres" {
  length           = 32
  special          = true
  override_special = "!#%^*()-_=+[]{}:?"
}

# 64 caracteres: bem acima do mínimo de 32 bytes do HS256 (ADR-07).
resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}

resource "random_password" "rabbitmq" {
  length  = 32
  special = false
}

resource "aws_secretsmanager_secret" "app" {
  name        = "${local.name}/app"
  description = "Segredos do store-microservices: senha do banco, chave JWT e senha do RabbitMQ."

  # Ambiente de demonstração: o destroy apaga na hora, e um apply seguinte pode recriar
  # o secret com o mesmo nome. O padrão (7 a 30 dias de espera) protege contra exclusão
  # acidental em produção.
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "app" {
  secret_id = aws_secretsmanager_secret.app.id

  secret_string = jsonencode({
    PostgresPassword = random_password.postgres.result
    JwtSigningKey    = random_password.jwt_signing_key.result
    RabbitMqPassword = random_password.rabbitmq.result
  })
}
