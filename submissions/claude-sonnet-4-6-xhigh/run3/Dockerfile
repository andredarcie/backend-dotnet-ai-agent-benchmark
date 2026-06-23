FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CreditCardApi/CreditCardApi.csproj CreditCardApi/
RUN dotnet restore CreditCardApi/CreditCardApi.csproj

COPY CreditCardApi/ CreditCardApi/
RUN dotnet publish CreditCardApi/CreditCardApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CreditCardApi.dll"]
