using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Niga_Domain.DTOs;

namespace Niga_Domain.Helpers
{
    public static class PatientExportFileBuilder
    {
        private static readonly string[] TodayExtraHeaders =
        {
            "Appointment Date",
            "Appointment Time",
            "Appointment Status",
        };

        private static readonly string[] BaseHeaders =
        {
            "Patient ID",
            "Case ID",
            "Patient Name",
            "Date of Birth",
            "Age",
            "Gender",
            "Address",
            "State",
            "Country",
            "Mobile",
            "Phone",
            "Email",
            "First Visit Date",
            "Referred By",
            "Diagnosis",
            "Chief Complaints",
            "WhatsApp Opt-In",
            "WhatsApp Opt-In Date",
            "Entered By",
            "Entered Date",
            "Changed By",
            "Changed Date",
        };

        public static string[] GetHeaders(bool includeAppointmentColumns)
        {
            if (!includeAppointmentColumns)
            {
                return BaseHeaders;
            }

            return BaseHeaders.Concat(TodayExtraHeaders).ToArray();
        }

        public static byte[] BuildCsv(List<PatientExportRowModel> rows, bool includeAppointmentColumns)
        {
            var headers = GetHeaders(includeAppointmentColumns);
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", GetRowValues(row, includeAppointmentColumns).Select(EscapeCsv)));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] BuildExcel(List<PatientExportRowModel> rows, bool includeAppointmentColumns)
        {
            var headers = GetHeaders(includeAppointmentColumns);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Patients");

            for (var col = 0; col < headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = headers[col];
                worksheet.Cell(1, col + 1).Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                var values = GetRowValues(row, includeAppointmentColumns);
                for (var col = 0; col < values.Length; col++)
                {
                    worksheet.Cell(rowIndex, col + 1).Value = values[col];
                }

                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static byte[] BuildPdf(List<PatientExportRowModel> rows, bool includeAppointmentColumns, string title)
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            writer.SetCloseStream(false);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4.Rotate());
            document.SetMargins(20, 20, 20, 20);

            document.Add(
                new Paragraph(title)
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginBottom(10)
            );

            var headers = GetHeaders(includeAppointmentColumns);
            var table = new Table(UnitValue.CreatePercentArray(headers.Length)).UseAllAvailableWidth();
            table.SetFontSize(6);

            foreach (var header in headers)
            {
                table.AddHeaderCell(
                    new Cell()
                        .Add(new Paragraph(header))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetBold()
                );
            }

            foreach (var row in rows)
            {
                foreach (var value in GetRowValues(row, includeAppointmentColumns))
                {
                    table.AddCell(new Cell().Add(new Paragraph(value ?? string.Empty)));
                }
            }

            document.Add(table);
            document.Close();
            return stream.ToArray();
        }

        public static string GetContentType(string format)
        {
            return format switch
            {
                "csv" => "text/csv",
                "pdf" => "application/pdf",
                _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            };
        }

        public static string GetFileExtension(string format)
        {
            return format switch
            {
                "csv" => "csv",
                "pdf" => "pdf",
                _ => "xlsx",
            };
        }

        private static string[] GetRowValues(PatientExportRowModel row, bool includeAppointmentColumns)
        {
            var values = new List<string>
            {
                row.PatientId.ToString(CultureInfo.InvariantCulture),
                row.CaseId.ToString(CultureInfo.InvariantCulture),
                row.PatientName ?? string.Empty,
                FormatDate(row.DateOfBirth),
                row.Age?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                FormatGender(row.Gender),
                row.Address ?? string.Empty,
                row.StateName ?? string.Empty,
                row.CountryName ?? string.Empty,
                row.MobileNo ?? string.Empty,
                row.PhoneNo ?? string.Empty,
                row.Email ?? string.Empty,
                FormatDate(row.DateOfFirstVisit),
                row.RefBy ?? string.Empty,
                row.Diagnosis ?? string.Empty,
                row.ChiefComplaints ?? string.Empty,
                row.IsWhatsAppOptIn ? "Yes" : "No",
                FormatDate(row.WhatsAppOptInDate),
                row.EnteredBy ?? string.Empty,
                FormatDateTime(row.EnteredDate),
                row.ChangedBy ?? string.Empty,
                FormatDateTime(row.ChangedDate),
            };

            if (includeAppointmentColumns)
            {
                values.Add(FormatDate(row.AppointmentDate));
                values.Add(FormatAppointmentTime(row.AppointmentTime));
                values.Add(row.AppointmentStatus ?? string.Empty);
            }

            return values.ToArray();
        }

        private static string FormatGender(int? gender)
        {
            return gender switch
            {
                0 => "M",
                1 => "F",
                _ => string.Empty,
            };
        }

        private static string FormatDate(DateTime? value)
        {
            return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatDateTime(DateTime? value)
        {
            return value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatAppointmentTime(TimeOnly? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            var dateTime = DateTime.Today.Add(value.Value.ToTimeSpan());
            return dateTime.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        private static string EscapeCsv(string? value)
        {
            var text = value ?? string.Empty;
            if (text.Contains('"', StringComparison.Ordinal))
            {
                text = text.Replace("\"", "\"\"", StringComparison.Ordinal);
            }

            if (text.Contains(',', StringComparison.Ordinal) || text.Contains('\n', StringComparison.Ordinal) || text.Contains('\r', StringComparison.Ordinal))
            {
                return $"\"{text}\"";
            }

            return text;
        }
    }
}
