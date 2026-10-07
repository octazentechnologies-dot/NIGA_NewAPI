using System.Net;
using System.Security.Cryptography;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Security.Audit;
using Homeocentrum.Niga.NewAPI.Domain.Security.Uploads;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.Security;

public class UploadGuardTests
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46 };
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%\u00e2\u00e3");
    private static readonly byte[] PeExecutable = { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
    private static readonly byte[] Elf = { 0x7F, 0x45, 0x4C, 0x46, 0x02, 0x01 };

    [Theory]
    [InlineData("photo.png")]
    [InlineData("PHOTO.PNG")]
    public void Real_png_is_allowed(string name) => Assert.True(UploadGuard.Inspect(Png, name).Allowed);

    [Fact]
    public void Real_jpeg_and_pdf_are_allowed()
    {
        Assert.True(UploadGuard.Inspect(Jpeg, "x.jpg").Allowed);
        Assert.True(UploadGuard.Inspect(Pdf, "report.pdf").Allowed);
    }

    [Theory]
    [InlineData("invoice.jpg")]
    [InlineData("report.pdf")]
    [InlineData("scan.png")]
    public void Windows_executable_renamed_to_a_safe_extension_is_blocked(string name)
    {
        var verdict = UploadGuard.Inspect(PeExecutable, name);
        Assert.False(verdict.Allowed);
        Assert.Equal("executable", verdict.DetectedType);
    }

    [Fact]
    public void Linux_executable_is_blocked() => Assert.False(UploadGuard.Inspect(Elf, "a.pdf").Allowed);

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("run.bat")]
    [InlineData("x.ps1")]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("shell.php")]
    [InlineData("report.pdf.exe")]
    [InlineData("photo.php.jpg")]
    [InlineData("macro.docm")]
    public void Blocked_extensions_are_rejected_even_with_valid_content(string name)
        => Assert.False(UploadGuard.Inspect(Png, name).Allowed);

    [Fact]
    public void Content_that_does_not_match_extension_is_rejected()
    {
        var verdict = UploadGuard.Inspect(Png, "document.pdf");
        Assert.False(verdict.Allowed);
        Assert.Equal("png", verdict.DetectedType);
    }

    [Fact]
    public void Safari_mp4_recording_named_webm_is_allowed()
    {
        byte[] mp4Audio = { 0, 0, 0, 0x20, 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41, 0x20 };
        Assert.True(UploadGuard.Inspect(mp4Audio, "recording.webm").Allowed);
        Assert.False(UploadGuard.Inspect(Png, "recording.webm").Allowed);
        Assert.False(UploadGuard.Inspect(PeExecutable, "recording.webm").Allowed);
    }

    [Theory]
    [InlineData("<?php system($_GET['c']); ?>")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("#!/bin/sh\nrm -rf /")]
    [InlineData("@echo off\r\ndel *.*")]
    [InlineData("name,amount\n=cmd|' /c calc'!A0,1")]
    public void Scripts_disguised_as_text_are_blocked(string body)
        => Assert.False(UploadGuard.Inspect(Encoding.UTF8.GetBytes(body), "notes.txt").Allowed);

    [Fact]
    public void Plain_csv_is_allowed()
        => Assert.True(UploadGuard.Inspect(Encoding.UTF8.GetBytes("name,mobile\nRam,-\nSita,10"), "patients.csv").Allowed);

    [Fact]
    public void Unknown_extension_and_bad_names_are_rejected()
    {
        Assert.False(UploadGuard.Inspect(Png, "image.xyz").Allowed);
        Assert.False(UploadGuard.Inspect(Png, "").Allowed);
        Assert.False(UploadGuard.Inspect(Array.Empty<byte>(), "empty.png").Allowed);
    }

    [Fact]
    public void Archive_with_macro_or_executable_is_blocked()
    {
        Assert.False(UploadGuard.InspectArchive(Zip(("word/vbaProject.bin", "x"))).Allowed);
        Assert.False(UploadGuard.InspectArchive(Zip(("payload/run.exe", "MZ"))).Allowed);
        Assert.False(UploadGuard.InspectArchive(Zip(("../../evil.txt", "x"))).Allowed);
        Assert.True(UploadGuard.InspectArchive(Zip(("word/document.xml", "<w/>"), ("_rels/.rels", "<r/>"))).Allowed);
    }

    [Fact]
    public void Garbage_with_docx_extension_is_rejected()
        => Assert.False(UploadGuard.InspectArchive(new MemoryStream(Encoding.ASCII.GetBytes("PK\u0003\u0004not a zip"))).Allowed);

    [Fact]
    public void Stored_name_is_random_and_keeps_only_a_safe_extension()
    {
        var a = UploadGuard.RandomStoredName("../../etc/Report.PDF");
        var b = UploadGuard.RandomStoredName("../../etc/Report.PDF");
        Assert.NotEqual(a, b);
        Assert.EndsWith(".pdf", a);
        Assert.DoesNotContain("Report", a);
        Assert.DoesNotContain("/", a);
        Assert.Equal(32, UploadGuard.RandomStoredName("x.p$p").Length);
    }

    private static MemoryStream Zip(params (string Name, string Body)[] entries)
    {
        var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, body) in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open());
                w.Write(body);
            }
        }
        ms.Position = 0;
        return ms;
    }
}

