using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Homeocentrum.Niga.API.Domain.Helpers
{
    public sealed class ErxPdfHeader
    {
        public int ErxId { get; set; }
        public int PatientAppId { get; set; }
        public string ClinicName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string? RegistrationNo { get; set; }
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
        private static readonly DeviceRgb SignInk = new(30, 58, 138);
        private static readonly DeviceRgb LockGreen = new(22, 163, 74);

        public static byte[] Build(ErxPdfHeader header, IReadOnlyList<ErxPdfLine> lines, IReadOnlyList<string>? labOrders = null)
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            writer.SetCloseStream(false);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);
            document.SetMargins(36, 36, 36, 36);

            var info = pdf.GetDocumentInfo();
            info.SetTitle($"eRx {header.ErxId} — {header.PatientName}");
            info.SetAuthor(header.DoctorName);
            info.SetSubject(header.SignedAt.HasValue
                ? $"Digitally signed prescription · {header.SignedAt:yyyy-MM-dd HH:mm}"
                : "Prescription");
            info.SetCreator("Homeocentrum eRx");

            var italic = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph(string.IsNullOrWhiteSpace(header.ClinicName) ? "Niga Homeocentrum" : header.ClinicName)
                .SetFont(bold).SetFontSize(16).SetFontColor(Brand).SetMarginBottom(0));
            document.Add(new Paragraph(header.DoctorName).SetFont(bold).SetFontSize(11).SetMarginBottom(0));
            if (!string.IsNullOrWhiteSpace(header.RegistrationNo))
                document.Add(new Paragraph($"Reg. no. {header.RegistrationNo}").SetFont(regular).SetFontSize(9).SetMarginBottom(0));
            if (!string.IsNullOrWhiteSpace(header.ClinicContact))
                document.Add(new Paragraph(header.ClinicContact).SetFont(regular).SetFontSize(9).SetMarginBottom(6));

            var meta = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth()
                .SetBorderTop(new SolidBorder(Brand, 1.5f)).SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f))
                .SetMarginBottom(12);
            meta.AddCell(MetaCell($"Patient: {header.PatientName}\n{header.PatientMeta ?? ""}\nVisit: {header.PatientAppId}", regular));
            meta.AddCell(MetaCell(
                    $"eRx no.: {header.ErxId}\nDate: {(header.SignedAt ?? DateTime.Now):dd MMM yyyy, hh:mm tt}" +
                    (header.SignedAt.HasValue ? "\nStatus: Signed & Locked" : ""),
                    regular)
                .SetTextAlignment(TextAlignment.RIGHT));
            document.Add(meta);

            document.Add(new Paragraph("Rx").SetFont(bold).SetFontSize(14).SetMarginBottom(4));
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 0.5f, 3, 1.2f, 1.6f, 1.4f, 2.6f })).UseAllAvailableWidth();
            foreach (var h in new[] { "#", "Remedy", "Potency", "Frequency", "Duration", "Instructions" })
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)).SetBackgroundColor(HeadFill));
            if (lines.Count == 0)
            {
                table.AddCell(new Cell(1, 6).Add(new Paragraph("No remedies").SetFont(regular).SetFontSize(9)));
            }
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                table.AddCell(BodyCell((i + 1).ToString(), regular));
                table.AddCell(BodyCell(string.IsNullOrWhiteSpace(line.Dose) ? line.Remedy : $"{line.Remedy}\n{line.Dose}", regular));
                table.AddCell(BodyCell(line.Potency, regular));
                table.AddCell(BodyCell(line.Frequency, regular));
                table.AddCell(BodyCell(line.Duration, regular));
                table.AddCell(BodyCell(line.Instructions, regular));
            }
            document.Add(table);

            // ERX-11 — lab orders copied onto the signed snapshot (never clinical history notes).
            var labs = labOrders?.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList() ?? new List<string>();
            if (labs.Count > 0)
            {
                document.Add(new Paragraph("Lab orders").SetFont(bold).SetFontSize(12).SetMarginTop(14).SetMarginBottom(4));
                var labTable = new Table(UnitValue.CreatePercentArray(new float[] { 0.5f, 5.5f })).UseAllAvailableWidth();
                labTable.AddHeaderCell(new Cell().Add(new Paragraph("#").SetFont(bold).SetFontSize(9)).SetBackgroundColor(HeadFill));
                labTable.AddHeaderCell(new Cell().Add(new Paragraph("Test / order").SetFont(bold).SetFontSize(9)).SetBackgroundColor(HeadFill));
                for (var i = 0; i < labs.Count; i++)
                {
                    labTable.AddCell(BodyCell((i + 1).ToString(), regular));
                    labTable.AddCell(BodyCell(labs[i], regular));
                }
                document.Add(labTable);
            }

            // Signature block — visual doctor signature + signed attestation (required on download).
            var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1.15f }))
                .UseAllAvailableWidth()
                .SetMarginTop(36);

            signTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("This prescription was electronically signed in Homeocentrum.")
                    .SetFont(regular).SetFontSize(8).SetFontColor(ColorConstants.GRAY))
                .Add(new Paragraph("Remedies and dosage are locked and cannot be changed after sign.")
                    .SetFont(regular).SetFontSize(8).SetFontColor(ColorConstants.GRAY)));

            var signPad = new Div()
                .SetBorder(new SolidBorder(new DeviceRgb(207, 230, 248), 1))
                .SetBackgroundColor(new DeviceRgb(248, 251, 255))
                .SetPadding(10)
                .SetTextAlignment(TextAlignment.CENTER);

            signPad.Add(new Paragraph(header.DoctorName)
                .SetFont(italic)
                .SetFontSize(20)
                .SetFontColor(SignInk)
                .SetMarginBottom(2));

            var caption = header.SignedAt.HasValue
                ? $"Doctor Signature · Digitally signed {header.SignedAt.Value:dd MMM yyyy, hh:mm tt}"
                : "Doctor Signature";
            signPad.Add(new Paragraph(caption)
                .SetFont(bold)
                .SetFontSize(8)
                .SetFontColor(Brand)
                .SetMarginBottom(4));

            if (!string.IsNullOrWhiteSpace(header.RegistrationNo))
            {
                signPad.Add(new Paragraph($"Reg. no. {header.RegistrationNo}")
                    .SetFont(regular).SetFontSize(8).SetFontColor(ColorConstants.GRAY).SetMarginBottom(4));
            }

            if (header.SignedAt.HasValue)
            {
                signPad.Add(new Paragraph("SIGNED & LOCKED")
                    .SetFont(bold)
                    .SetFontSize(9)
                    .SetFontColor(LockGreen)
                    .SetMarginBottom(0));
            }

            signTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(signPad));
            document.Add(signTable);

            document.Close();
            return stream.ToArray();
        }

        private static Cell MetaCell(string text, PdfFont font) =>
            new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetPaddingTop(6).SetPaddingBottom(6);

        private static Cell BodyCell(string? text, PdfFont font) =>
            new Cell().Add(new Paragraph(string.IsNullOrWhiteSpace(text) ? "—" : text).SetFont(font).SetFontSize(9));
    }
}
