using System.Net.Http.Json;
using Freshwalk.Application;
using Freshwalk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Freshwalk.Infrastructure.Services;

public class EmailService(IHttpClientFactory httpFactory, IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _opts = options.Value;

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient("Resend");
        var payload = new
        {
            from    = $"{_opts.FromName} <{_opts.FromAddress}>",
            to      = new[] { toEmail },
            subject = subject,
            html    = htmlBody
        };
        var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload, ct);
        response.EnsureSuccessStatusCode();
    }
}
