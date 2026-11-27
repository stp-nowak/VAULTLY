namespace Vaultly.Accounts.Api.Endpoints;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
        => app.MapAccountEndpoints().MapNetWorthEndpoints();
}
