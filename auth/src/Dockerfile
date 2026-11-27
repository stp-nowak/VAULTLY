# Identity API. Build context: this directory (auth/src), plus Vaultly.Core it references as the named context core
# (the compose file and auth.yml pass it):
#   docker build --build-context core=core/src/Vaultly.Core -t vaultly-identity-api auth/src
# Projects keep their repository paths below /repo so the relative project reference resolves.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo/auth/src
COPY --from=core *.csproj /repo/core/src/Vaultly.Core/
COPY Vaultly.Identity.Api/Vaultly.Identity.Api.csproj Vaultly.Identity.Api/
COPY Vaultly.Identity.Application/Vaultly.Identity.Application.csproj Vaultly.Identity.Application/
COPY Vaultly.Identity.Domain/Vaultly.Identity.Domain.csproj Vaultly.Identity.Domain/
COPY Vaultly.Identity.Infrastructure/Vaultly.Identity.Infrastructure.csproj Vaultly.Identity.Infrastructure/
COPY Vaultly.Identity.PublicTypes/Vaultly.Identity.PublicTypes.csproj Vaultly.Identity.PublicTypes/
COPY Vaultly.Identity.ApiContracts/Vaultly.Identity.ApiContracts.csproj Vaultly.Identity.ApiContracts/
RUN dotnet restore Vaultly.Identity.Api/Vaultly.Identity.Api.csproj
COPY --from=core *.cs /repo/core/src/Vaultly.Core/
COPY . .
RUN dotnet publish Vaultly.Identity.Api/Vaultly.Identity.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
# Home of the JWT signing key (Identity:JwtPrivateKeyPemFilePath). Owned by the app user so a volume mounted here
# is writable when the key is auto-generated in Development.
RUN install --directory --owner "$APP_UID" .secrets
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultly.Identity.Api.dll"]
