using JUpdate.Models;
using System.Text.RegularExpressions;

namespace JUpdate.Services
{
    public class AnalyticsService
    {
        private readonly DatabaseService _dbService;

        public AnalyticsService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public int GetCurrentStreak()
        {
            var entries = _dbService.GetAllJournalEntries()
                .OrderByDescending(e => e.EntryDate)
                .ToList();

            if (entries.Count == 0) return 0;

            var today = DateTime.Today;
            var streak = 0;
            var currentDate = today;

            foreach (var entry in entries)
            {
                var entryDate = entry.EntryDate.Date;
                if (entryDate == currentDate)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else if (entryDate < currentDate)
                {
                    break;
                }
            }

            return streak;
        }

        public int GetLongestStreak()
        {
            var entries = _dbService.GetAllJournalEntries()
                .OrderBy(e => e.EntryDate)
                .ToList();

            if (entries.Count == 0) return 0;

            int longestStreak = 1;
            int currentStreak = 1;
            var previousDate = entries[0].EntryDate.Date;

            for (int i = 1; i < entries.Count; i++)
            {
                var currentDate = entries[i].EntryDate.Date;
                var daysDiff = (currentDate - previousDate).Days;

                if (daysDiff == 1)
                {
                    currentStreak++;
                }
                else
                {
                    longestStreak = Math.Max(longestStreak, currentStreak);
                    currentStreak = 1;
                }

                previousDate = currentDate;
            }

            return Math.Max(longestStreak, currentStreak);
        }

        public List<DateTime> GetMissedDays(DateTime startDate, DateTime endDate)
        {
            var entries = _dbService.GetAllJournalEntries()
                .Where(e => e.EntryDate.Date >= startDate && e.EntryDate.Date <= endDate)
                .Select(e => e.EntryDate.Date)
                .ToHashSet();

            var missedDays = new List<DateTime>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                if (!entries.Contains(currentDate))
                {
                    missedDays.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }

            return missedDays;
        }

        public Dictionary<string, int> GetMoodDistribution(DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = _dbService.GetAllJournalEntries();
            var moods = _dbService.GetAllMoods().ToDictionary(m => m.Id, m => m);

            if (startDate.HasValue)
                entries = entries.Where(e => e.EntryDate >= startDate.Value).ToList();
            if (endDate.HasValue)
                entries = entries.Where(e => e.EntryDate <= endDate.Value).ToList();

            var distribution = new Dictionary<string, int>
            {
                ["Positive"] = 0,
                ["Neutral"] = 0,
                ["Negative"] = 0
            };

            foreach (var entry in entries)
            {
                if (moods.TryGetValue(entry.PrimaryMoodId, out var mood))
                {
                    distribution[mood.Category.ToString()]++;
                }
            }

            return distribution;
        }

        public Dictionary<string, int> GetMostFrequentMoods(int count = 5, DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = _dbService.GetAllJournalEntries();
            var moods = _dbService.GetAllMoods().ToDictionary(m => m.Id, m => m.Name);

            if (startDate.HasValue)
                entries = entries.Where(e => e.EntryDate >= startDate.Value).ToList();
            if (endDate.HasValue)
                entries = entries.Where(e => e.EntryDate <= endDate.Value).ToList();

            var moodCounts = entries
                .GroupBy(e => e.PrimaryMoodId)
                .Select(g => new { Mood = moods.GetValueOrDefault(g.Key, "Unknown"), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToDictionary(x => x.Mood, x => x.Count);

            return moodCounts;
        }

        public Dictionary<string, int> GetMostUsedTags(int count = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = _dbService.GetAllJournalEntries();

            if (startDate.HasValue)
                entries = entries.Where(e => e.EntryDate >= startDate.Value).ToList();
            if (endDate.HasValue)
                entries = entries.Where(e => e.EntryDate <= endDate.Value).ToList();

            var tagCounts = new Dictionary<string, int>();

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Tags))
                {
                    var tags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var tag in tags)
                    {
                        tagCounts[tag] = tagCounts.GetValueOrDefault(tag, 0) + 1;
                    }
                }
            }

            return tagCounts
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<string, double> GetWordCountTrends(DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = _dbService.GetAllJournalEntries();

            if (startDate.HasValue)
                entries = entries.Where(e => e.EntryDate >= startDate.Value).ToList();
            if (endDate.HasValue)
                entries = entries.Where(e => e.EntryDate <= endDate.Value).ToList();

            var trends = new Dictionary<string, double>();

            if (entries.Count == 0) return trends;

            var groupedByMonth = entries
                .GroupBy(e => new { Year = e.EntryDate.Year, Month = e.EntryDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            foreach (var group in groupedByMonth)
            {
                var monthKey = $"{group.Key.Year}-{group.Key.Month:D2}";
                var avgWords = group.Average(e => CountWords(e.Content));
                trends[monthKey] = Math.Round(avgWords, 1);
            }

            return trends;
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Remove markdown formatting and count words
            var cleanText = Regex.Replace(text, @"[#*_`\[\]()]", " ");
            return cleanText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}

