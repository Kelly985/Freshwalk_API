namespace Freshwalk.Application;

public record StkInitiateResult(
    bool Ok,
    string? MerchantRequestId,
    string? CheckoutRequestId,
    string? CustomerMessage,
    string? Error);

public interface IMpesaStkService
{
    Task<StkInitiateResult> InitiateAsync(
        decimal amountKes,
        string phoneNumber,
        string accountReference,
        string? description,
        CancellationToken cancellationToken = default);
}