public class SecurityAuditRowTests
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("unit-test-audit-key");
    private static readonly DateTime At = new(2026, 10, 7, 9, 30, 15, 123, DateTimeKind.Utc);

    private static SecurityAuditRow Row(string outcome = "SUCCESS", string? detail = "POST /api/Account/Login -> 200")
        => SecurityAuditRow.Create(At.AddTicks(4567), "LOGIN", outcome, 10032, "Doctor", "user:Tufan_Doctor", "NewAPI",
            "10.29.1.0/24", "corr-1", detail, "tk1");

    [Fact]
    public void Canonical_format_is_stable()
    {
        Assert.Equal("v1|2026-10-07T09:30:15.123Z|LOGIN|SUCCESS|10032|Doctor|user:Tufan_Doctor|NewAPI|10.29.1.0/24|corr-1|POST /api/Account/Login -> 200|tk1",
            Row().Canonical);
    }

    [Fact]
    public void Pipes_and_newlines_cannot_forge_extra_fields()
    {
        var row = Row(detail: "a|b\r\nc");
        Assert.EndsWith("|a/b  c|tk1", row.Canonical);
        Assert.Equal(12, row.Canonical.Split('|').Length);
    }

    [Fact]
    public void Mac_changes_when_any_field_changes()
    {
        var mac = SecurityAuditRow.ComputeMac(Key, Row().Canonical);
        Assert.Equal(64, mac.Length);
        Assert.NotEqual(mac, SecurityAuditRow.ComputeMac(Key, Row(outcome: "DENIED").Canonical));
        Assert.NotEqual(mac, SecurityAuditRow.ComputeMac(Encoding.UTF8.GetBytes("other-key"), Row().Canonical));
    }

    [Fact]
    public void Row_hash_matches_the_database_formula()
    {
        var canonical = Row().Canonical;
        var mac = SecurityAuditRow.ComputeMac(Key, canonical);
        var prev = new string('0', 64);
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(prev + "|" + canonical + "|" + mac)));
        Assert.Equal(expected, SecurityAuditRow.ComputeRowHash(prev, canonical, mac));
    }

    [Fact]
    public void Editing_a_middle_row_breaks_the_chain()
    {
        var chain = new List<(string Canonical, string Mac, string Prev, string Hash)>();
        var prev = new string('0', 64);
        foreach (var outcome in new[] { "SUCCESS", "DENIED", "SUCCESS" })
        {
            var c = Row(outcome).Canonical;
            var m = SecurityAuditRow.ComputeMac(Key, c);
            var h = SecurityAuditRow.ComputeRowHash(prev, c, m);
            chain.Add((c, m, prev, h));
            prev = h;
        }

        var tampered = chain[1].Canonical.Replace("|DENIED|", "|SUCCESS|");
        Assert.NotEqual(chain[1].Mac, SecurityAuditRow.ComputeMac(Key, tampered));
        var forgedMac = SecurityAuditRow.ComputeMac(Key, tampered);
        var forgedHash = SecurityAuditRow.ComputeRowHash(chain[1].Prev, tampered, forgedMac);
        Assert.NotEqual(chain[2].Prev, forgedHash);
    }

    [Fact]
    public void Ip_is_masked_to_network()
    {
        Assert.Equal("10.29.1.0/24", SecurityAuditLog.MaskIp(IPAddress.Parse("10.29.1.142")));
        Assert.Equal("10.29.1.0/24", SecurityAuditLog.MaskIp(IPAddress.Parse("::ffff:10.29.1.142")));
        Assert.Equal("2001:db8:1234::/48", SecurityAuditLog.MaskIp(IPAddress.Parse("2001:db8:1234:5678::1")));
    }

    [Fact]
    public void Keys_derive_from_token_key_and_keep_retired_keys()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TokenKey"] = "token-key-for-tests",
            ["SecurityAudit:HmacKey"] = "new-dedicated-key",
            ["SecurityAudit:KeyId"] = "k2",
            ["SecurityAudit:PreviousKeys:k1"] = "old-dedicated-key",
        }).Build();
        var keys = SecurityAuditKeys.From(config);
        Assert.Equal("k2", keys.CurrentKeyId);
        Assert.True(keys.TryGet("k1", out _));
        Assert.True(keys.TryGet("tk1", out var derived));
        Assert.Equal(HMACSHA256.HashData(Encoding.UTF8.GetBytes("token-key-for-tests"), Encoding.UTF8.GetBytes("homeocentrum-security-audit-v1")), derived);
    }

    [Fact]
    public void Retired_token_key_still_verifies_old_tk1_rows()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TokenKey"] = "rotated-token-key",
            ["SecurityAudit:HmacKey"] = "dedicated-key",
            ["SecurityAudit:KeyId"] = "k1",
            ["SecurityAudit:PreviousKeys:tk1"] = "token-key-before-rotation",
        }).Build();
        var keys = SecurityAuditKeys.From(config);
        Assert.Equal("k1", keys.CurrentKeyId);
        Assert.True(keys.TryGet("tk1", out var tk1));
        Assert.Equal(HMACSHA256.HashData(Encoding.UTF8.GetBytes("token-key-before-rotation"), Encoding.UTF8.GetBytes("homeocentrum-security-audit-v1")), tk1);
    }

    [Fact]
    public void Missing_keys_fail_fast()
        => Assert.Throws<InvalidOperationException>(() => SecurityAuditKeys.From(new ConfigurationBuilder().Build()));
}

