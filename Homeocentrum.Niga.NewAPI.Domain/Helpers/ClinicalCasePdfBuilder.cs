using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    public static class ClinicalCasePdfBuilder
    {
        public static byte[] Build(string title, IEnumerable<string> lines)
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            writer.SetCloseStream(false);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);
            document.SetMargins(36, 36, 36, 36);
            document.Add(new Paragraph(title).SetFontSize(14).SetBold().SetMarginBottom(12));
            foreach (var line in lines)
            {
                document.Add(new Paragraph(line ?? string.Empty).SetFontSize(10).SetMarginBottom(4));
            }
            document.Close();
            return stream.ToArray();
        }
    }
}
