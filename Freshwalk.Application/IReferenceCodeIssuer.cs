namespace Freshwalk.Application;

public interface IReferenceCodeIssuer
{
    /// <summary>Issues a unique code: PREFIX-yyyyMMddHHmmssfff-#### (cryptographic 4-digit suffix).</summary>
    Task<string> IssueAsync(string prefix, CancellationToken cancellationToken = default);
}
