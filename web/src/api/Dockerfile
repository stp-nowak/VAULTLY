# Web API (backend-for-frontend). Build context: this directory (web/src/api), plus the Identity project it references
# as the named context identity-api-contracts (the compose file and web-api.yml pass it):
#   docker build --build-context identity-api-contracts=auth/src/Vaultly.Identity.ApiContracts -t vaultly-web-api web/src/api
# Projects keep their repository paths below /src so the relative project reference resolves.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY --from=identity-api-contracts *.csproj auth/src/Vaultly.Identity.ApiContracts/
COPY Vaultly.Web.Api/Vaultly.Web.Api.csproj web/src/api/Vaultly.Web.Api/
RUN dotnet restore web/src/api/Vaultly.Web.Api/Vaultly.Web.Api.csproj
COPY --from=identity-api-contracts *.cs auth/src/Vaultly.Identity.ApiContracts/
COPY Vaultly.Web.Api/ web/src/api/Vaultly.Web.Api/
RUN dotnet publish web/src/api/Vaultly.Web.Api/Vaultly.Web.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultly.Web.Api.dll"]
