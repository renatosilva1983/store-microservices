# Multi-stage (ADR-09): o SDK compila; a imagem final leva só o runtime (menor e mais segura).
# Contexto do build = raiz do repositório (ver docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Só os arquivos de projeto primeiro: o restore fica em cache enquanto as dependências não mudarem.
#    Directory.*.props trazem as versões centralizadas e as configurações comuns.
COPY Directory.Packages.props Directory.Build.props ./
COPY src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj src/BuildingBlocks/BuildingBlocks.Application/
COPY src/Services/Catalog/Catalog.Domain/Catalog.Domain.csproj src/Services/Catalog/Catalog.Domain/
COPY src/Services/Catalog/Catalog.Application/Catalog.Application.csproj src/Services/Catalog/Catalog.Application/
COPY src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Services/Catalog/Catalog.Infrastructure/
COPY src/Services/Catalog/Catalog.Api/Catalog.Api.csproj src/Services/Catalog/Catalog.Api/
RUN dotnet restore src/Services/Catalog/Catalog.Api/Catalog.Api.csproj

# 2) Depois o código, e publish.
COPY src/BuildingBlocks/BuildingBlocks.Application/ src/BuildingBlocks/BuildingBlocks.Application/
COPY src/Services/Catalog/ src/Services/Catalog/
RUN dotnet publish src/Services/Catalog/Catalog.Api/Catalog.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# Usuário sem privilégios, já existente na imagem oficial.
USER $APP_UID
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
