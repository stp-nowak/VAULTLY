# Transactions API. Build context: this directory (transactions/src), plus the projects it references as named
# contexts: core = core/src, accounts-public-types = accounts/src/Vaultly.Accounts.PublicTypes and
# identity-public-types = auth/src/Vaultly.Identity.PublicTypes (the compose file and transactions.yml pass them):
#   docker build --build-context core=core/src --build-context accounts-public-types=accounts/src/Vaultly.Accounts.PublicTypes --build-context identity-public-types=auth/src/Vaultly.Identity.PublicTypes -t vaultly-transactions-api transactions/src
# Projects keep their repository paths below /repo so the relative project references resolve.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo/transactions/src
COPY --from=core Vaultly.Core/Vaultly.Core.csproj /repo/core/src/Vaultly.Core/
COPY --from=core Vaultly.Core.Persistence/Vaultly.Core.Persistence.csproj /repo/core/src/Vaultly.Core.Persistence/
COPY --from=core Vaultly.Core.Authentication/Vaultly.Core.Authentication.csproj /repo/core/src/Vaultly.Core.Authentication/
COPY --from=core Vaultly.Core.Messaging.Rebus/Vaultly.Core.Messaging.Rebus.csproj /repo/core/src/Vaultly.Core.Messaging.Rebus/
COPY --from=accounts-public-types *.csproj /repo/accounts/src/Vaultly.Accounts.PublicTypes/
COPY --from=identity-public-types *.csproj /repo/auth/src/Vaultly.Identity.PublicTypes/
COPY Vaultly.Transactions.Api/Vaultly.Transactions.Api.csproj Vaultly.Transactions.Api/
COPY Vaultly.Transactions.Application/Vaultly.Transactions.Application.csproj Vaultly.Transactions.Application/
COPY Vaultly.Transactions.Domain/Vaultly.Transactions.Domain.csproj Vaultly.Transactions.Domain/
COPY Vaultly.Transactions.Infrastructure/Vaultly.Transactions.Infrastructure.csproj Vaultly.Transactions.Infrastructure/
RUN dotnet restore Vaultly.Transactions.Api/Vaultly.Transactions.Api.csproj
COPY --from=core Vaultly.Core/ /repo/core/src/Vaultly.Core/
COPY --from=core Vaultly.Core.Persistence/ /repo/core/src/Vaultly.Core.Persistence/
COPY --from=core Vaultly.Core.Authentication/ /repo/core/src/Vaultly.Core.Authentication/
COPY --from=core Vaultly.Core.Messaging.Rebus/ /repo/core/src/Vaultly.Core.Messaging.Rebus/
COPY --from=accounts-public-types Commands/ /repo/accounts/src/Vaultly.Accounts.PublicTypes/Commands/
COPY --from=accounts-public-types Events/ /repo/accounts/src/Vaultly.Accounts.PublicTypes/Events/
COPY --from=identity-public-types *.cs /repo/auth/src/Vaultly.Identity.PublicTypes/
COPY . .
RUN dotnet publish Vaultly.Transactions.Api/Vaultly.Transactions.Api.csproj --configuration Release --no-restore -p:OpenApiGenerateDocuments=false --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultly.Transactions.Api.dll"]
