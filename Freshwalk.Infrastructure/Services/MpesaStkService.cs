using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Freshwalk.Application;
using Freshwalk.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Freshwalk.Infrastructure.Services;

public class MpesaStkService(IOptions<MpesaOptions> options, ILogger<MpesaStkService> log) : IMpesaStkService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<StkInitiateResult> InitiateAsync(
        decimal amountKes,
        string phoneNumber,
        string accountReference,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        if (string.IsNullOrWhiteSpace(cfg.ConsumerKey)
            || string.IsNullOrWhiteSpace(cfg.ConsumerSecret)
            || string.IsNullOrWhiteSpace(cfg.BusinessShortCode)
            || string.IsNullOrWhiteSpace(cfg.PassKey)
            || string.IsNullOrWhiteSpace(cfg.CallbackUrl))
            return new StkInitiateResult(false, null, null, null, "M-Pesa is not configured. Set Mpesa section in appsettings.");

        var kes = (int)Math.Ceiling(amountKes);
        if (kes < 1) kes = 1;

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new StkInitiateResult(false, null, null, null, "Phone number is required.");

        var env = cfg.Environment ?? "sandbox";
        var baseUrl = env.Equals("production", StringComparison.OrdinalIgnoreCase)
            ? "https://api.safaricom.co.ke"
            : "https://sandbox.safaricom.co.ke";

        try
        {
            using var authClient = new HttpClient();
            var authBytes = Encoding.UTF8.GetBytes($"{cfg.ConsumerKey}:{cfg.ConsumerSecret}");
            authClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var tokenRes = await authClient.GetAsync(
                $"{baseUrl}/oauth/v1/generate?grant_type=client_credentials",
                cancellationToken);
            var tokenJson = await tokenRes.Content.ReadAsStringAsync(cancellationToken);
            tokenRes.EnsureSuccessStatusCode();

            var tokenData = JsonSerializer.Deserialize<MpesaTokenResponse>(tokenJson, JsonOpts);
            var accessToken = tokenData?.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
                return new StkInitiateResult(false, null, null, null, "Failed to retrieve M-Pesa access token.");

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{cfg.BusinessShortCode}{cfg.PassKey}{timestamp}"));

            var phone = NormalizePhone(phoneNumber);

            var stkBody = new
            {
                BusinessShortCode = int.Parse(cfg.BusinessShortCode, System.Globalization.CultureInfo.InvariantCulture),
                Password = password,
                Timestamp = timestamp,
                TransactionType = "CustomerPayBillOnline",
                Amount = kes,
                PartyA = long.Parse(phone, System.Globalization.CultureInfo.InvariantCulture),
                PartyB = int.Parse(cfg.BusinessShortCode, System.Globalization.CultureInfo.InvariantCulture),
                PhoneNumber = long.Parse(phone, System.Globalization.CultureInfo.InvariantCulture),
                CallBackURL = cfg.CallbackUrl,
                AccountReference = accountReference.Length > 12 ? accountReference[..12] : accountReference,
                TransactionDesc = (description ?? "Freshwalk payment").Length > 13
                    ? (description ?? "Freshwalk payment")[..13]
                    : (description ?? "Freshwalk payment")
            };

            log.LogInformation(
                "M-Pesa STK request: env={Env}, phone={Phone}, amount={Amount}, shortCode={Short}",
                env, phone, kes, cfg.BusinessShortCode);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var jsonContent = new StringContent(JsonSerializer.Serialize(stkBody), Encoding.UTF8, "application/json");
            var stkRes = await client.PostAsync($"{baseUrl}/mpesa/stkpush/v1/processrequest", jsonContent, cancellationToken);
            var resJson = await stkRes.Content.ReadAsStringAsync(cancellationToken);

            if (!stkRes.IsSuccessStatusCode)
            {
                log.LogWarning("M-Pesa STK HTTP error: {Status} {Body}", (int)stkRes.StatusCode, resJson);
                return new StkInitiateResult(false, null, null, null, "STK Push request failed.");
            }

            var stkResponse = JsonSerializer.Deserialize<StkPushResponseDto>(resJson, JsonOpts);
            if (stkResponse is null)
                return new StkInitiateResult(false, null, null, null, "Invalid STK response.");

            if (stkResponse.ResponseCode != "0")
            {
                log.LogWarning("M-Pesa STK declined: {Desc}", stkResponse.ResponseDescription);
                return new StkInitiateResult(
                    false,
                    stkResponse.MerchantRequestID,
                    stkResponse.CheckoutRequestID,
                    stkResponse.CustomerMessage,
                    stkResponse.ResponseDescription ?? "STK request was not accepted.");
            }

            return new StkInitiateResult(
                true,
                stkResponse.MerchantRequestID,
                stkResponse.CheckoutRequestID,
                stkResponse.CustomerMessage,
                null);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "M-Pesa STK error");
            return new StkInitiateResult(false, null, null, null, ex.Message);
        }
    }

    private static string NormalizePhone(string raw)
    {
        var phone = raw.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (phone.StartsWith('+')) phone = phone[1..];
        if (phone.StartsWith('0')) phone = "254" + phone[1..];
        else if (!phone.StartsWith("254", StringComparison.Ordinal)) phone = "254" + phone;
        return phone;
    }

    private sealed class MpesaTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class StkPushResponseDto
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? CustomerMessage { get; set; }
    }
}
