# syntax=docker/dockerfile:1

# ---- build: restore (cached on project files only), then build and publish ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props SalesDashboard.sln ./
COPY src/SalesDashboard.Domain/SalesDashboard.Domain.csproj                 src/SalesDashboard.Domain/
COPY src/SalesDashboard.Application/SalesDashboard.Application.csproj       src/SalesDashboard.Application/
COPY src/SalesDashboard.Infrastructure/SalesDashboard.Infrastructure.csproj src/SalesDashboard.Infrastructure/
COPY src/SalesDashboard.Api/SalesDashboard.Api.csproj                       src/SalesDashboard.Api/
COPY tests/SalesDashboard.UnitTests/SalesDashboard.UnitTests.csproj         tests/SalesDashboard.UnitTests/
RUN dotnet restore src/SalesDashboard.Api/SalesDashboard.Api.csproj \
 && dotnet restore tests/SalesDashboard.UnitTests/SalesDashboard.UnitTests.csproj

COPY .editorconfig ./
COPY src/ src/
COPY tests/ tests/

# ---- test: `docker build --target test .` runs the unit tests ----
FROM build AS test
RUN dotnet test tests/SalesDashboard.UnitTests/SalesDashboard.UnitTests.csproj --no-restore -c Release

# ---- publish ----
FROM build AS publish
RUN dotnet publish src/SalesDashboard.Api/SalesDashboard.Api.csproj --no-restore -c Release -o /app /p:UseAppHost=false

# ---- runtime: small ASP.NET image, non-root user ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# curl only for the container healthcheck (the API listens only after migrations and seeding finish).
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
COPY --from=publish /app .
USER $APP_UID
HEALTHCHECK --interval=5s --timeout=3s --start-period=10s --retries=30 CMD curl -fsS http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "SalesDashboard.Api.dll"]
