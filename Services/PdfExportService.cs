using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using JUpdate.Models;
using Colors = QuestPDF.Helpers.Colors;
using IContainer = QuestPDF.Infrastructure.IContainer;
using IComponent = QuestPDF.Infrastructure.IComponent;

namespace JUpdate.Services
{
    public class PdfExportService
    {
        public PdfExportService()
        {
            // License configuration is required for QuestPDF 2022.12+
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GeneratePdf(List<JournalEntry> entries)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("My Journal Entries")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            foreach (var entry in entries.OrderByDescending(e => e.EntryDate))
                            {
                                x.Item().Component(new JournalEntryComponent(entry));
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GeneratePdf(JournalEntry entry)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("My Journal Entry")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Component(new JournalEntryComponent(entry));

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }
    }

    public class JournalEntryComponent : IComponent
    {
        private JournalEntry Entry { get; }

        public JournalEntryComponent(JournalEntry entry)
        {
            Entry = entry;
        }

        public void Compose(IContainer container)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten4)
                .Padding(15)
                .Column(column =>
                {
                    // Date and Mood
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(Entry.EntryDate.ToString("D")).Bold().FontSize(14);
                        if (!string.IsNullOrEmpty(Entry.Category))
                        {
                            row.AutoItem().Text(Entry.Category).Italic().FontColor(Colors.Grey.Darken1);
                        }
                    });

                    // Title
                    if (!string.IsNullOrEmpty(Entry.Title))
                    {
                        column.Item().PaddingTop(5).Text(Entry.Title).FontSize(16).SemiBold();
                    }

                    // Content
                    column.Item().PaddingVertical(10).Text(Entry.Content);

                    // Tags
                    if (!string.IsNullOrEmpty(Entry.Tags))
                    {
                        column.Item().Row(row =>
                        {
                            foreach (var tag in Entry.Tags.Split(','))
                            {
                                row.AutoItem()
                                   .PaddingRight(5)
                                   .Text($"#{tag.Trim()}")
                                   .FontColor(Colors.Blue.Medium)
                                   .FontSize(10);
                            }
                        });
                    }
                });
        }
    }
}
