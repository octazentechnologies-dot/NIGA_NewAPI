namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    /// <summary>
    /// S3 Week 3 appointment rules. New rows only. No historical rewrite.
    /// </summary>
    public static class S3AppointmentRules
    {
        public const string InClinic = "InClinic";
        public const string Tele = "Tele";
        public const string Cancelled = "CANCELLED";
        public const string Unpaid = "UNPAID";
        public const string Paid = "PAID";

        public static readonly string[] CancelReasons =
        {
            "PatientRequest", "DoctorUnavailable", "Duplicate", "Other"
        };

        public static string NormalizeMode(string? raw, string fallback = InClinic)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            var value = raw.Trim();
            if (value.Equals(Tele, StringComparison.OrdinalIgnoreCase)
                || value.Equals("E-CONSULT", StringComparison.OrdinalIgnoreCase)
                || value.Equals("E-Consult", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Online", StringComparison.OrdinalIgnoreCase))
                return Tele;

            if (value.Equals(InClinic, StringComparison.OrdinalIgnoreCase)
                || value.Equals("In-clinic", StringComparison.OrdinalIgnoreCase)
                || value.Equals("In clinic", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Clinic", StringComparison.OrdinalIgnoreCase))
                return InClinic;

            return fallback;
        }

        public static bool IsCancelled(string? status)
            => string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);

        /// <summary>REC-13.02 — clinic SPA must not mark appointments paid; Account/webhook does.</summary>
        public static bool IsPaid(string? paymentStatus)
            => string.Equals(paymentStatus, Paid, StringComparison.OrdinalIgnoreCase);
    }
}
