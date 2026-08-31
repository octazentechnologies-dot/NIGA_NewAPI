using System;
using System.Collections.Generic;
using System.Globalization;
using Niga_Domain.DTOs;

namespace Niga_Domain.Helpers;

public static class WhatsAppMessageTemplateEngine
{
    public const string DefaultProfessionalTemplate =
        "Dear {{PatientName}},\r\n\r\n" +
        "Greetings from {{HospitalName}}.\r\n\r\n" +
        "We are pleased to inform you about our healthcare services under the guidance of {{DoctorName}}.\r\n\r\n" +
        "📅 Effective Date:\r\n{{Date}}\r\n\r\n" +
        "📢 Important Information:\r\n{{Message}}\r\n\r\n" +
        "For appointments and inquiries, please contact our clinic.\r\n\r\n" +
        "Thank you for choosing us for your healthcare journey.\r\n\r\n" +
        "Warm Regards,\r\n{{DoctorName}}\r\n{{HospitalName}}";

    public const string DefaultOfferTemplate =
        "Dear {{PatientName}},\r\n\r\n" +
        "{{HospitalName}} has an exclusive offer for you:\r\n\r\n" +
        "{{Offer}}\r\n\r\n" +
        "Valid until {{Date}}.\r\n\r\n" +
        "Contact {{DoctorName}} for details.\r\n\r\n" +
        "Warm Regards,\r\n{{HospitalName}}";

    public const string DefaultHealthTipTemplate =
        "Dear {{PatientName}},\r\n\r\n" +
        "Health Tip from {{DoctorName}} at {{HospitalName}}:\r\n\r\n" +
        "{{HealthTip}}\r\n\r\n" +
        "Stay healthy!\r\n\r\n" +
        "Warm Regards,\r\n{{DoctorName}}";

    public const string DefaultProfessionalTemplateMarathi =
        "प्रिय {{PatientName}},\r\n\r\n" +
        "{{HospitalName}} कडून शुभेच्छा.\r\n\r\n" +
        "{{DoctorName}} यांच्या मार्गदर्शनाखाली आमच्या आरोग्य सेवांबद्दल माहिती:\r\n\r\n" +
        "📅 दिनांक:\r\n{{Date}}\r\n\r\n" +
        "📢 महत्वाची माहिती:\r\n{{Message}}\r\n\r\n" +
        "भेटीसाठी कृपया आमच्या क्लिनिकशी संपर्क साधा.\r\n\r\n" +
        "सादर,\r\n{{DoctorName}}\r\n{{HospitalName}}";

    public const string DefaultOfferTemplateMarathi =
        "प्रिय {{PatientName}},\r\n\r\n" +
        "{{HospitalName}} कडून तुमच्यासाठी खास ऑफर:\r\n\r\n" +
        "{{Offer}}\r\n\r\n" +
        "{{Date}} पर्यंत वैध.\r\n\r\n" +
        "तपशीलांसाठी {{DoctorName}} यांच्याशी संपर्क साधा.\r\n\r\n" +
        "सादर,\r\n{{HospitalName}}";

    public const string DefaultHealthTipTemplateMarathi =
        "प्रिय {{PatientName}},\r\n\r\n" +
        "{{HospitalName}} मधील {{DoctorName}} यांचा आरोग्य टिप:\r\n\r\n" +
        "{{HealthTip}}\r\n\r\n" +
        "निरोगी राहा!\r\n\r\n" +
        "सादर,\r\n{{DoctorName}}";

