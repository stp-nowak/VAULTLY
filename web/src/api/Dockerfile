# Web API (backend-for-frontend). Build context: this directory (web/src/api).
#   docker build -t vaultly-web-api web/src/api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Vaultly.Web.Api/Vaultly.Web.Api.csproj Vaultly.Web.Api/
RUN dotnet restore Vaultly.Web.Api/Vaultly.Web.Api.csproj
COPY Vaultly.Web.Api/ Vaultly.Web.Api/
RUN dotnet publish Vaultly.Web.Api/Vaultly.Web.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultly.Web.Api.dll"]
