namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Resolves SMS body from dbo.SmsTemplate (clinic-edited) with catalog fallback, then fills {placeholders}.
/// </summary>
public interface ISmsTemplateRenderer
{
    Task<string> RenderAsync(
        string templateCode,
        IReadOnlyDictionary<string, string?>? tokens = null,
        CancellationToken cancellationToken = default);
}
