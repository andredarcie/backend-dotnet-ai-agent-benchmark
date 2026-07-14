# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, on just the project files, so source-only edits don't invalidate the layer cache.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/CreditCardApi.Domain/CreditCardApi.Domain.csproj src/CreditCardApi.Domain/
COPY src/CreditCardApi.Application/CreditCardApi.Application.csproj src/CreditCardApi.Application/
COPY src/CreditCardApi.Infrastructure/CreditCardApi.Infrastructure.csproj src/CreditCardApi.Infrastructure/
COPY src/CreditCardApi.Api/CreditCardApi.Api.csproj src/CreditCardApi.Api/
RUN dotnet restore src/CreditCardApi.Api/CreditCardApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/CreditCardApi.Api/CreditCardApi.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app .

# mcr.microsoft.com/dotnet/aspnet:10.0 already ships a non-root "app" account - no useradd needed.
USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "CreditCardApi.Api.dll"]