public class RazorpayPaymentVerifierTests
{
    private const string Secret = "unit_test_secret";

    private static RazorpayPaymentVerifier Verifier(string? secret = Secret) => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Razorpay:KeyId"] = "rzp_test_unit",
            ["Razorpay:KeySecret"] = secret,
        }).Build(),
        new NoHttpClientFactory());

    private static string Sign(string order, string payment)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(order + "|" + payment))).ToLowerInvariant();

    [Fact]
    public void Valid_signature_is_accepted() => Assert.True(Verifier().SignatureValid("order_1", "pay_1", Sign("order_1", "pay_1")));

    [Fact]
    public void Signature_for_another_payment_is_rejected()
    {
        Assert.False(Verifier().SignatureValid("order_1", "pay_2", Sign("order_1", "pay_1")));
        Assert.False(Verifier().SignatureValid("order_2", "pay_1", Sign("order_1", "pay_1")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-hex")]
    public void Missing_or_malformed_signature_is_rejected(string? signature)
        => Assert.False(Verifier().SignatureValid("order_1", "pay_1", signature));

    [Fact]
    public void Unconfigured_verifier_rejects_everything()
    {
        var v = Verifier(secret: "");
        Assert.False(v.Configured);
        Assert.False(v.SignatureValid("order_1", "pay_1", Sign("order_1", "pay_1")));
    }

    private sealed class NoHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("No network in unit tests.");
    }
}

public class LogRedactorTests
{
    [Fact]
    public void Jwt_and_bearer_tokens_are_removed()
    {
        var jwt = "eyJhbGciOiJIUzI1NiJ9." + "eyJzdWIiOiIxMDAzMiJ9." + "c2lnbmF0dXJlLXZhbHVl";
        var text = LogRedactor.Redact("Authorization: Bearer " + jwt + " token=" + jwt);
        Assert.DoesNotContain(jwt, text);
        Assert.DoesNotContain("eyJzdWIi", text);
    }

    [Theory]
    [InlineData("Your OTP is 482913", "482913")]
    [InlineData("verification code: 7731", "7731")]
    [InlineData("GET /api/Otp/Verify/9876543210/554433", "554433")]
    public void Otps_are_removed(string input, string otp) => Assert.DoesNotContain(otp, LogRedactor.Redact(input));

    [Theory]
    [InlineData("call 9876543210 now")]
    [InlineData("mobile +91 9876543210")]
    [InlineData("mobile=+91-9876543210")]
    public void Indian_mobile_numbers_are_masked(string input)
    {
        var text = LogRedactor.Redact(input);
        Assert.DoesNotContain("9876543210", text);
        Assert.DoesNotContain("98765432", text);
    }

    [Fact]
    public void Emails_and_secret_values_are_masked()
    {
        var text = LogRedactor.Redact("user ramesh.kumar@example.com password=Hunter2! \"apiKey\":\"abc123secret\"");
        Assert.DoesNotContain("ramesh.kumar@", text);
        Assert.DoesNotContain("Hunter2!", text);
        Assert.DoesNotContain("abc123secret", text);
    }

    [Fact]
    public void Ordinary_text_is_untouched()
        => Assert.Equal("Patient 3046 saved by doctor 1010", LogRedactor.Redact("Patient 3046 saved by doctor 1010"));
}

public class LoginThrottleTests
{
    // The throttle is process-wide, so every test uses its own user name and IP.
    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public void Fifth_failure_for_a_user_blocks_that_user()
    {
        var user = Unique("u");
        var now = DateTime.UtcNow;
        for (var i = 0; i < LoginThrottle.MaxFailuresPerUser - 1; i++)
            LoginThrottle.RecordFailure(user, Unique("ip"), now);
        Assert.Null(LoginThrottle.BlockedFor(user, Unique("ip"), now));

        LoginThrottle.RecordFailure(user, Unique("ip"), now);
        Assert.NotNull(LoginThrottle.BlockedFor(user.ToUpperInvariant(), Unique("ip"), now));
    }

    [Fact]
    public void Block_expires_after_the_block_period()
    {
        var user = Unique("u");
        var now = DateTime.UtcNow;
        for (var i = 0; i < LoginThrottle.MaxFailuresPerUser; i++)
            LoginThrottle.RecordFailure(user, null, now);
        Assert.Null(LoginThrottle.BlockedFor(user, null, now + LoginThrottle.Block + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Failures_outside_the_window_do_not_add_up()
    {
        var user = Unique("u");
        var now = DateTime.UtcNow;
        for (var i = 0; i < LoginThrottle.MaxFailuresPerUser - 1; i++)
            LoginThrottle.RecordFailure(user, null, now);
        var later = now + LoginThrottle.Window + TimeSpan.FromMinutes(1);
        LoginThrottle.RecordFailure(user, null, later);
        Assert.Null(LoginThrottle.BlockedFor(user, null, later));
    }

    [Fact]
    public void Success_clears_the_user_counter()
    {
        var user = Unique("u");
        var now = DateTime.UtcNow;
        for (var i = 0; i < LoginThrottle.MaxFailuresPerUser - 1; i++)
            LoginThrottle.RecordFailure(user, null, now);
        LoginThrottle.RecordSuccess(user);
        LoginThrottle.RecordFailure(user, null, now);
        Assert.Null(LoginThrottle.BlockedFor(user, null, now));
    }

    [Fact]
    public void Many_user_names_from_one_ip_block_the_ip()
    {
        var ip = Unique("ip");
        var now = DateTime.UtcNow;
        for (var i = 0; i < LoginThrottle.MaxFailuresPerIp; i++)
            LoginThrottle.RecordFailure(Unique("u"), ip, now);
        Assert.NotNull(LoginThrottle.BlockedFor(Unique("u"), ip, now));
        Assert.Null(LoginThrottle.BlockedFor(Unique("u"), Unique("ip"), now));
    }
}
