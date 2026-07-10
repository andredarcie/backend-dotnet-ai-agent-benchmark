# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder
WORKDIR /src

COPY src/CreditCardApi.Api/CreditCardApi.Api.csproj src/CreditCardApi.Api/
RUN dotnet restore src/CreditCardApi.Api/CreditCardApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/CreditCardApi.Api/CreditCardApi.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser:appuser /app
USER appuser

COPY --from=builder /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CreditCardApi.Api.dll"]
