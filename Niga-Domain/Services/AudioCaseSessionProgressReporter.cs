using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services;

public class AudioCaseSessionProgressReporter : IAudioCaseSessionProgressReporter
{
    private readonly NIGACentrumContext _context;

    public AudioCaseSessionProgressReporter(NIGACentrumContext context) => _context = context;

    public async Task ReportAsync(
        Guid sessionId,
        string progressStep,
        int? percentHint = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.AudioCaseSessions
            .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId && !x.DeleteStatus, cancellationToken);
        if (session == null) return;

        session.Status = "Processing";
        session.CurrentStep = progressStep;
        session.ChangedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
