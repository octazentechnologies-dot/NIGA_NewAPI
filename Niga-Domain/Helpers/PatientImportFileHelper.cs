using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Niga_Domain.DTOs;

namespace Niga_Domain.Helpers
{
    public static class PatientImportFileHelper
    {
        public static readonly string[] Headers =
        {
            "First Name",
            "Last Name",
            "Gender",
            "Date of Birth",
            "Address",
            "Country",
            "State",
            "Mobile No",
            "Phone No",
            "Email",
            "Refer By",
            "WhatsApp Opt-In",
            "Appointment Date",
            "Appointment Time",
        };

        private static readonly Dictionary<string, string> HeaderAliases = BuildHeaderAliases();

        public static byte[] BuildTemplateExcel()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Patients");

            for (var col = 0; col < Headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = Headers[col];
                worksheet.Cell(1, col + 1).Style.Font.Bold = true;
            }

            worksheet.Cell(2, 1).Value = "Gourav";
            worksheet.Cell(2, 2).Value = "Nikam";
            worksheet.Cell(2, 3).Value = "M";
            worksheet.Cell(2, 4).Value = "01/15/1990";
            worksheet.Cell(2, 5).Value = "Kop";
            worksheet.Cell(2, 6).Value = "India";
            worksheet.Cell(2, 7).Value = "Maharashtra";
            worksheet.Cell(2, 8).Value = "9876543210";
            worksheet.Cell(2, 9).Value = "";
            worksheet.Cell(2, 10).Value = "gourav@example.com";
            worksheet.Cell(2, 11).Value = "Dr. Smith";
            worksheet.Cell(2, 12).Value = "No";
            worksheet.Cell(2, 13).Value = "";
            worksheet.Cell(2, 14).Value = "";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static byte[] BuildTemplateCsv()
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine(string.Join(",", Headers.Select(EscapeCsv)));
            sb.AppendLine(string.Join(",",
                EscapeCsv("Gourav"),
                EscapeCsv("Nikam"),
                EscapeCsv("M"),
                EscapeCsv("01/15/1990"),
                EscapeCsv("Kop"),
                EscapeCsv("India"),
                EscapeCsv("Maharashtra"),
                EscapeCsv("9876543210"),
                EscapeCsv(""),
                EscapeCsv("gourav@example.com"),
                EscapeCsv("Dr. Smith"),
                EscapeCsv("No"),
                EscapeCsv(""),
                EscapeCsv("")));
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static async Task<List<PatientImportRowModel>> ParseImportFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            return extension switch
            {
                ".csv" => ParseCsv(stream),
                ".xlsx" or ".xls" => ParseExcel(stream),
                _ => throw new InvalidOperationException("Unsupported file type. Please upload .xlsx or .csv."),
            };
        }

