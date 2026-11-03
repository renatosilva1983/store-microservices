# Rede (ADR-27 e ADR-36)
#
#   VPC 10.20.0.0/16
#   ├── subnets públicas  (10.20.0.0/24, 10.20.1.0/24)   → ALB e tasks do ECS
#   │     rota 0.0.0.0/0 → Internet Gateway
#   └── subnets de dados  (10.20.10.0/24, 10.20.11.0/24) → RDS
#         sem rota para a internet (isoladas)
#
# Sem NAT Gateway (custaria ~US$ 32/mês só por existir). As tasks ficam nas subnets
# públicas para baixar imagens e falar com a AWS, mas o security group de cada uma só
# aceita tráfego vindo do ALB ou dos outros serviços. Em produção: tasks em subnets
# privadas com NAT ou VPC Endpoints.

resource "aws_vpc" "main" {
  cidr_block = var.vpc_cidr

  # DNS interno ligado: o Service Connect e o endpoint do RDS dependem dele.
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = { Name = local.name }
}

resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
  tags   = { Name = local.name }
}

resource "aws_subnet" "public" {
  count = length(local.azs)

  vpc_id            = aws_vpc.main.id
  availability_zone = local.azs[count.index]
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index) # 10.20.0.0/24, 10.20.1.0/24

  # IP público só quando o recurso pedir (o serviço ECS pede de forma explícita).
  map_public_ip_on_launch = false

  tags = { Name = "${local.name}-public-${local.azs[count.index]}", Tier = "public" }
}

resource "aws_subnet" "data" {
  count = length(local.azs)

  vpc_id            = aws_vpc.main.id
  availability_zone = local.azs[count.index]
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index + 10) # 10.20.10.0/24, 10.20.11.0/24

  tags = { Name = "${local.name}-data-${local.azs[count.index]}", Tier = "data" }
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }

  tags = { Name = "${local.name}-public" }
}

resource "aws_route_table_association" "public" {
  count = length(aws_subnet.public)

  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# Tabela sem rota de saída: o banco só conversa dentro da VPC.
resource "aws_route_table" "data" {
  vpc_id = aws_vpc.main.id
  tags   = { Name = "${local.name}-data" }
}

resource "aws_route_table_association" "data" {
  count = length(aws_subnet.data)

  subnet_id      = aws_subnet.data[count.index].id
  route_table_id = aws_route_table.data.id
}

# O security group padrão da VPC libera tudo entre seus membros. Assumir o controle dele
# sem regras força cada recurso a usar um security group próprio e explícito.
resource "aws_default_security_group" "default" {
  vpc_id = aws_vpc.main.id
  tags   = { Name = "${local.name}-default-sem-regras" }
}
