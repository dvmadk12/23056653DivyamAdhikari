namespace JUpdate.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public DateTime EntryDate { get; set; } // Date of the journal entry (one per day)
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ContentFormat { get; set; } = "Markdown"; // Markdown or RichText
        public int PrimaryMoodId { get; set; }
        public int? SecondaryMood1Id { get; set; }
        public int? SecondaryMood2Id { get; set; }
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Tags { get; set; } // Comma-separated tags
        public bool IsDraft { get; set; } // Whether the entry is a draft
    }
}

