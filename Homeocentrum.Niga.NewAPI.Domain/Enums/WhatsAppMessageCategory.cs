using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Enums;

public static class WhatsAppMessageCategory
{
    public const string HospitalService = "HospitalService";
    public const string OffersDiscount = "OffersDiscount";
    public const string HealthTips = "HealthTips";

    public static bool IsValid(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        return category.Equals(HospitalService, StringComparison.OrdinalIgnoreCase)
               || category.Equals(OffersDiscount, StringComparison.OrdinalIgnoreCase)
               || category.Equals(HealthTips, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string category)
    {
        if (category.Equals(HospitalService, StringComparison.OrdinalIgnoreCase))
        {
            return HospitalService;
        }

        if (category.Equals(OffersDiscount, StringComparison.OrdinalIgnoreCase))
        {
            return OffersDiscount;
        }

        if (category.Equals(HealthTips, StringComparison.OrdinalIgnoreCase))
        {
            return HealthTips;
        }

        return category;
    }
}
