# Banco de dados (ADR-28 e ADR-40)
#
# Uma instância RDS PostgreSQL com um banco por serviço (catalog e ordering). Os bancos
# não são criados aqui: as migrations do EF Core criam cada um na primeira subida do
# serviço (Database__ApplyMigrationsOnStartup=true), como no docker-compose.

resource "aws_db_subnet_group" "main" {
  name        = local.name
  description = "Subnets de dados, sem rota para a internet"
  subnet_ids  = aws_subnet.data[*].id
}

resource "aws_db_instance" "main" {
  identifier = "${local.name}-postgres"

  engine         = "postgres"
  engine_version = var.postgres_version # mesma versão major do ambiente local
  instance_class = var.db_instance_class

  allocated_storage = 20
  storage_type      = "gp3"
  storage_encrypted = true

  username = "store_admin"
  password = random_password.postgres.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.database.id]
  publicly_accessible    = false

  # Escolhas de ambiente de demonstração, todas com custo menor:
  multi_az                = false # produção: true (réplica em outra zona, failover automático)
  backup_retention_period = 1     # produção: 7 ou mais
  skip_final_snapshot     = true  # produção: false (snapshot antes de apagar)
  deletion_protection     = false # produção: true
  apply_immediately       = true

  auto_minor_version_upgrade = true
}
