FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/CreditCardApi/CreditCardApi.csproj", "CreditCardApi/"]
RUN dotnet restore "CreditCardApi/CreditCardApi.csproj"

COPY ["src/CreditCardApi/", "CreditCardApi/"]
WORKDIR "/src/CreditCardApi"
RUN dotnet build "CreditCardApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CreditCardApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
EXPOSE 8080

RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CreditCardApi.dll"]
