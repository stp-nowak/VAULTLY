# Accounts API. Build context: this directory (accounts/src), plus the projects it references as named contexts:
# core = core/src and identity-public-types = auth/src/Vaultly.Identity.PublicTypes (the compose file and
# accounts.yml pass them):
#   docker build --build-context core=core/src --build-context identity-public-types=auth/src/Vaultly.Identity.PublicTypes -t vaultly-accounts-api accounts/src
# Projects keep their repository paths below /repo so the relative project references resolve.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo/accounts/src
COPY --from=core Vaultly.Core/Vaultly.Core.csproj /repo/core/src/Vaultly.Core/
COPY --from=core Vaultly.Core.Persistence/Vaultly.Core.Persistence.csproj /repo/core/src/Vaultly.Core.Persistence/
COPY --from=core Vaultly.Core.Authentication/Vaultly.Core.Authentication.csproj /repo/core/src/Vaultly.Core.Authentication/
COPY --from=identity-public-types *.csproj /repo/auth/src/Vaultly.Identity.PublicTypes/
COPY Vaultly.Accounts.Api/Vaultly.Accounts.Api.csproj Vaultly.Accounts.Api/
COPY Vaultly.Accounts.Application/Vaultly.Accounts.Application.csproj Vaultly.Accounts.Application/
COPY Vaultly.Accounts.Domain/Vaultly.Accounts.Domain.csproj Vaultly.Accounts.Domain/
COPY Vaultly.Accounts.Infrastructure/Vaultly.Accounts.Infrastructure.csproj Vaultly.Accounts.Infrastructure/
RUN dotnet restore Vaultly.Accounts.Api/Vaultly.Accounts.Api.csproj
COPY --from=core Vaultly.Core/ /repo/core/src/Vaultly.Core/
COPY --from=core Vaultly.Core.Persistence/ /repo/core/src/Vaultly.Core.Persistence/
COPY --from=core Vaultly.Core.Authentication/ /repo/core/src/Vaultly.Core.Authentication/
COPY --from=identity-public-types *.cs /repo/auth/src/Vaultly.Identity.PublicTypes/
COPY . .
RUN dotnet publish Vaultly.Accounts.Api/Vaultly.Accounts.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultly.Accounts.Api.dll"]
