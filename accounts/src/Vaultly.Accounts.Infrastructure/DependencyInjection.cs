using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;
using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Infrastructure.Accounts;
using Vaultly.Accounts.Infrastructure.Persistence;
using Vaultly.Core;
using Vaultly.Core.Messaging.Rebus;
using Vaultly.Core.Persistence;

namespace Vaultly.Accounts.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "AccountsDb";
    public const string Queue = "accounts";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AccountsDbContext>(options =>
            options.UsePostgres(configuration.GetConnectionString(ConnectionStringName)));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, OutboxUnitOfWork<AccountsDbContext>>();
        services.AddRebusMessaging(configuration, new MessagingEndpoint(Queue, ConnectionStringName));
        services.AddScoped<AccountReadService>();
        services.AddScoped<IAccountReadService>(provider => provider.GetRequiredService<AccountReadService>());
        services.AddScoped<INetWorthReadService>(provider => provider.GetRequiredService<AccountReadService>());

        return services;
    }
}
