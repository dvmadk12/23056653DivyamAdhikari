namespace JUpdate.Models
{
    public enum MoodCategory
    {
        Positive = 0,
        Neutral = 1,
        Negative = 2
    }

    public class Mood
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MoodCategory Category { get; set; }
        public string Emoji { get; set; } = string.Empty;
    }
}

