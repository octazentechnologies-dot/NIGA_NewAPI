using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    public static class PatientImportValidator
    {
        private static readonly string[] DateFormats =
        {
            "M/d/yyyy",
            "MM/dd/yyyy",
            "d/M/yyyy",
            "dd/MM/yyyy",
            "d-M-yyyy",
            "dd-MM-yyyy",
            "yyyy-MM-dd",
            "M/d/yy",
            "MM/dd/yy",
        };

        public static bool TryValidateRow(
            PatientImportRowModel row,
            IReadOnlyDictionary<string, int> countriesByName,
            IReadOnlyDictionary<int, string> countryNamesById,
            IReadOnlyDictionary<string, int> statesByName,
            IReadOnlyDictionary<int, int> stateCountryById,
            HashSet<string> existingDoctorMobiles,
            HashSet<string> fileMobiles,
            out ValidatedPatientImportRow? validated,
            out string error)
        {
            validated = null;
            error = string.Empty;

            var firstName = (row.FirstName ?? string.Empty).Trim();
            var lastName = (row.LastName ?? string.Empty).Trim();
            if (firstName.Length < 2)
            {
                error = "First name is required (min 2 characters).";
                return false;
            }

            if (lastName.Length < 2)
            {
                error = "Last name is required (min 2 characters).";
                return false;
            }

            if (!TryParseGender(row.Gender, out var gender))
            {
                error = "Gender must be M/F, Male/Female, or 0/1.";
                return false;
            }

            if (!TryParseDate(row.DateOfBirth, out var dateOfBirth))
            {
                error = "Date of birth is invalid.";
                return false;
            }

            if (dateOfBirth.Date > DateTime.Today)
            {
                error = "Date of birth cannot be in the future.";
                return false;
            }

            var address = (row.Address ?? string.Empty).Trim();
            if (address.Length < 5)
            {
                error = "Address is required (min 5 characters).";
                return false;
            }

            if (!TryResolveCountry(row.Country, countriesByName, countryNamesById, out var countryId))
            {
                error = "Country is invalid.";
                return false;
            }

            if (!TryResolveState(row.State, statesByName, stateCountryById, countryId, out var stateId))
            {
                error = "State is invalid or does not belong to the selected country.";
                return false;
            }

            var mobile = NormalizeMobile(row.MobileNo);
            if (!Regex.IsMatch(mobile, @"^\d{10}$"))
            {
                error = "Mobile number must be exactly 10 digits.";
                return false;
            }

            if (!fileMobiles.Add(mobile))
            {
                error = "Duplicate mobile number in import file.";
                return false;
            }

            if (existingDoctorMobiles.Contains(mobile))
            {
                error = "Mobile number already exists for this doctor.";
                return false;
            }

            var email = (row.Email ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            {
                error = "Email format is invalid.";
                return false;
            }

            DateTime? appointmentDate = null;
            TimeOnly? appointmentTime = null;
            var hasAppointmentDate = !string.IsNullOrWhiteSpace(row.AppointmentDate);
            var hasAppointmentTime = !string.IsNullOrWhiteSpace(row.AppointmentTime);

            if (hasAppointmentDate)
            {
                if (!TryParseDate(row.AppointmentDate, out var parsedAppointmentDate))
                {
                    error = "Appointment date is invalid.";
                    return false;
                }

                appointmentDate = parsedAppointmentDate.Date;
            }

            if (hasAppointmentTime)
            {
                if (!TryParseTime(row.AppointmentTime, out appointmentTime))
                {
                    error = "Appointment time is invalid.";
                    return false;
                }
            }

            if (hasAppointmentTime && !hasAppointmentDate)
            {
                error = "Appointment date is required when appointment time is provided.";
                return false;
            }

            validated = new ValidatedPatientImportRow
            {
                Source = row,
                PatientName = $"{firstName} {lastName}".Trim(),
                Gender = gender,
                DateOfBirth = dateOfBirth.Date,
                Address = address,
                CountryId = countryId,
                StateId = stateId,
                MobileNo = mobile,
                PhoneNo = string.IsNullOrWhiteSpace(row.PhoneNo) ? null : row.PhoneNo.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                RefBy = string.IsNullOrWhiteSpace(row.ReferBy) ? null : row.ReferBy.Trim(),
                IsWhatsAppOptIn = ParseWhatsAppOptIn(row.WhatsAppOptIn),
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
            };

            return true;
        }

        public static Dictionary<string, int> BuildCountryNameMap(IEnumerable<CountryMaster> countries)
        {
            return countries
                .Where(c => !string.IsNullOrWhiteSpace(c.CountryName))
                .GroupBy(c => c.CountryName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().CountryId, StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<int, string> BuildCountryIdNameMap(IEnumerable<CountryMaster> countries)
        {
            return countries.ToDictionary(c => c.CountryId, c => c.CountryName);
        }

        public static Dictionary<string, int> BuildStateNameMap(IEnumerable<StateMaster> states)
        {
            return states
                .Where(s => !string.IsNullOrWhiteSpace(s.StateName))
                .GroupBy(s => s.StateName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().StateId, StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<int, int> BuildStateCountryMap(IEnumerable<StateMaster> states)
        {
            return states
                .Where(s => s.CountryId.HasValue)
                .ToDictionary(s => s.StateId, s => s.CountryId!.Value);
        }

        private static bool TryParseGender(string? value, out int gender)
        {
            gender = 0;
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (int.TryParse(text, out var numeric) && (numeric == 0 || numeric == 1))
            {
                gender = numeric;
                return true;
            }

            if (text.Equals("m", StringComparison.OrdinalIgnoreCase)
                || text.Equals("male", StringComparison.OrdinalIgnoreCase))
            {
                gender = 0;
                return true;
            }

            if (text.Equals("f", StringComparison.OrdinalIgnoreCase)
                || text.Equals("female", StringComparison.OrdinalIgnoreCase))
            {
                gender = 1;
                return true;
            }

            return false;
        }

        private static bool TryParseDate(string? value, out DateTime date)
        {
            date = default;
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }

            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static bool TryParseTime(string? value, out TimeOnly? time)
        {
            time = null;
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (TimeOnly.TryParseExact(text, new[] { "H:mm", "HH:mm", "h:mm tt", "hh:mm tt", "H:mm:ss", "HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                time = parsed;
                return true;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                time = TimeOnly.FromDateTime(dateTime);
                return true;
            }

            return false;
        }

        private static bool TryResolveCountry(
            string? value,
            IReadOnlyDictionary<string, int> countriesByName,
            IReadOnlyDictionary<int, string> countryNamesById,
            out int countryId)
        {
            countryId = 0;
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (int.TryParse(text, out var id) && countryNamesById.ContainsKey(id))
            {
                countryId = id;
                return true;
            }

            return countriesByName.TryGetValue(text, out countryId);
        }

        private static bool TryResolveState(
            string? value,
            IReadOnlyDictionary<string, int> statesByName,
            IReadOnlyDictionary<int, int> stateCountryById,
            int countryId,
            out int stateId)
        {
            stateId = 0;
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (int.TryParse(text, out var id) && stateCountryById.TryGetValue(id, out var stateCountryId) && stateCountryId == countryId)
            {
                stateId = id;
                return true;
            }

            if (!statesByName.TryGetValue(text, out stateId))
            {
                return false;
            }

            return stateCountryById.TryGetValue(stateId, out var countryForState) && countryForState == countryId;
        }

        private static string NormalizeMobile(string? value)
        {
            return Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
        }

        private static bool ParseWhatsAppOptIn(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || text.Equals("y", StringComparison.OrdinalIgnoreCase)
                || text.Equals("true", StringComparison.OrdinalIgnoreCase)
                || text == "1";
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ValidatedPatientImportRow
    {
        public PatientImportRowModel Source { get; set; } = new();
        public string PatientName { get; set; } = string.Empty;
        public int Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public string MobileNo { get; set; } = string.Empty;
        public string? PhoneNo { get; set; }
        public string? Email { get; set; }
        public string? RefBy { get; set; }
        public bool IsWhatsAppOptIn { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeOnly? AppointmentTime { get; set; }
    }
}
