using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Security.Uploads;

public sealed record UploadVerdict(bool Allowed, string? Reason, string DetectedType)
{
    public static UploadVerdict Ok(string type) => new(true, null, type);
    public static UploadVerdict Block(string reason, string type = "unknown") => new(false, reason, type);
}

/// <summary>
/// Checks an uploaded file by its content, not its name: blocks executables and scripts, and requires the
/// extension to match the real file signature (magic bytes) for every type the platform accepts.
/// </summary>
public static class UploadGuard
{
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".com", ".scr", ".msi", ".msp", ".bat", ".cmd", ".ps1", ".psm1", ".psd1", ".vbs", ".vbe",
        ".js", ".jse", ".mjs", ".wsf", ".wsh", ".hta", ".cpl", ".jar", ".class", ".sh", ".bash", ".zsh", ".py", ".pl",
        ".rb", ".php", ".phtml", ".php5", ".asp", ".aspx", ".ashx", ".asmx", ".axd", ".cshtml", ".vbhtml", ".config",
        ".jsp", ".cgi", ".htm", ".html", ".xhtml", ".shtml", ".svg", ".svgz", ".xml", ".xsl", ".lnk", ".reg", ".inf",
        ".iso", ".img", ".vhd", ".vhdx", ".appx", ".msix", ".apk", ".app", ".dmg", ".deb", ".rpm", ".gadget", ".chm",
        ".docm", ".xlsm", ".pptm", ".dotm", ".xltm", ".xlam", ".ppam", ".sys", ".drv", ".ocx",
    };

    /// <summary>Extensions the product accepts, grouped by the content signature each must carry.</summary>
    private static readonly Dictionary<string, string[]> ExpectedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new[] { "jpeg" }, [".jpeg"] = new[] { "jpeg" }, [".jfif"] = new[] { "jpeg" },
        [".png"] = new[] { "png" }, [".gif"] = new[] { "gif" }, [".webp"] = new[] { "webp" },
        [".bmp"] = new[] { "bmp" }, [".tif"] = new[] { "tiff" }, [".tiff"] = new[] { "tiff" },
        [".heic"] = new[] { "heic" }, [".heif"] = new[] { "heic" }, [".ico"] = new[] { "ico" },
        [".pdf"] = new[] { "pdf" },
        [".docx"] = new[] { "zip" }, [".xlsx"] = new[] { "zip" }, [".pptx"] = new[] { "zip" }, [".zip"] = new[] { "zip" },
        [".doc"] = new[] { "ole" }, [".xls"] = new[] { "ole" }, [".ppt"] = new[] { "ole" },
        [".mp3"] = new[] { "mp3" }, [".wav"] = new[] { "wav" }, [".ogg"] = new[] { "ogg" }, [".oga"] = new[] { "ogg" },
        [".opus"] = new[] { "ogg" }, [".webm"] = new[] { "webm" }, [".weba"] = new[] { "webm" },
        [".m4a"] = new[] { "mp4" }, [".mp4"] = new[] { "mp4" }, [".mov"] = new[] { "mp4" }, [".aac"] = new[] { "aac", "mp4" },
        [".flac"] = new[] { "flac" }, [".amr"] = new[] { "amr" }, [".3gp"] = new[] { "mp4" },
        [".glb"] = new[] { "glb" }, [".gltf"] = new[] { "text" }, [".obj"] = new[] { "text" }, [".fbx"] = new[] { "binary", "text" },
        [".csv"] = new[] { "text" }, [".txt"] = new[] { "text" }, [".json"] = new[] { "text" }, [".md"] = new[] { "text" },
    };

    public static UploadVerdict Inspect(ReadOnlySpan<byte> head, string? fileName)
    {
        var name = Path.GetFileName(fileName ?? "");
        var ext = Path.GetExtension(name);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return UploadVerdict.Block("The file name is not valid.");
        if (head.Length == 0)
            return UploadVerdict.Block("The file is empty.");

        foreach (var part in name.Split('.').Skip(1))
        {
            if (BlockedExtensions.Contains("." + part.Trim()))
                return UploadVerdict.Block("This file type is not allowed.");
        }

        var detected = Detect(head);
        if (detected is "executable" or "script")
            return UploadVerdict.Block("Executable or script content is not allowed.", detected);

        if (string.IsNullOrEmpty(ext) || !ExpectedTypes.TryGetValue(ext, out var expected))
            return UploadVerdict.Block("This file type is not allowed.", detected);

        if (expected.Contains(detected))
            return UploadVerdict.Ok(detected);
        if (expected.Contains("binary") && detected != "text")
            return UploadVerdict.Ok(detected);
        // Browsers name recordings by habit rather than by codec (Safari records MP4 into "recording.webm").
        if (AudioExtensions.Contains(ext) && AudioContainers.Contains(detected))
            return UploadVerdict.Ok(detected);
        return UploadVerdict.Block("The file content does not match its extension.", detected);
    }

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".oga", ".opus", ".webm", ".weba", ".m4a", ".aac", ".flac", ".amr",
    };

    private static readonly HashSet<string> AudioContainers = new(StringComparer.Ordinal)
    {
        "mp3", "wav", "ogg", "webm", "mp4", "aac", "flac", "amr",
    };

    public static string Detect(ReadOnlySpan<byte> b)
    {
        if (StartsWith(b, 0x4D, 0x5A)) return "executable";                       // MZ (PE)
        if (StartsWith(b, 0x7F, 0x45, 0x4C, 0x46)) return "executable";           // ELF
        if (StartsWith(b, 0xCF, 0xFA, 0xED, 0xFE) || StartsWith(b, 0xCE, 0xFA, 0xED, 0xFE)
            || StartsWith(b, 0xFE, 0xED, 0xFA, 0xCE) || StartsWith(b, 0xFE, 0xED, 0xFA, 0xCF)) return "executable"; // Mach-O
        if (StartsWith(b, 0xCA, 0xFE, 0xBA, 0xBE)) return "executable";           // Java class / fat Mach-O
        if (StartsWith(b, 0x23, 0x21, 0x41, 0x4D, 0x52)) return "amr";            // #!AMR audio
        if (StartsWith(b, 0x23, 0x21)) return "script";                           // #! shebang

        if (StartsWith(b, 0xFF, 0xD8, 0xFF)) return "jpeg";
        if (StartsWith(b, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return "png";
        if (StartsWith(b, 0x47, 0x49, 0x46, 0x38)) return "gif";
        if (StartsWith(b, 0x52, 0x49, 0x46, 0x46) && b.Length >= 12)
        {
            if (b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "webp";
            if (b[8] == 0x57 && b[9] == 0x41 && b[10] == 0x56 && b[11] == 0x45) return "wav";
        }
        if (StartsWith(b, 0x42, 0x4D)) return "bmp";
        if (StartsWith(b, 0x49, 0x49, 0x2A, 0x00) || StartsWith(b, 0x4D, 0x4D, 0x00, 0x2A)) return "tiff";
        if (StartsWith(b, 0x00, 0x00, 0x01, 0x00)) return "ico";
        if (StartsWith(b, 0x25, 0x50, 0x44, 0x46, 0x2D)) return "pdf";
        if (StartsWith(b, 0x50, 0x4B, 0x03, 0x04) || StartsWith(b, 0x50, 0x4B, 0x05, 0x06)) return "zip";
        if (StartsWith(b, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)) return "ole";
        if (StartsWith(b, 0x49, 0x44, 0x33) || (b.Length > 1 && b[0] == 0xFF && (b[1] & 0xE0) == 0xE0 && (b[1] & 0x06) != 0)) return "mp3";
        if (b.Length > 1 && b[0] == 0xFF && (b[1] & 0xF6) == 0xF0) return "aac";
        if (StartsWith(b, 0x4F, 0x67, 0x67, 0x53)) return "ogg";
        if (StartsWith(b, 0x1A, 0x45, 0xDF, 0xA3)) return "webm";
        if (StartsWith(b, 0x66, 0x4C, 0x61, 0x43)) return "flac";
        if (StartsWith(b, 0x67, 0x6C, 0x54, 0x46)) return "glb";
        if (b.Length >= 12 && b[4] == 0x66 && b[5] == 0x74 && b[6] == 0x79 && b[7] == 0x70)
        {
            var brand = Encoding.ASCII.GetString(b.Slice(8, 4));
            return brand is "heic" or "heix" or "mif1" or "msf1" or "hevc" ? "heic" : "mp4";
        }

        if (LooksLikeText(b))
            return LooksLikeScript(b) ? "script" : "text";
        return "binary";
    }

    private static bool LooksLikeText(ReadOnlySpan<byte> b)
    {
        var sample = b.Length > 4096 ? b[..4096] : b;
        var control = 0;
        foreach (var c in sample)
        {
            if (c == 0) return false;
            if (c < 0x09 || (c > 0x0D && c < 0x20)) control++;
        }
        return control <= sample.Length / 50;
    }

    private static bool LooksLikeScript(ReadOnlySpan<byte> b)
    {
        var sample = Encoding.UTF8.GetString(b.Length > 4096 ? b[..4096] : b).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        var lower = sample.ToLowerInvariant();
        return lower.StartsWith("<?php") || lower.Contains("<script") || lower.StartsWith("<!doctype html")
            || lower.StartsWith("<html") || lower.StartsWith("<svg") || lower.Contains("<%@") || lower.Contains("<?xml-stylesheet")
            || lower.StartsWith("@echo off") || lower.Contains("powershell -") || lower.Contains("wscript.shell")
            || lower.Contains("javascript:") || lower.StartsWith("=cmd|") || lower.Contains("|'/c ")
            || LooksLikeCsvInjection(sample);
    }

    /// <summary>Spreadsheet formula injection: DDE/command formulas such as =cmd|' /c calc'!A0 or =HYPERLINK to a script.</summary>
    private static bool LooksLikeCsvInjection(string text)
    {
        foreach (var line in text.Split('\n').Take(200))
        {
            foreach (var cell in line.Split(',', ';', '\t'))
            {
                var c = cell.Trim().Trim('"').TrimStart();
                if (c.Length < 2) continue;
                if ((c[0] == '=' || c[0] == '+' || c[0] == '-' || c[0] == '@')
                    && DdeFormula.IsMatch(c))
                    return true;
            }
        }
        return false;
    }

    private static readonly System.Text.RegularExpressions.Regex DdeFormula = new(
        @"(?i)\b(cmd|msexcel|powershell|mshta|rundll32|regsvr32|certutil)\s*\||\bdde(auto)?\s*\(|\bwebservice\s*\(",
        System.Text.RegularExpressions.RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    private static bool StartsWith(ReadOnlySpan<byte> data, params byte[] sig)
        => data.Length >= sig.Length && data[..sig.Length].SequenceEqual(sig);

    /// <summary>Zip-based uploads (docx/xlsx/pptx/zip): no macros, no executables or scripts inside, no zip bombs.</summary>
    public static UploadVerdict InspectArchive(Stream content, long maxExpandedBytes = 200L * 1024 * 1024)
    {
        try
        {
            using var zip = new System.IO.Compression.ZipArchive(content, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            long expanded = 0;
            foreach (var entry in zip.Entries)
            {
                expanded += entry.Length;
                if (expanded > maxExpandedBytes || zip.Entries.Count > 20000)
                    return UploadVerdict.Block("The archive expands too much.", "zip");
                var entryName = entry.FullName.Replace('\\', '/');
                if (entryName.Contains("../") || entryName.StartsWith("/"))
                    return UploadVerdict.Block("The archive contains an unsafe path.", "zip");
                if (entryName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)
                    || entryName.Contains("/activeX/", StringComparison.OrdinalIgnoreCase))
                    return UploadVerdict.Block("Files with macros are not allowed.", "zip");
                var ext = Path.GetExtension(entryName);
                if (!string.IsNullOrEmpty(ext) && BlockedExtensions.Contains(ext) && !IsOfficeXmlPart(entryName))
                    return UploadVerdict.Block("The archive contains an executable or script.", "zip");
            }
            return UploadVerdict.Ok("zip");
        }
        catch (InvalidDataException)
        {
            return UploadVerdict.Block("The file is not a valid document.", "zip");
        }
    }

    /// <summary>Office Open XML documents are made of .xml parts; those are expected inside docx/xlsx/pptx.</summary>
    private static bool IsOfficeXmlPart(string entryName)
        => entryName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entryName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase);

    /// <summary>Random stored name that keeps only a safe, lower-case extension. Never use the client's file name on disk.</summary>
    public static string RandomStoredName(string? originalName)
    {
        var ext = Path.GetExtension(Path.GetFileName(originalName ?? "")).ToLowerInvariant();
        if (ext.Length > 10 || ext.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.'))
            ext = "";
        return Guid.NewGuid().ToString("N") + ext;
    }
}
