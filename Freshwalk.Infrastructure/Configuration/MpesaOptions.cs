namespace Freshwalk.Infrastructure.Configuration;

public class MpesaOptions
{
    public const string SectionName = "Mpesa";

    public string Environment { get; set; } = "sandbox";
    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;
    public string BusinessShortCode { get; set; } = string.Empty;
    public string PassKey { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}
