# Render no ejecuta .NET de forma nativa: se despliega con Docker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Kassette.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish Kassette.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["dotnet", "Kassette.dll"]
