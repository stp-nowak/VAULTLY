using Microsoft.AspNetCore.DataProtection;

using StackExchange.Redis;

namespace Vaultly.Web.Api.Common.Hosting;

/// <summary>
/// Cross-feature infrastructure of the web API: Redis (session store + Data Protection key ring) and time.
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    /// <summary>Environment name used by integration tests.</summary>
    public const string TestingEnvironment = "Testing";

    private const string RedisInstanceName = "vaultly:web:";
    private const string DataProtectionKeysRedisKey = "vaultly:web:data-protection-keys";

    /// <summary>
    /// Registers Redis-backed <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> and Data Protection.
    /// Redis is mandatory outside Development/Testing because tokens must survive restarts and be shared by all API instances.
    /// </summary>
    public static IServiceCollection AddPlatform(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton(TimeProvider.System);

        var dataProtection = services.AddDataProtection().SetApplicationName("Vaultly.Web.Api");
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment(TestingEnvironment))
            {
                throw new InvalidOperationException("ConnectionStrings:Redis is required: the web API stores session tokens in Redis.");
            }

            // Development fallback only: sessions are lost on restart and not shared between instances.
            services.AddDistributedMemoryCache();
            return services;
        }

        // Lazy so the app (and tests that replace the cache) can start without Redis being reachable yet.
        var multiplexer = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = RedisInstanceName;
            options.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer.Value);
        });

        dataProtection.PersistKeysToStackExchangeRedis(() => multiplexer.Value.GetDatabase(), DataProtectionKeysRedisKey);

        return services;
    }
}