    private static readonly Dictionary<string, Func<WhatsAppPlaceholderContext, string>> PlaceholderMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["{{PatientName}}"] = ctx => ctx.PatientName,
            ["{{patient_name}}"] = ctx => ctx.PatientName,
            ["{{HospitalName}}"] = ctx => ctx.HospitalName,
            ["{{hospital_name}}"] = ctx => ctx.HospitalName,
            ["{{DoctorName}}"] = ctx => ctx.DoctorName,
            ["{{doctor_name}}"] = ctx => ctx.DoctorName,
            ["{{Date}}"] = ctx => ctx.Date,
            ["{{date}}"] = ctx => ctx.Date,
            ["{{Message}}"] = ctx => ctx.Message,
            ["{{message}}"] = ctx => ctx.Message,
            ["{{Offer}}"] = ctx => ctx.Offer,
            ["{{offer}}"] = ctx => ctx.Offer,
            ["{{HealthTip}}"] = ctx => ctx.HealthTip,
            ["{{healthtip}}"] = ctx => ctx.HealthTip,
            ["{{health_tip}}"] = ctx => ctx.HealthTip,
            ["{{AppointmentDate}}"] = ctx => ctx.AppointmentDate,
            ["{{appointmentdate}}"] = ctx => ctx.AppointmentDate,
            ["{{appointment_date}}"] = ctx => ctx.AppointmentDate,
            ["{{AppointmentTime}}"] = ctx => ctx.AppointmentTime,
            ["{{appointmenttime}}"] = ctx => ctx.AppointmentTime,
            ["{{appointment_time}}"] = ctx => ctx.AppointmentTime
        };

    public static string ResolveTemplate(string? template, WhatsAppPlaceholderContext context, string? defaultTemplate = null)
    {
        var source = string.IsNullOrWhiteSpace(template)
            ? (defaultTemplate ?? DefaultProfessionalTemplate)
            : template;
        var result = source;

        foreach (var placeholder in PlaceholderMap)
        {
            result = result.Replace(placeholder.Key, placeholder.Value(context) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    public static string FormatDisplayDate(string? dateInput)
    {
        if (string.IsNullOrWhiteSpace(dateInput))
        {
            return DateTime.Now.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(dateInput, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            || DateTime.TryParse(dateInput, out parsed))
        {
            return parsed.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
        }

        return dateInput.Trim();
    }

    public static string NormalizeMobileNumber(string? mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return string.Empty;
        }

        var digits = new string(mobileNumber.Where(char.IsDigit).ToArray());
        return digits;
    }

    public static string FormatForWhatsAppApi(string? mobileNumber, string? defaultCountryDialCode = "91")
    {
        var digits = NormalizeMobileNumber(mobileNumber);
        if (string.IsNullOrEmpty(digits))
        {
            return string.Empty;
        }

        var countryCode = NormalizeMobileNumber(defaultCountryDialCode);
        if (digits.Length == 10 && !string.IsNullOrEmpty(countryCode) && !digits.StartsWith(countryCode, StringComparison.Ordinal))
        {
            return countryCode + digits;
        }

        return digits;
    }

    public static bool MobileNumbersMatch(string? first, string? second, string? defaultCountryDialCode = "91")
    {
        var a = FormatForWhatsAppApi(first, defaultCountryDialCode);
        var b = FormatForWhatsAppApi(second, defaultCountryDialCode);
        return !string.IsNullOrEmpty(a) && a == b;
    }

    public static bool TryDecodeBase64Image(string? base64Input, out byte[] imageBytes, out string? mimeType, out string? errorMessage)
    {
        imageBytes = Array.Empty<byte>();
        mimeType = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(base64Input))
        {
            return false;
        }

        var payload = base64Input.Trim();
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = payload.IndexOf(',');
            if (commaIndex < 0)
            {
                errorMessage = "Invalid Base64 data URI format.";
                return false;
            }

            var header = payload[..commaIndex];
            payload = payload[(commaIndex + 1)..];

            if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase)
                || header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/jpeg";
            }
            else if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/png";
            }
            else if (header.Contains("image/webp", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/webp";
            }
            else
            {
                errorMessage = "Unsupported image MIME type. Allowed: JPEG, PNG, WEBP.";
                return false;
            }
        }

        try
        {
            imageBytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            errorMessage = "Invalid Base64 image format.";
            return false;
        }

        if (imageBytes.Length == 0)
        {
            errorMessage = "Image data is empty.";
            return false;
        }

        mimeType ??= DetectImageMimeType(imageBytes);
        if (mimeType == null)
        {
            errorMessage = "Unable to detect image type. Allowed: JPEG, PNG, WEBP.";
            return false;
        }

        return true;
    }

    private static string? DetectImageMimeType(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47)
        {
            return "image/png";
        }

        if (bytes.Length >= 12
            && bytes[0] == 0x52
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x46
            && bytes[8] == 0x57
            && bytes[9] == 0x45
            && bytes[10] == 0x42
            && bytes[11] == 0x50)
        {
            return "image/webp";
        }

        return null;
    }
}
