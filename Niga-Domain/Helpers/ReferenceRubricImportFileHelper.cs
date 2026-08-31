using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Niga_Domain.DTOs;

namespace Niga_Domain.Helpers
{
    public static class ReferenceRubricImportFileHelper
    {
        public static readonly string[] Headers =
        {
            "SubSectionName",
            "RefSubSectionName",
        };

        private static readonly Dictionary<string, string> HeaderAliases = BuildHeaderAliases();

        public static byte[] BuildTemplateExcel()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ReferenceRubrics");

            for (var col = 0; col < Headers.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = Headers[col];
                worksheet.Cell(1, col + 1).Style.Font.Bold = true;
            }

            worksheet.Cell(2, 1).Value = "MIND-ABRUPT";
            worksheet.Cell(2, 2).Value = "MIND-ANGER";
            worksheet.Cell(3, 1).Value = "MIND-ABRUPT";
            worksheet.Cell(3, 2).Value = "MIND-FEAR";

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
            sb.AppendLine(string.Join(",", EscapeCsv("MIND-ABRUPT"), EscapeCsv("MIND-ANGER")));
            sb.AppendLine(string.Join(",", EscapeCsv("MIND-ABRUPT"), EscapeCsv("MIND-FEAR")));
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static async Task<List<ReferenceRubricImportRowModel>> ParseImportFileAsync(IFormFile file)
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

        public static byte[] BuildSkippedRowsFile(List<ReferenceRubricImportRowModel> skippedRows, string sourceExtension)
        {
            var exportHeaders = Headers.Concat(new[] { "Skip Reason" }).ToArray();

            if (sourceExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.Append('\uFEFF');
                sb.AppendLine(string.Join(",", exportHeaders.Select(EscapeCsv)));
                foreach (var row in skippedRows)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(row.SubSectionName),
                        EscapeCsv(row.RefSubSectionName),
                        EscapeCsv(row.SkipReason)));
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
                worksheet.Cell(rowIndex, 1).Value = row.SubSectionName ?? string.Empty;
                worksheet.Cell(rowIndex, 2).Value = row.RefSubSectionName ?? string.Empty;
                worksheet.Cell(rowIndex, 3).Value = row.SkipReason ?? string.Empty;
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
            return $"ReferenceRubrics_Import_Skipped_{stamp}.{ext}";
        }

        public static string GetSkippedFileContentType(string sourceExtension)
        {
            return sourceExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? "text/csv"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }

        private static List<ReferenceRubricImportRowModel> ParseExcel(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("Worksheet not found in Excel file.");

            var headerMap = BuildHeaderMap(worksheet.Row(1));
            var rows = new List<ReferenceRubricImportRowModel>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (IsExcelRowEmpty(row))
                {
                    continue;
                }

                rows.Add(new ReferenceRubricImportRowModel
                {
                    RowNumber = rowNumber,
                    SubSectionName = GetExcelCell(row, headerMap, "SubSectionName"),
                    RefSubSectionName = GetExcelCell(row, headerMap, "RefSubSectionName"),
                });
            }

            return rows;
        }

        private static List<ReferenceRubricImportRowModel> ParseCsv(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return new List<ReferenceRubricImportRowModel>();
            }

            var headerCells = ParseCsvLine(headerLine);
            var headerMap = BuildHeaderMap(headerCells);
            var rows = new List<ReferenceRubricImportRowModel>();
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

                rows.Add(new ReferenceRubricImportRowModel
                {
                    RowNumber = rowNumber,
                    SubSectionName = GetCsvCell(cells, headerMap, "SubSectionName"),
                    RefSubSectionName = GetCsvCell(cells, headerMap, "RefSubSectionName"),
                });
            }

            return rows;
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var cells = headerRow.CellsUsed().Select(cell => cell.GetString().Trim()).ToList();
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

            aliases["subsectionname"] = "SubSectionName";
            aliases["refsubsectionname"] = "RefSubSectionName";
            aliases["reference subsection name"] = "RefSubSectionName";
            aliases["reference rubric"] = "RefSubSectionName";
            return aliases;
        }

        private static string NormalizeHeader(string value)
        {
            return Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
        }

        private static bool IsExcelRowEmpty(IXLRow row)
        {
            return string.IsNullOrWhiteSpace(row.Cell(1).GetString())
                && string.IsNullOrWhiteSpace(row.Cell(2).GetString());
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
