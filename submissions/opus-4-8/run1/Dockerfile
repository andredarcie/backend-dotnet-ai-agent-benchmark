# syntax=docker/dockerfile:1

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached layer): copy only the files restore needs.
COPY Directory.Build.props Directory.Packages.props CreditCardApi.sln ./
COPY src/CreditCardApi.Domain/*.csproj src/CreditCardApi.Domain/
COPY src/CreditCardApi.Application/*.csproj src/CreditCardApi.Application/
COPY src/CreditCardApi.Infrastructure/*.csproj src/CreditCardApi.Infrastructure/
COPY src/CreditCardApi.Api/*.csproj src/CreditCardApi.Api/
RUN dotnet restore src/CreditCardApi.Api/CreditCardApi.Api.csproj

# Copy the rest and publish.
COPY src/ src/
RUN dotnet publish src/CreditCardApi.Api/CreditCardApi.Api.csproj \
    -c Release -o /app --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Run as a non-root user.
RUN groupadd --system appgroup \
    && useradd --system --gid appgroup --home /app --shell /usr/sbin/nologin appuser \
    && chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "CreditCardApi.Api.dll"]
