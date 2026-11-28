namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

/// <summary>The cursor was not one this service handed out, or the page size is out of range.</summary>
public sealed class InvalidPageRequestException(string message) : Exception(message);
