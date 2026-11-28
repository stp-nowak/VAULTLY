using System.Net.Http.Headers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Vaultly.Core.Messaging;
using Vaultly.Core.Testing;
using Vaultly.Transactions.Infrastructure.Persistence;

namespace Vaultly.Transactions.Application.Tests;

/// <summary>Hosts the Transactions API behind a <see cref="TestGateway"/>, on an in-memory database, messaging off.</summary>
internal sealed class TransactionsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    public TestGateway Gateway { get; } = new("transactions");

    public RecordingMessageBus Bus { get; } = new();

    public HttpClient ClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Gateway.TokenFor(userId));
        return client;
    }

    public async Task WithDatabaseAsync(Func<TransactionsDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        await action(dbContext);
        await dbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Messaging:Enabled", "false");
        foreach (var (key, value) in Gateway.Configuration)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TransactionsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TransactionsDbContext>>();
            services.AddDbContext<TransactionsDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<IMessageBus>(Bus);
            Gateway.ServeKeys(services);
        });
    }
}