        public static byte[] BuildSkippedRowsFile(List<PatientImportRowModel> skippedRows, string sourceExtension)
        {
            var exportHeaders = Headers.Concat(new[] { "Skip Reason" }).ToArray();

            if (sourceExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.Append('\uFEFF');
                sb.AppendLine(string.Join(",", exportHeaders.Select(EscapeCsv)));
                foreach (var row in skippedRows)
                {
                    sb.AppendLine(string.Join(",", GetRowValues(row).Concat(new[] { row.SkipReason ?? string.Empty }).Select(EscapeCsv)));
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Skipped Rows");
            for (var col = 0; col < exportHeaders.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = exportHeaders[col];
                worksheet.Cell(1, col + 1).Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var row in skippedRows)
            {
                var values = GetRowValues(row).Concat(new[] { row.SkipReason ?? string.Empty }).ToArray();
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

        public static string BuildSkippedFileName(string sourceExtension)
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var ext = sourceExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "xlsx";
            return $"Patients_Import_Skipped_{stamp}.{ext}";
        }

        public static string GetSkippedFileContentType(string sourceExtension)
        {
            return sourceExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? "text/csv"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }

        private static List<PatientImportRowModel> ParseExcel(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("Worksheet not found in Excel file.");

            var headerMap = BuildHeaderMap(worksheet.Row(1));
            var rows = new List<PatientImportRowModel>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (IsExcelRowEmpty(row, headerMap.Count))
                {
                    continue;
                }

                rows.Add(new PatientImportRowModel
                {
                    RowNumber = rowNumber,
                    FirstName = GetExcelCell(row, headerMap, "First Name"),
                    LastName = GetExcelCell(row, headerMap, "Last Name"),
                    Gender = GetExcelCell(row, headerMap, "Gender"),
                    DateOfBirth = GetExcelCell(row, headerMap, "Date of Birth"),
                    Address = GetExcelCell(row, headerMap, "Address"),
                    Country = GetExcelCell(row, headerMap, "Country"),
                    State = GetExcelCell(row, headerMap, "State"),
                    MobileNo = GetExcelCell(row, headerMap, "Mobile No"),
                    PhoneNo = GetExcelCell(row, headerMap, "Phone No"),
                    Email = GetExcelCell(row, headerMap, "Email"),
                    ReferBy = GetExcelCell(row, headerMap, "Refer By"),
                    WhatsAppOptIn = GetExcelCell(row, headerMap, "WhatsApp Opt-In"),
                    AppointmentDate = GetExcelCell(row, headerMap, "Appointment Date"),
                    AppointmentTime = GetExcelCell(row, headerMap, "Appointment Time"),
                });
            }

            return rows;
        }

        private static List<PatientImportRowModel> ParseCsv(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return new List<PatientImportRowModel>();
            }

            var headerCells = ParseCsvLine(headerLine);
            var headerMap = BuildHeaderMap(headerCells);
            var rows = new List<PatientImportRowModel>();
            var rowNumber = 1;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                rowNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (cells.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                rows.Add(new PatientImportRowModel
                {
                    RowNumber = rowNumber,
                    FirstName = GetCsvCell(cells, headerMap, "First Name"),
                    LastName = GetCsvCell(cells, headerMap, "Last Name"),
                    Gender = GetCsvCell(cells, headerMap, "Gender"),
                    DateOfBirth = GetCsvCell(cells, headerMap, "Date of Birth"),
                    Address = GetCsvCell(cells, headerMap, "Address"),
                    Country = GetCsvCell(cells, headerMap, "Country"),
                    State = GetCsvCell(cells, headerMap, "State"),
                    MobileNo = GetCsvCell(cells, headerMap, "Mobile No"),
                    PhoneNo = GetCsvCell(cells, headerMap, "Phone No"),
                    Email = GetCsvCell(cells, headerMap, "Email"),
                    ReferBy = GetCsvCell(cells, headerMap, "Refer By"),
                    WhatsAppOptIn = GetCsvCell(cells, headerMap, "WhatsApp Opt-In"),
                    AppointmentDate = GetCsvCell(cells, headerMap, "Appointment Date"),
                    AppointmentTime = GetCsvCell(cells, headerMap, "Appointment Time"),
                });
            }

            return rows;
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var cells = new List<string>();
            foreach (var cell in headerRow.CellsUsed())
            {
                cells.Add(cell.GetString().Trim());
            }

            return BuildHeaderMap(cells);
        }

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headerCells)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerCells.Count; i++)
            {
                var normalized = NormalizeHeader(headerCells[i]);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (HeaderAliases.TryGetValue(normalized, out var canonical) && !map.ContainsKey(canonical))
                {
                    map[canonical] = i;
                }
            }

            return map;
        }

        private static Dictionary<string, string> BuildHeaderAliases()
        {
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in Headers)
            {
                aliases[NormalizeHeader(header)] = header;
            }

            void Map(string alias, string canonical) => aliases[alias] = canonical;

            Map("firstname", "First Name");
            Map("lastname", "Last Name");
            Map("dob", "Date of Birth");
            Map("dateofbirth", "Date of Birth");
            Map("mobile", "Mobile No");
            Map("mobileno", "Mobile No");
            Map("phone", "Phone No");
            Map("phoneno", "Phone No");
            Map("referby", "Refer By");
            Map("whatsappoptin", "WhatsApp Opt-In");
            Map("appointmentdate", "Appointment Date");
            Map("appointmenttime", "Appointment Time");

            return aliases;
        }

        private static string NormalizeHeader(string value)
        {
            return Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
        }

        private static bool IsExcelRowEmpty(IXLRow row, int columnCount)
        {
            for (var col = 1; col <= Math.Max(columnCount, Headers.Length); col++)
            {
                if (!string.IsNullOrWhiteSpace(row.Cell(col).GetString()))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetExcelCell(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string canonicalHeader)
        {
            if (!headerMap.TryGetValue(canonicalHeader, out var index))
            {
                return string.Empty;
            }

            return row.Cell(index + 1).GetString().Trim();
        }

        private static string GetCsvCell(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerMap, string canonicalHeader)
        {
            if (!headerMap.TryGetValue(canonicalHeader, out var index) || index >= cells.Count)
            {
                return string.Empty;
            }

            return cells[index].Trim();
        }

        private static string[] GetRowValues(PatientImportRowModel row)
        {
            return new[]
            {
                row.FirstName ?? string.Empty,
                row.LastName ?? string.Empty,
                row.Gender ?? string.Empty,
                row.DateOfBirth ?? string.Empty,
                row.Address ?? string.Empty,
                row.Country ?? string.Empty,
                row.State ?? string.Empty,
                row.MobileNo ?? string.Empty,
                row.PhoneNo ?? string.Empty,
                row.Email ?? string.Empty,
                row.ReferBy ?? string.Empty,
                row.WhatsAppOptIn ?? string.Empty,
                row.AppointmentDate ?? string.Empty,
                row.AppointmentTime ?? string.Empty,
            };
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            result.Add(current.ToString());
            return result;
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
