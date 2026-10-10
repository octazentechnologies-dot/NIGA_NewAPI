using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Privacy;
using Homeocentrum.Niga.API.Domain.Security;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests.Security;

public class ConsentRulesTests
{
    private static readonly DateTime Today = new(2026, 10, 7);
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("2008-10-07", false)]
    [InlineData("2008-10-08", true)]
    [InlineData("2015-01-01", true)]
    [InlineData("1990-05-20", false)]
    public void Minor_is_decided_by_date_of_birth(string dob, bool minor)
        => Assert.Equal(minor, ConsentRules.IsMinor(DateTime.Parse(dob), null, Today));

    [Fact]
    public void Date_of_birth_wins_over_recorded_age()
        => Assert.False(ConsentRules.IsMinor(new DateTime(1990, 1, 1), 12, Today));

    [Theory]
    [InlineData(17, true)]
    [InlineData(18, false)]
    public void Recorded_age_is_used_without_date_of_birth(int age, bool minor)
        => Assert.Equal(minor, ConsentRules.IsMinor(null, age, Today));

    [Fact]
    public void Unknown_age_is_not_assumed_adult_or_minor()
        => Assert.Null(ConsentRules.IsMinor(null, null, Today));

    [Fact]
    public void Withdrawn_consent_does_not_count()
        => Assert.Equal(ConsentState.Withdrawn,
            ConsentRules.Evaluate(true, Now.AddDays(-5), Now.AddDays(-1), false, false, Array.Empty<DateTime>(), Now));

    [Fact]
    public void Notice_requiring_reconsent_after_grant_invalidates_it()
        => Assert.Equal(ConsentState.NoticeChanged,
            ConsentRules.Evaluate(true, Now.AddDays(-10), null, false, false, new[] { Now.AddDays(-2) }, Now));

    [Fact]
    public void Notice_published_before_grant_or_in_future_does_not_invalidate()
    {
        var dates = new[] { Now.AddDays(-20), Now.AddDays(3) };
        Assert.Equal(ConsentState.Valid, ConsentRules.Evaluate(true, Now.AddDays(-10), null, false, false, dates, Now));
    }

    [Fact]
    public void Minor_self_consent_needs_guardian()
        => Assert.Equal(ConsentState.GuardianRequired,
            ConsentRules.Evaluate(true, Now.AddDays(-1), null, false, true, Array.Empty<DateTime>(), Now));

    [Fact]
    public void Guardian_consent_is_valid_while_patient_is_minor()
        => Assert.Equal(ConsentState.Valid,
            ConsentRules.Evaluate(true, Now.AddDays(-1), null, true, true, Array.Empty<DateTime>(), Now));

    [Fact]
    public void Guardian_consent_lapses_when_patient_turns_18()
        => Assert.Equal(ConsentState.MajorityReached,
            ConsentRules.Evaluate(true, Now.AddYears(-2), null, true, false, Array.Empty<DateTime>(), Now));

    [Fact]
    public void Notice_hash_matches_sql_server_hashbytes_format()
    {
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", ConsentRules.NoticeSha256("abc"));
        Assert.Matches("^[0-9a-f]{64}$", ConsentRules.NoticeSha256("Line one\nLine two"));
    }

    [Theory]
    [InlineData("familyaccount", GuardianMethods.FamilyAccount)]
    [InlineData(" InClinic ", GuardianMethods.InClinic)]
    [InlineData("OTP", GuardianMethods.Otp)]
    [InlineData("Selfie", null)]
    [InlineData(null, null)]
    public void Guardian_method_is_normalised_to_known_values(string? input, string? expected)
        => Assert.Equal(expected, GuardianMethods.Normalize(input));

    [Fact]
    public void Id_proof_type_must_be_on_the_list()
    {
        Assert.Equal("Aadhaar", GuardianMethods.NormalizeIdProof("aadhaar"));
        Assert.Null(GuardianMethods.NormalizeIdProof("1234-5678-9012"));
    }
}

public class ReceptionStaffPasswordTests
{
    [Fact]
    public void New_passwords_are_pbkdf2_and_not_reversible()
    {
        var stored = ReceptionStaffPasswordHelper.HashPassword("Clinic@2026");
        Assert.True(UserPasswordHasher.IsWellFormedHash(stored));
        Assert.DoesNotContain(CommonMethods.Encoding("Clinic@2026"), stored);
        Assert.False(ReceptionStaffPasswordHelper.NeedsRehash(stored));
        Assert.True(ReceptionStaffPasswordHelper.VerifyPassword("Clinic@2026", stored));
        Assert.False(ReceptionStaffPasswordHelper.VerifyPassword("clinic@2026", stored));
    }

    [Fact]
    public void Legacy_encoded_password_still_verifies_and_needs_rehash()
    {
        var legacy = CommonMethods.Encoding("Front#Desk1");
        Assert.True(ReceptionStaffPasswordHelper.NeedsRehash(legacy));
        Assert.True(ReceptionStaffPasswordHelper.VerifyPassword("Front#Desk1", legacy));
        Assert.False(ReceptionStaffPasswordHelper.VerifyPassword("wrong", legacy));
        Assert.Equal("Front#Desk1", ReceptionStaffPasswordHelper.LegacyPlaintext(legacy));
    }

    [Fact]
    public void Legacy_plaintext_password_still_verifies()
    {
        Assert.True(ReceptionStaffPasswordHelper.VerifyPassword("plain-pass!", "plain-pass!"));
        Assert.Equal("plain-pass!", ReceptionStaffPasswordHelper.LegacyPlaintext("plain-pass!"));
    }

    [Fact]
    public void Migrated_legacy_value_verifies_with_the_original_password()
    {
        var migrated = ReceptionStaffPasswordHelper.HashPassword(
            ReceptionStaffPasswordHelper.LegacyPlaintext(CommonMethods.Encoding("123456")));
        Assert.True(ReceptionStaffPasswordHelper.VerifyPassword("123456", migrated));
    }

    [Fact]
    public void Truncated_hash_never_falls_back_to_plaintext_compare()
    {
        var truncated = ReceptionStaffPasswordHelper.HashPassword("abc12345")[..40];
        Assert.False(ReceptionStaffPasswordHelper.VerifyPassword(truncated, truncated));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_stored_value_never_verifies(string? stored)
        => Assert.False(ReceptionStaffPasswordHelper.VerifyPassword("x", stored));
}
