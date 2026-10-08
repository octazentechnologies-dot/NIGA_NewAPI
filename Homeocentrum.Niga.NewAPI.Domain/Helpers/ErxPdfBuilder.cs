using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    public sealed class ErxPdfHeader
    {
        public int ErxId { get; set; }
        public int PatientAppId { get; set; }
        public string ClinicName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string? ClinicContact { get; set; }
        public string PatientName { get; set; } = "";
        public string? PatientMeta { get; set; }
        public DateTime? SignedAt { get; set; }
    }

    public sealed class ErxPdfLine
    {
        public string Remedy { get; set; } = "";
        public string? Potency { get; set; }
        public string? Dose { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }

    public static class ErxPdfBuilder
    {
        private static readonly DeviceRgb Brand = new(37, 160, 226);
        private static readonly DeviceRgb HeadFill = new(241, 248, 253);

        public static byte[] Build(ErxPdfHeader header, IReadOnlyList<ErxPdfLine> lines)
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            writer.SetCloseStream(false);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);
            document.SetMargins(36, 36, 36, 36);

            document.Add(new Paragraph(string.IsNullOrWhiteSpace(header.ClinicName) ? "Niga Homeocentrum" : header.ClinicName)
                .SetFontSize(16).SimulateBold().SetFontColor(Brand).SetMarginBottom(0));
            document.Add(new Paragraph(header.DoctorName).SetFontSize(11).SimulateBold().SetMarginBottom(0));
            if (!string.IsNullOrWhiteSpace(header.ClinicContact))
                document.Add(new Paragraph(header.ClinicContact).SetFontSize(9).SetMarginBottom(6));

            var meta = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth()
                .SetBorderTop(new SolidBorder(Brand, 1.5f)).SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f))
                .SetMarginBottom(12);
            meta.AddCell(MetaCell($"Patient: {header.PatientName}\n{header.PatientMeta ?? ""}\nVisit: {header.PatientAppId}"));
            meta.AddCell(MetaCell($"eRx no.: {header.ErxId}\nDate: {(header.SignedAt ?? DateTime.Now):dd MMM yyyy, hh:mm tt}")
                .SetTextAlignment(TextAlignment.RIGHT));
            document.Add(meta);

            document.Add(new Paragraph("Rx").SetFontSize(14).SimulateBold().SetMarginBottom(4));
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 0.5f, 3, 1.2f, 1.6f, 1.4f, 2.6f })).UseAllAvailableWidth();
            foreach (var h in new[] { "#", "Remedy", "Potency", "Frequency", "Duration", "Instructions" })
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SimulateBold().SetFontSize(9)).SetBackgroundColor(HeadFill));
            if (lines.Count == 0)
            {
                table.AddCell(new Cell(1, 6).Add(new Paragraph("No remedies").SetFontSize(9)));
            }
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                table.AddCell(BodyCell((i + 1).ToString()));
                table.AddCell(BodyCell(string.IsNullOrWhiteSpace(line.Dose) ? line.Remedy : $"{line.Remedy}\n{line.Dose}"));
                table.AddCell(BodyCell(line.Potency));
                table.AddCell(BodyCell(line.Frequency));
                table.AddCell(BodyCell(line.Duration));
                table.AddCell(BodyCell(line.Instructions));
            }
            document.Add(table);

            document.Add(new Paragraph($"{header.DoctorName}\nDigitally signed{(header.SignedAt.HasValue ? " on " + header.SignedAt.Value.ToString("dd MMM yyyy, hh:mm tt") : "")}")
                .SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(40));
            document.Close();
            return stream.ToArray();
        }

        private static Cell MetaCell(string text) =>
            new Cell().Add(new Paragraph(text).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetPaddingTop(6).SetPaddingBottom(6);

        private static Cell BodyCell(string? text) =>
            new Cell().Add(new Paragraph(string.IsNullOrWhiteSpace(text) ? "—" : text).SetFontSize(9));
    }
}
