# Imagen de producción de HobbitCoder.TMTBucaramanga.Identity.
# Escucha HTTP en el puerto 8080: el HTTPS lo termina el proxy (Traefik de Dokploy) delante del contenedor.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los archivos de proyecto: la restauración queda en caché mientras no cambien las dependencias.
COPY Directory.Build.props Directory.Packages.props ./
COPY HobbitCoder.TMTBucaramanga.Identity.WebAPI/HobbitCoder.TMTBucaramanga.Identity.WebAPI.csproj HobbitCoder.TMTBucaramanga.Identity.WebAPI/
COPY HobbitCoder.TMTBucaramanga.Identity.Application/HobbitCoder.TMTBucaramanga.Identity.Application.csproj HobbitCoder.TMTBucaramanga.Identity.Application/
COPY HobbitCoder.TMTBucaramanga.Identity.Domain/HobbitCoder.TMTBucaramanga.Identity.Domain.csproj HobbitCoder.TMTBucaramanga.Identity.Domain/
COPY HobbitCoder.TMTBucaramanga.Identity.Mediator/HobbitCoder.TMTBucaramanga.Identity.Mediator.csproj HobbitCoder.TMTBucaramanga.Identity.Mediator/
COPY HobbitCoder.TMTBucaramanga.Identity.Infrastructure/HobbitCoder.TMTBucaramanga.Identity.Infrastructure.csproj HobbitCoder.TMTBucaramanga.Identity.Infrastructure/
COPY HobbitCoder.TMTBucaramanga.Identity.Persistence.MSSQL/HobbitCoder.TMTBucaramanga.Identity.Persistence.MSSQL.csproj HobbitCoder.TMTBucaramanga.Identity.Persistence.MSSQL/
COPY HobbitCoder.TMTBucaramanga.Identity.Persistence.PostgreSQL/HobbitCoder.TMTBucaramanga.Identity.Persistence.PostgreSQL.csproj HobbitCoder.TMTBucaramanga.Identity.Persistence.PostgreSQL/
RUN dotnet restore HobbitCoder.TMTBucaramanga.Identity.WebAPI/HobbitCoder.TMTBucaramanga.Identity.WebAPI.csproj

COPY . .
RUN dotnet publish HobbitCoder.TMTBucaramanga.Identity.WebAPI/HobbitCoder.TMTBucaramanga.Identity.WebAPI.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASPNETCORE_FORWARDEDHEADERS_ENABLED: respeta X-Forwarded-Proto/For del proxy, así la app sabe que la
# petición original fue HTTPS. El issuer no depende de esto: siempre sale de Jwt:Issuer.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HobbitCoder.TMTBucaramanga.Identity.WebAPI.dll"]
