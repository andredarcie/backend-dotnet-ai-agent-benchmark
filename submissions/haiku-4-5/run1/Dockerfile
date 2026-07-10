FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CreditCardApi/CreditCardApi.csproj", "CreditCardApi/"]
RUN dotnet restore "CreditCardApi/CreditCardApi.csproj"

COPY src/CreditCardApi/ CreditCardApi/
WORKDIR /src/CreditCardApi
RUN dotnet build "CreditCardApi.csproj" -c Release -o /app/build
RUN dotnet publish "CreditCardApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CreditCardApi.dll"]
