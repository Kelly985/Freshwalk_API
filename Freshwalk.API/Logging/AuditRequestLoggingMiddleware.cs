using System.Text;
using System.Text.Json;
using Freshwalk.API.Configuration;
using Freshwalk.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Freshwalk.API.Logging;

/// <summary>
/// Writes one text file per request under RootPath/yyyy/MM/dd/source/userKey/.
/// Expects clients to send <c>X-Client-Source</c> (e.g. customer-web, agent-portal, rider-portal).
/// </summary>
public sealed class AuditRequestLoggingMiddleware : IMiddleware
{
    private const string SqlKey = "audit_sql";
    private readonly AuditLoggingOptions _options;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<AuditRequestLoggingMiddleware> _logger;

    public AuditRequestLoggingMiddleware(
        IOptions<AuditLoggingOptions> options,
        UserManager<ApplicationUser> users,
        ILogger<AuditRequestLoggingMiddleware> logger)
    {
        _options = options.Value;
        _users = users;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.RootPath))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        var correlationId = Guid.NewGuid().ToString("N");
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        string requestBody = "";
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;
        }
        catch
        {
            requestBody = "(could not read body)";
        }

        var originalBody = context.Response.Body;
        await using var mem = new MemoryStream();
        context.Response.Body = mem;

        Exception? caught = null;
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            caught = ex;
            throw;
        }
        finally
        {
            mem.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(mem, Encoding.UTF8).ReadToEndAsync(context.RequestAborted);
            mem.Seek(0, SeekOrigin.Begin);
            await mem.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;

            try
            {
                await WriteAuditFileAsync(
                    context,
                    correlationId,
                    requestBody,
                    responseText,
                    caught);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log file for {CorrelationId}", correlationId);
            }
        }
    }

    private async Task WriteAuditFileAsync(
        HttpContext context,
        string correlationId,
        string requestBody,
        string responseBody,
        Exception? error)
    {
        var root = Path.GetFullPath(_options.RootPath);
        var now = DateTimeOffset.UtcNow;
        var source = SanitizeFolder(context.Request.Headers.TryGetValue("X-Client-Source", out StringValues sv)
            ? sv.ToString()
            : "unknown");
        var userKey = await ResolveUserFolderAsync(context, requestBody);

        var dir = Path.Combine(root, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"), source, userKey);
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{correlationId}.log");

        var sb = new StringBuilder(4096);
        sb.AppendLine("=== REQUEST ===");
        sb.AppendLine($"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
        sb.AppendLine($"CorrelationId: {correlationId}");
        sb.AppendLine("Headers:");
        foreach (var h in context.Request.Headers.Where(x => !string.Equals(x.Key, "Authorization", StringComparison.OrdinalIgnoreCase)))
            sb.AppendLine($"  {h.Key}: {h.Value}");
        sb.AppendLine("(Authorization header omitted)");
        sb.AppendLine();
        sb.AppendLine("Body:");
        sb.AppendLine(Truncate(requestBody, 50_000));
        sb.AppendLine();

        if (context.Items.TryGetValue(SqlKey, out var sqlObj) && sqlObj is List<string> sqlLines && sqlLines.Count > 0)
        {
            sb.AppendLine("=== DATABASE COMMANDS ===");
            foreach (var line in sqlLines)
            {
                sb.AppendLine(line);
                sb.AppendLine("---");
            }

            sb.AppendLine();
        }

        sb.AppendLine("=== RESPONSE ===");
        sb.AppendLine($"Status: {context.Response.StatusCode}");
        if (error is not null)
        {
            sb.AppendLine("Exception:");
            sb.AppendLine(error.ToString());
            sb.AppendLine();
        }

        sb.AppendLine("Body:");
        sb.AppendLine(Truncate(responseBody, 50_000));

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, context.RequestAborted);
    }

    private async Task<string> ResolveUserFolderAsync(HttpContext context, string requestBody)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                        ?? context.User.FindFirst("sub");
            if (claim is not null && Guid.TryParse(claim.Value, out var uid))
            {
                var u = await _users.FindByIdAsync(uid.ToString());
                if (u is not null)
                {
                    var userPhone = NormalizeDigits(u.PhoneNumber);
                    if (!string.IsNullOrEmpty(userPhone))
                        return SanitizeFolder("phone_" + userPhone);
                    if (!string.IsNullOrEmpty(u.Email))
                        return SanitizeFolder("email_" + u.Email.Replace("@", "_at_"));
                }
            }
        }

        TryParseAnonymousIdentifiers(requestBody, out var email, out var phone);
        if (!string.IsNullOrEmpty(phone))
            return SanitizeFolder("phone_" + phone);
        if (!string.IsNullOrEmpty(email))
            return SanitizeFolder("email_" + email.Replace("@", "_at_"));
        return "anonymous";
    }

    private static void TryParseAnonymousIdentifiers(string json, out string? email, out string? phone)
    {
        email = null;
        phone = null;
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("email", out var e) && e.ValueKind == JsonValueKind.String)
                email = e.GetString();
            if (root.TryGetProperty("phoneNumber", out var p) && p.ValueKind == JsonValueKind.String)
                phone = NormalizeDigits(p.GetString());
            if (phone is null && root.TryGetProperty("mobileNumber", out var m) && m.ValueKind == JsonValueKind.String)
                phone = NormalizeDigits(m.GetString());
            if (phone is null && root.TryGetProperty("PhoneNumber", out var p2) && p2.ValueKind == JsonValueKind.String)
                phone = NormalizeDigits(p2.GetString());
            if (phone is null && root.TryGetProperty("MobileNumber", out var m2) && m2.ValueKind == JsonValueKind.String)
                phone = NormalizeDigits(m2.GetString());
        }
        catch
        {
            /* ignore */
        }
    }

    private static string? NormalizeDigits(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var d = new string(raw.Where(char.IsDigit).ToArray());
        return d.Length == 0 ? null : d;
    }

    private static string SanitizeFolder(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(invalid.Contains(c) ? '_' : c);
        var s = sb.ToString();
        return string.IsNullOrWhiteSpace(s) ? "unknown" : s[..Math.Min(s.Length, 120)];
    }

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max) return s;
        return s[..max] + "\n... (truncated)";
    }
}
