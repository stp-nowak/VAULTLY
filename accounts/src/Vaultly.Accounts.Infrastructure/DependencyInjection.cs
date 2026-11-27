using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;
using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Infrastructure.Persistence;
using Vaultly.Accounts.Infrastructure.Persistence.ReadServices;
using Vaultly.Accounts.Infrastructure.Persistence.Repositories;
using Vaultly.Core;
using Vaultly.Core.Persistence;

namespace Vaultly.Accounts.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "AccountsDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AccountsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString(ConnectionStringName)));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<AccountsDbContext>>();
        services.AddScoped<AccountReadService>();
        services.AddScoped<IAccountReadService>(provider => provider.GetRequiredService<AccountReadService>());
        services.AddScoped<INetWorthReadService>(provider => provider.GetRequiredService<AccountReadService>());

        return services;
    }
}
