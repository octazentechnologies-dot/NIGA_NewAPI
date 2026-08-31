using System.Threading;
using System.Threading.Tasks;

namespace Niga_Domain.Interfaces;

public interface IWhatsAppMetaApiClient
{
    Task<(bool Success, string? MediaId, string? ErrorMessage)> UploadImageAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendTextMessageAsync(
        string recipientNumber,
        string messageText,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendImageMessageAsync(
        string recipientNumber,
        string mediaId,
        string caption,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendTemplateMessageAsync(
        string recipientNumber,
        string metaTemplateName,
        string languageCode,
        IReadOnlyList<string>? bodyParameters = null,
        CancellationToken cancellationToken = default);
}
