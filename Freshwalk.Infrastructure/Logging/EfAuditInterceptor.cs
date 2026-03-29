using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Freshwalk.Infrastructure.Logging;

/// <summary>Appends SQL command text for the current HTTP request audit file.</summary>
public sealed class EfAuditInterceptor(IHttpContextAccessor httpContextAccessor) : DbCommandInterceptor
{
    private const string Key = "audit_sql";

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Append(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Append(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Append(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Append(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Append(DbCommand command)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return;

        if (!ctx.Items.TryGetValue(Key, out var existing) || existing is not List<string> lines)
        {
            lines = new List<string>();
            ctx.Items[Key] = lines;
        }

        var text = command.CommandText;
        if (command.Parameters.Count > 0)
        {
            var p = string.Join(", ", command.Parameters.Cast<DbParameter>().Select(x => $"{x.ParameterName}={x.Value}"));
            text += "\n  -- " + p;
        }

        lines.Add(text);
    }
}
