# ==============================================================================
# Stage 1: Build & Restore Environment (.NET 9 SDK)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Install dotnet-ef global tool for standalone migration bundle generation
RUN dotnet tool install --global dotnet-ef --version 9.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# 1. Copy solution and project definitions only for maximum layer cache efficiency
COPY RetailOS.sln ./
COPY src/RetailOS.Shared/RetailOS.Shared.csproj src/RetailOS.Shared/
COPY src/RetailOS.Domain/RetailOS.Domain.csproj src/RetailOS.Domain/
COPY src/RetailOS.Application/RetailOS.Application.csproj src/RetailOS.Application/
COPY src/RetailOS.Infrastructure/RetailOS.Infrastructure.csproj src/RetailOS.Infrastructure/
COPY src/RetailOS.Api/RetailOS.Api.csproj src/RetailOS.Api/
COPY tests/RetailOS.UnitTests/RetailOS.UnitTests.csproj tests/RetailOS.UnitTests/
COPY tests/RetailOS.IntegrationTests/RetailOS.IntegrationTests.csproj tests/RetailOS.IntegrationTests/

# 2. Restore NuGet dependencies
RUN dotnet restore src/RetailOS.Api/RetailOS.Api.csproj --runtime linux-musl-x64

# 3. Copy full source code
COPY . .

# 4. Compile and publish the Web API
RUN dotnet publish src/RetailOS.Api/RetailOS.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# 5. Build standalone EF Core Migrations Bundle for automated database updates
RUN dotnet-ef migrations bundle \
    --project src/RetailOS.Infrastructure/RetailOS.Infrastructure.csproj \
    --startup-project src/RetailOS.Api/RetailOS.Api.csproj \
    --output /app/publish/bundle \
    --no-build \
    --configuration Release \
    --self-contained \
    -r linux-musl-x64

# Copy entrypoint script into publish folder
COPY entrypoint.sh /app/publish/entrypoint.sh
RUN chmod +x /app/publish/entrypoint.sh /app/publish/bundle

# ==============================================================================
# Stage 2: Production Runtime Environment (.NET 9 ASP.NET Alpine)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install curl for docker healthcheck and tzdata for timezone support
RUN apk add --no-cache curl tzdata

# Set Production Environment Variables
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Africa/Cairo

# Copy compiled artifacts from build stage
COPY --from=build /app/publish .

# Security Compliance: Run as non-root user (built-in UID 1654 in .NET Alpine)
RUN chown -R $APP_UID:$APP_UID /app
USER $APP_UID

EXPOSE 5000

# Docker Healthcheck targeting the live health probe
HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health/live || exit 1

ENTRYPOINT ["/app/entrypoint.sh"]
