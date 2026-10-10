using System.Security.Cryptography;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.Privacy;

/// <summary>Why a consent does or does not currently count (DPDP Act 2023 s.6 and s.9).</summary>
public enum ConsentState
{
    Valid,
    None,
    Withdrawn,
    /// <summary>A notice version that requires fresh consent was published after this consent.</summary>
    NoticeChanged,
    /// <summary>The patient is under 18 and the consent was not given by a parent or guardian.</summary>
    GuardianRequired,
    /// <summary>A guardian consented while the patient was a child; the patient is now an adult and must consent.</summary>
    MajorityReached,
}

public static class GuardianMethods
{
    /// <summary>The signed-in adult owns the child's family record or holds an active caregiver grant.</summary>
    public const string FamilyAccount = "FamilyAccount";

    /// <summary>Clinic staff checked the adult's identity document in person.</summary>
    public const string InClinic = "InClinic";

    /// <summary>The adult proved control of their mobile with an OTP (Action GuardianConsent, EntityType Patient).</summary>
    public const string Otp = "Otp";

    public const string OtpAction = "GuardianConsent";

    public static readonly IReadOnlyList<string> All = new[] { FamilyAccount, InClinic, Otp };

    /// <summary>Identity documents staff may check. Only the type is stored, never the number.</summary>
    public static readonly IReadOnlyList<string> IdProofTypes = new[]
    {
        "Aadhaar", "PAN", "Passport", "DrivingLicence", "VoterId", "BirthCertificate", "Other"
    };

    public static string? Normalize(string? method)
        => All.FirstOrDefault(m => string.Equals(m, method?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string? NormalizeIdProof(string? type)
        => IdProofTypes.FirstOrDefault(t => string.Equals(t, type?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class ConsentRules
{
    public const int AgeOfMajority = 18;

    /// <summary>Age in whole years from date of birth, else the recorded age; null when neither is known.</summary>
    public static int? AgeOn(DateTime? dateOfBirth, int? recordedAge, DateTime today)
    {
        if (dateOfBirth.HasValue)
        {
            var dob = dateOfBirth.Value.Date;
            if (dob > today.Date)
                return 0;
            var age = today.Year - dob.Year;
            if (dob > today.Date.AddYears(-age))
                age--;
            return age;
        }
        return recordedAge is >= 0 ? recordedAge : null;
    }

    /// <summary>True, false, or null when the age is unknown.</summary>
    public static bool? IsMinor(DateTime? dateOfBirth, int? recordedAge, DateTime today)
    {
        var age = AgeOn(dateOfBirth, recordedAge, today);
        return age.HasValue ? age.Value < AgeOfMajority : null;
    }

    public static bool NeedsReconsent(DateTime grantedAtUtc, IEnumerable<DateTime> reconsentEffectiveFromUtc, DateTime nowUtc)
        => reconsentEffectiveFromUtc.Any(e => e > grantedAtUtc && e <= nowUtc);

    public static ConsentState Evaluate(
        bool hasRecord,
        DateTime grantedAtUtc,
        DateTime? withdrawnAtUtc,
        bool grantedForMinor,
        bool? subjectIsMinorNow,
        IEnumerable<DateTime> reconsentEffectiveFromUtc,
        DateTime nowUtc)
    {
        if (!hasRecord)
            return ConsentState.None;
        if (withdrawnAtUtc.HasValue)
            return ConsentState.Withdrawn;
        if (NeedsReconsent(grantedAtUtc, reconsentEffectiveFromUtc, nowUtc))
            return ConsentState.NoticeChanged;
        if (subjectIsMinorNow == true && !grantedForMinor)
            return ConsentState.GuardianRequired;
        if (subjectIsMinorNow == false && grantedForMinor)
            return ConsentState.MajorityReached;
        return ConsentState.Valid;
    }

    /// <summary>Lower-case hex SHA-256 of the UTF-8 notice text, exactly as stored.</summary>
    public static string NoticeSha256(string body)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
}
