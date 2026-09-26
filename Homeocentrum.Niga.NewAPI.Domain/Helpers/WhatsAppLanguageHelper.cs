using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers;

public static class WhatsAppLanguageHelper
{
    public const int FallbackEnglishLanguageId = 1;

    private static readonly HashSet<string> EnglishNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ENGLISH",
        "EN"
    };

    private static readonly HashSet<string> MarathiNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MARATHI",
        "MR",
        "MAR"
    };

    public static bool IsEnglishLanguage(string? languageName)
    {
        return !string.IsNullOrWhiteSpace(languageName)
               && EnglishNames.Contains(languageName.Trim());
    }

    public static bool IsMarathiLanguage(string? languageName)
    {
        return !string.IsNullOrWhiteSpace(languageName)
               && MarathiNames.Contains(languageName.Trim());
    }

    public static string GetMetaLanguageCode(int languageId, string? languageName)
    {
        if (IsMarathiLanguage(languageName))
        {
            return "mr";
        }

        return "en";
    }

    public static string GetDefaultTemplateBody(string category, int languageId, string? languageName)
    {
        if (IsMarathiLanguage(languageName))
        {
            if (category == Enums.WhatsAppMessageCategory.OffersDiscount)
            {
                return WhatsAppMessageTemplateEngine.DefaultOfferTemplateMarathi;
            }

            if (category == Enums.WhatsAppMessageCategory.HealthTips)
            {
                return WhatsAppMessageTemplateEngine.DefaultHealthTipTemplateMarathi;
            }

            return WhatsAppMessageTemplateEngine.DefaultProfessionalTemplateMarathi;
        }

        if (category == Enums.WhatsAppMessageCategory.OffersDiscount)
        {
            return WhatsAppMessageTemplateEngine.DefaultOfferTemplate;
        }

        if (category == Enums.WhatsAppMessageCategory.HealthTips)
        {
            return WhatsAppMessageTemplateEngine.DefaultHealthTipTemplate;
        }

        return WhatsAppMessageTemplateEngine.DefaultProfessionalTemplate;
    }
}
