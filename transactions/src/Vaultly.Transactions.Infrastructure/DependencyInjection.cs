using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Rebus.Config;

using Vaultly.Accounts.PublicTypes.Commands;
using Vaultly.Accounts.PublicTypes.Events;
using Vaultly.Core;
using Vaultly.Core.Messaging.Rebus;
using Vaultly.Core.Persistence;
using Vaultly.Transactions.Application.Features.Transactions;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Infrastructure.Accounts;
using Vaultly.Transactions.Infrastructure.Persistence;
using Vaultly.Transactions.Infrastructure.Postings;
using Vaultly.Transactions.Infrastructure.Transactions;

namespace Vaultly.Transactions.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TransactionsDb";
    public const string Queue = "transactions";
    public const string AccountsQueue = "accounts";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TransactionsDbContext>(options =>
            options.UsePostgres(configuration.GetConnectionString(ConnectionStringName)));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionReadService, TransactionReadService>();
        services.AddScoped<IUnitOfWork, OutboxUnitOfWork<TransactionsDbContext>>();

        services.AddRebusMessaging(configuration, new MessagingEndpoint(Queue, ConnectionStringName)
        {
            Subscriptions = [typeof(AccountOpened), typeof(AccountClosed)],
            CommandOwners = new Dictionary<Type, string> { [typeof(PostTransaction)] = AccountsQueue },
            Sagas = true
        });
        services.AddRebusHandler<AccountEventsMessageHandler>();
        services.AddRebusHandler<TransactionPostingSaga>();

        return services;
    }
}
