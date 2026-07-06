FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/CreditCardApi.Domain/CreditCardApi.Domain.csproj src/CreditCardApi.Domain/
COPY src/CreditCardApi.Application/CreditCardApi.Application.csproj src/CreditCardApi.Application/
COPY src/CreditCardApi.Infrastructure/CreditCardApi.Infrastructure.csproj src/CreditCardApi.Infrastructure/
COPY src/CreditCardApi.Api/CreditCardApi.Api.csproj src/CreditCardApi.Api/
RUN dotnet restore src/CreditCardApi.Api/CreditCardApi.Api.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/CreditCardApi.Api/CreditCardApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# "app" (uid/gid 1654) is a non-root service account already provisioned by the base image.
USER app

ENTRYPOINT ["dotnet", "CreditCardApi.Api.dll"]
