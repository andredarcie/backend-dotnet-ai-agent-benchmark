# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------------------------
# Build stage: restore first (own layer, cached until package graph changes), then publish.
# ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# .editorconfig carries the analyzer configuration and must be present for the build to be
# warning-free (warnings are errors in this repo).
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/CreditCardApi.Domain/CreditCardApi.Domain.csproj src/CreditCardApi.Domain/
COPY src/CreditCardApi.Application/CreditCardApi.Application.csproj src/CreditCardApi.Application/
COPY src/CreditCardApi.Infrastructure/CreditCardApi.Infrastructure.csproj src/CreditCardApi.Infrastructure/
COPY src/CreditCardApi.Api/CreditCardApi.Api.csproj src/CreditCardApi.Api/
RUN dotnet restore src/CreditCardApi.Api/CreditCardApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/CreditCardApi.Api/CreditCardApi.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------------------------
# Runtime stage: aspnet base image, non-root, plain HTTP on 8080 (TLS terminates upstream).
# ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The base image ships an unprivileged 'app' user; APP_UID is provided by the image.
USER $APP_UID

ENTRYPOINT ["dotnet", "CreditCardApi.Api.dll"]
