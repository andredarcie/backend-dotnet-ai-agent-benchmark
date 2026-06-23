# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first to leverage Docker layer caching.
COPY CreditCardApi.csproj ./
RUN dotnet restore CreditCardApi.csproj

# Copy the rest and publish.
COPY . ./
RUN dotnet publish CreditCardApi.csproj -c Release -o /app /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# librdkafka (Confluent.Kafka) dlopen's the GSSAPI/Kerberos library on startup.
# Not needed for PLAINTEXT, but installing it silences the probe error and keeps
# the SASL code path available.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "CreditCardApi.dll"]
