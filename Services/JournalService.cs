using JUpdate.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JUpdate.Services
{
    public class JournalService
    {
        private readonly DatabaseService _databaseService;

        public JournalService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void SaveEntry(JournalEntry entry)
        {
            _databaseService.AddOrUpdateJournalEntry(entry);
        }

        public JournalEntry? GetEntryByDate(DateTime date)
        {
            return _databaseService.GetJournalEntryByDate(date);
        }

        public List<JournalEntry> GetAllEntries()
        {
            return _databaseService.GetAllJournalEntries();
        }

        public void DeleteEntry(int id)
        {
            _databaseService.DeleteJournalEntry(id);
        }

        public List<string> GetAllTags()
        {
            return _databaseService.GetAllTags();
        }

        public List<Mood> GetMoods()
        {
            return _databaseService.GetAllMoods();
        }
        
        public List<JournalEntry> GetRecentEntries(int count = 5)
        {
             return GetAllEntries().Take(count).ToList();
        }
    }
}
