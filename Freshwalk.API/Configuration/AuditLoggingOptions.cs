namespace Freshwalk.API.Configuration;

public class AuditLoggingOptions
{
    public const string SectionName = "AuditLogging";

    /// <summary>Root directory for per-request audit files (year/month/day/source/user).</summary>
    public string RootPath { get; set; } = "";

    public bool Enabled { get; set; } = true;
}
