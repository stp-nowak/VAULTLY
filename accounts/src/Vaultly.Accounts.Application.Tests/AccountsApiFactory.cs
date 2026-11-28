using System.Net.Http.Headers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Vaultly.Accounts.Infrastructure.Persistence;
using Vaultly.Core.Messaging;
using Vaultly.Core.Testing;

namespace Vaultly.Accounts.Application.Tests;

/// <summary>Hosts the Accounts API behind a <see cref="TestGateway"/>, on an in-memory database.</summary>
internal sealed class AccountsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    public TestGateway Gateway { get; } = new("accounts");

    public RecordingMessageBus Bus { get; } = new();

    public HttpClient ClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Gateway.TokenFor(userId));
        return client;
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
            services.RemoveAll<DbContextOptions<AccountsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AccountsDbContext>>();
            services.AddDbContext<AccountsDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<IMessageBus>(Bus);
            Gateway.ServeKeys(services);
        });
    }
}
