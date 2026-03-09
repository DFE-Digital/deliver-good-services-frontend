using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ServiceManual.Models;

namespace ServiceManual.Services
{
    /// <summary>
    /// Builds a .docx for a job specification (title, grade, role description, skills) for download.
    /// Uses Heading 1 for the job role, Heading 2 for "You will" and "Skills you need".
    /// </summary>
    public static class WordDocumentService
    {
        public static byte[] BuildJobSpecificationDocx(JobSpecification jobSpec)
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                AddStylesPart(mainPart);
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Job role (Heading 1)
                body.AppendChild(CreateHeading1(jobSpec.Title ?? ""));

                // Grade (body text under the heading)
                if (!string.IsNullOrEmpty(jobSpec.Grade))
                {
                    body.AppendChild(CreateParagraph($"Grade: {jobSpec.Grade}"));
                }

                // You will (Heading 2)
                if (!string.IsNullOrWhiteSpace(jobSpec.RoleDescription))
                {
                    body.AppendChild(CreateParagraph()); // spacing before section
                    body.AppendChild(CreateHeading2("You will:"));
                    foreach (var para in ToPlainTextParagraphs(jobSpec.RoleDescription))
                        body.AppendChild(CreateParagraph(para));
                }

                // Skills you need (Heading 2)
                if (!string.IsNullOrWhiteSpace(jobSpec.Skills))
                {
                    body.AppendChild(CreateParagraph()); // spacing before section
                    body.AppendChild(CreateHeading2("Skills you need"));
                    foreach (var para in ToPlainTextParagraphs(jobSpec.Skills))
                        body.AppendChild(CreateParagraph(para));
                }

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Adds a styles part so Word recognizes Heading1 and Heading2 (document map, navigation, accessibility).
        /// </summary>
        private static void AddStylesPart(MainDocumentPart mainPart)
        {
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            var styles = new Styles();
            styles.AppendChild(new DocDefaults(
                new RunPropertiesDefault(new RunProperties(new FontSize { Val = "22" }, new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" })),
                new ParagraphPropertiesDefault(new SpacingBetweenLines { After = "160" })));
            styles.AppendChild(CreateHeadingStyle("Heading1", "Heading 1", 0));
            styles.AppendChild(CreateHeadingStyle("Heading2", "Heading 2", 1));
            stylesPart.Styles = styles;
        }

        private static Style CreateHeadingStyle(string styleId, string name, int outlineLvl)
        {
            return new Style(
                new StyleName { Val = name },
                new UIPriority { Val = 9 },
                new StyleParagraphProperties(
                    new OutlineLevel { Val = outlineLvl },
                    new SpacingBetweenLines { Before = "240", After = "120" }))
            {
                Type = StyleValues.Paragraph,
                StyleId = styleId
            };
        }

        private static Paragraph CreateParagraph(string? text = null)
        {
            var p = new Paragraph();
            var run = new Run();
            if (!string.IsNullOrEmpty(text))
                run.AppendChild(new Text(System.Net.WebUtility.HtmlDecode(text)) { Space = SpaceProcessingModeValues.Preserve });
            p.AppendChild(run);
            return p;
        }

        private static Paragraph CreateHeading1(string text)
        {
            var p = new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
                new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
            return p;
        }

        private static Paragraph CreateHeading2(string text)
        {
            var p = new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" }),
                new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
            return p;
        }

        /// <summary>
        /// Converts HTML or markdown-like content to a list of plain-text paragraphs (one per block/line).
        /// </summary>
        private static IEnumerable<string> ToPlainTextParagraphs(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                yield break;
            }

            // Strip HTML tags and decode entities
            var stripped = Regex.Replace(content, @"<[^>]+>", " ");
            stripped = System.Net.WebUtility.HtmlDecode(stripped);

            // Normalise markdown list markers to a single character so we keep structure
            stripped = Regex.Replace(stripped, @"^[\*\-\•]\s*", "• ", RegexOptions.Multiline);

            // Split by newlines; each non-empty line becomes a paragraph (preserves lists and line breaks)
            foreach (var line in stripped.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = Regex.Replace(line.Trim(), @"\s+", " ");
                if (trimmed.Length > 0)
                    yield return trimmed;
            }
        }
    }
}
