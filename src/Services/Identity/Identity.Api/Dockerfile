FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props Directory.Build.props ./
COPY src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj src/BuildingBlocks/BuildingBlocks.Application/
COPY src/BuildingBlocks/BuildingBlocks.Api/BuildingBlocks.Api.csproj src/BuildingBlocks/BuildingBlocks.Api/
COPY src/Services/Identity/Identity.Api/Identity.Api.csproj src/Services/Identity/Identity.Api/
RUN dotnet restore src/Services/Identity/Identity.Api/Identity.Api.csproj

COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Identity/ src/Services/Identity/
RUN dotnet publish src/Services/Identity/Identity.Api/Identity.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
