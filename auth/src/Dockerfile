# Identity API. Build context: this directory (auth/src).
#   docker build -t vaultly-identity-api auth/src

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Vaultly.Identity.Api/Vaultly.Identity.Api.csproj Vaultly.Identity.Api/
COPY Vaultly.Identity.Application/Vaultly.Identity.Application.csproj Vaultly.Identity.Application/
COPY Vaultly.Identity.Domain/Vaultly.Identity.Domain.csproj Vaultly.Identity.Domain/
COPY Vaultly.Identity.Infrastructure/Vaultly.Identity.Infrastructure.csproj Vaultly.Identity.Infrastructure/
COPY Vaultly.Identity.SharedKernel/Vaultly.Identity.SharedKernel.csproj Vaultly.Identity.SharedKernel/
RUN dotnet restore Vaultly.Identity.Api/Vaultly.Identity.Api.csproj
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
