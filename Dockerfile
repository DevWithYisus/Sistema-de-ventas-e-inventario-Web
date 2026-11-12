# Imagen de la API. Construir desde la raíz del repositorio:
#   docker build -t mininos-api .
# Configuración por variables de entorno (ver .env.example y DESPLIEGUE.md).

# ---------- Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY global.json SistemaVentas.slnx ./
COPY src/ src/

RUN dotnet restore src/Api/SistemaVentas.Api/SistemaVentas.Api.csproj
RUN dotnet publish src/Api/SistemaVentas.Api/SistemaVentas.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Usuario sin privilegios que ya trae la imagen oficial
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SistemaVentas.Api.dll"]
