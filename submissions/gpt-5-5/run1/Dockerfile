FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props CreditCardService.slnx ./
COPY src/CreditCardApi/CreditCardApi.csproj src/CreditCardApi/
RUN dotnet restore src/CreditCardApi/CreditCardApi.csproj --locked-mode || dotnet restore src/CreditCardApi/CreditCardApi.csproj

COPY . .
RUN dotnet publish src/CreditCardApi/CreditCardApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "CreditCardApi.dll"]

