# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (cached on csproj changes)
COPY *.csproj ./
RUN dotnet restore

# Build + publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# AssemblyName in the .csproj is CreditCardApi -> CreditCardApi.dll
ENTRYPOINT ["dotnet", "CreditCardApi.dll"]
