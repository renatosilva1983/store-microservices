# Security groups (ADR-27 e ADR-36): firewall por recurso, liberando só quem precisa.
#
#   internet ──80/443──▶ ALB ──▶ gateway ──▶ demais serviços ──5432──▶ RDS
#
# Regras escritas como recursos separados (aws_vpc_security_group_*_rule): cada regra
# tem nome e descrição, e mudar uma não recria as outras.
# As regras de ALB e entre serviços entram na etapa do ECS.

resource "aws_security_group" "services" {
  name        = "${local.name}-services"
  description = "Tasks do ECS (microsservicos e RabbitMQ)"
  vpc_id      = aws_vpc.main.id

  tags = { Name = "${local.name}-services" }
}

# Saída liberada: as tasks baixam imagens do ECR, leem o Secrets Manager e
# mandam logs ao CloudWatch, tudo pela internet (não há NAT nem VPC Endpoints).
resource "aws_vpc_security_group_egress_rule" "services_all" {
  security_group_id = aws_security_group.services.id
  description       = "Saida para a internet e para a VPC"
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"
}

resource "aws_security_group" "database" {
  name        = "${local.name}-database"
  description = "RDS PostgreSQL: so aceita as tasks do ECS"
  vpc_id      = aws_vpc.main.id

  tags = { Name = "${local.name}-database" }
}

# A origem é outro security group, não uma faixa de IP: vale para qualquer task
# que use o SG "services", mesmo com IPs que mudam a cada deploy.
resource "aws_vpc_security_group_ingress_rule" "database_from_services" {
  security_group_id            = aws_security_group.database.id
  description                  = "PostgreSQL a partir das tasks"
  ip_protocol                  = "tcp"
  from_port                    = 5432
  to_port                      = 5432
  referenced_security_group_id = aws_security_group.services.id
}
