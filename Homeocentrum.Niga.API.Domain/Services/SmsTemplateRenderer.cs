using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Homeocentrum.Niga.API.Domain.Data;

namespace Homeocentrum.Niga.API.Domain.Services;

public sealed class SmsTemplateRenderer : ISmsTemplateRenderer
{
    private readonly IServiceScopeFactory _scopes;

    public SmsTemplateRenderer(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task<string> RenderAsync(
        string templateCode,
        IReadOnlyDictionary<string, string?>? tokens = null,
        CancellationToken cancellationToken = default)
    {
        var code = (templateCode ?? "").Trim().ToUpperInvariant();
        var body = await LoadBodyAsync(code, cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
            body = CatalogBody(code) ?? "";
        return ApplyTokens(body, tokens);
    }

    private async Task<string?> LoadBodyAsync(string code, CancellationToken cancellationToken)
    {
        if (code.Length == 0) return null;
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
            var row = await db.Database.SqlQuery<TemplateBodyRow>($@"
                SELECT TOP 1 Body
                FROM dbo.SmsTemplate
                WHERE Code = {code} AND IsActive = 1").FirstOrDefaultAsync(cancellationToken);
            return row?.Body;
        }
        catch
        {
            return null;
        }
    }

    private static string? CatalogBody(string code)
    {
        foreach (var (c, body) in SmsTemplateCatalog.All)
        {
            if (string.Equals(c, code, StringComparison.OrdinalIgnoreCase))
                return body;
        }
        return null;
    }

    public static string ApplyTokens(string body, IReadOnlyDictionary<string, string?>? tokens)
    {
        if (string.IsNullOrEmpty(body) || tokens == null || tokens.Count == 0)
            return body ?? "";
        var text = body;
        foreach (var pair in tokens)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)) continue;
            var key = pair.Key.Trim().Trim('{', '}');
            var value = pair.Value ?? "";
            text = text
                .Replace("{" + key + "}", value, StringComparison.OrdinalIgnoreCase)
                .Replace("{" + key.ToLowerInvariant() + "}", value, StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }

    private sealed class TemplateBodyRow
    {
        public string? Body { get; set; }
    }
}
