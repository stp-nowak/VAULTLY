# API gateway. Build context: this directory (gateway/src), plus the Identity project it references as the named
# context identity-public-types (the compose file and gateway.yml pass it):
#   docker build --build-context identity-public-types=auth/src/Vaultly.Identity.PublicTypes -t vaultly-gateway gateway/src
# Projects keep their repository paths below /src so the relative project reference resolves.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY --from=identity-public-types *.csproj auth/src/Vaultly.Identity.PublicTypes/
COPY Vaultly.Gateway/Vaultly.Gateway.csproj gateway/src/Vaultly.Gateway/
RUN dotnet restore gateway/src/Vaultly.Gateway/Vaultly.Gateway.csproj
COPY --from=identity-public-types *.cs auth/src/Vaultly.Identity.PublicTypes/
COPY Vaultly.Gateway/ gateway/src/Vaultly.Gateway/
RUN dotnet publish gateway/src/Vaultly.Gateway/Vaultly.Gateway.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
COPY --from=build /app/publish .
# Home of the internal token signing key (InternalTokens:SigningKeyPemFilePath). Owned by the app user so a volume
# mounted here is writable when the key is auto-generated in Development.
RUN install --directory --owner "$APP_UID" .secrets
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultly.Gateway.dll"]
