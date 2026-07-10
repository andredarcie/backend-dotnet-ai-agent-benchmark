FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CreditCardApi.Api/CreditCardApi.Api.csproj", "CreditCardApi.Api/"]
RUN dotnet restore "CreditCardApi.Api/CreditCardApi.Api.csproj"

COPY . .
WORKDIR "/src/CreditCardApi.Api"
RUN dotnet build "CreditCardApi.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CreditCardApi.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

RUN useradd -m -u 1001 dotnetuser && chown -R dotnetuser:dotnetuser /app
USER dotnetuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "CreditCardApi.Api.dll"]
