using Microsoft.Data.Sqlite;
using JUpdate.Models;
using System.Text;

namespace JUpdate.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            var appDataPath = FileSystem.AppDataDirectory;
            _dbPath = Path.Combine(appDataPath, "journal.db");
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Create Journal Entries table
            var createEntriesTable = @"
                CREATE TABLE IF NOT EXISTS JournalEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EntryDate TEXT NOT NULL UNIQUE,
                    Title TEXT,
                    Content TEXT NOT NULL,
                    ContentFormat TEXT DEFAULT 'Markdown',
                    PrimaryMoodId INTEGER NOT NULL,
                    SecondaryMood1Id INTEGER,
                    SecondaryMood2Id INTEGER,
                    Category TEXT,
                    Tags TEXT,
                    IsDraft INTEGER DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );";

            // Create Moods table
            var createMoodsTable = @"
                CREATE TABLE IF NOT EXISTS Moods (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Category INTEGER NOT NULL,
                    Emoji TEXT
                );";

            // Create UserSettings table
            var createSettingsTable = @"
                CREATE TABLE IF NOT EXISTS UserSettings (
                    Id INTEGER PRIMARY KEY,
                    PinHash TEXT,
                    Theme TEXT DEFAULT 'light',
                    CustomThemeData TEXT
                );";

            using var command = connection.CreateCommand();
            command.CommandText = createEntriesTable + createMoodsTable + createSettingsTable;
            command.ExecuteNonQuery();

            // Enable Write-Ahead Logging (WAL) for better concurrency
            using var walCmd = connection.CreateCommand();
            walCmd.CommandText = "PRAGMA journal_mode = WAL;";
            walCmd.ExecuteNonQuery();

            // Migration: Robustly check and add columns
            EnsureColumnExists(connection, "JournalEntries", "Tags", "TEXT");
            EnsureColumnExists(connection, "JournalEntries", "IsDraft", "INTEGER DEFAULT 0");

            // Initialize default moods if not exists
            InitializeDefaultMoods(connection);
        }

        private void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnType)
        {
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

            if (!exists)
            {
                var addCmd = connection.CreateCommand();
                addCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
                addCmd.ExecuteNonQuery();
            }
        }

        private void InitializeDefaultMoods(SqliteConnection connection)
        {
            var checkMoods = "SELECT COUNT(*) FROM Moods";
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = checkMoods;
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (count == 0)
            {
                var moods = new[]
                {
                    // Positive
                    ("Happy", 0, "😊"), ("Excited", 0, "🎉"), ("Relaxed", 0, "😌"),
                    ("Grateful", 0, "🙏"), ("Confident", 0, "💪"),
                    // Neutral
                    ("Calm", 1, "😐"), ("Thoughtful", 1, "🤔"), ("Curious", 1, "🤨"),
                    ("Nostalgic", 1, "😌"), ("Bored", 1, "😑"),
                    // Negative
                    ("Sad", 2, "😔"), ("Angry", 2, "😠"), ("Stressed", 2, "😰"),
                    ("Lonely", 2, "😞"), ("Anxious", 2, "😟")
                };

                foreach (var (name, category, emoji) in moods)
                {
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = "INSERT INTO Moods (Name, Category, Emoji) VALUES (@name, @category, @emoji)";
                    insertCmd.Parameters.AddWithValue("@name", name);
                    insertCmd.Parameters.AddWithValue("@category", category);
                    insertCmd.Parameters.AddWithValue("@emoji", emoji);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        // Journal Entry CRUD
        public void AddOrUpdateJournalEntry(JournalEntry entry)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var existing = GetJournalEntryByDate(entry.EntryDate);
            if (existing != null)
            {
                entry.Id = existing.Id;
                entry.CreatedAt = existing.CreatedAt;
                entry.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                entry.CreatedAt = DateTime.UtcNow;
                entry.UpdatedAt = DateTime.UtcNow;
            }

            using var command = connection.CreateCommand();
            if (existing != null)
            {
                command.CommandText = @"
                    UPDATE JournalEntries 
                    SET Title = @title, Content = @content, ContentFormat = @format,
                        PrimaryMoodId = @primaryMood, SecondaryMood1Id = @secondary1,
                        SecondaryMood2Id = @secondary2, Category = @category,
                        Tags = @tags, IsDraft = @isDraft, UpdatedAt = @updatedAt
                    WHERE Id = @id";
                command.Parameters.AddWithValue("@id", entry.Id);
            }
            else
            {
                command.CommandText = @"
                    INSERT INTO JournalEntries 
                    (EntryDate, Title, Content, ContentFormat, PrimaryMoodId, 
                     SecondaryMood1Id, SecondaryMood2Id, Category, Tags, IsDraft, CreatedAt, UpdatedAt)
                    VALUES (@date, @title, @content, @format, @primaryMood, @secondary1,
                            @secondary2, @category, @tags, @isDraft, @createdAt, @updatedAt)";
                command.Parameters.AddWithValue("@date", entry.EntryDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@createdAt", entry.CreatedAt.ToString("O"));
            }

            command.Parameters.AddWithValue("@title", entry.Title ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@content", entry.Content);
            command.Parameters.AddWithValue("@format", entry.ContentFormat);
            command.Parameters.AddWithValue("@primaryMood", entry.PrimaryMoodId);
            command.Parameters.AddWithValue("@secondary1", entry.SecondaryMood1Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@secondary2", entry.SecondaryMood2Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@category", entry.Category ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@tags", entry.Tags ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isDraft", entry.IsDraft ? 1 : 0);
            command.Parameters.AddWithValue("@updatedAt", entry.UpdatedAt.ToString("O"));

            command.ExecuteNonQuery();
        }

        public JournalEntry? GetJournalEntryByDate(DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM JournalEntries WHERE EntryDate = @date";
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadJournalEntry(reader);
            }
            return null;
        }

        public List<JournalEntry> GetAllJournalEntries()
        {
            var entries = new List<JournalEntry>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM JournalEntries ORDER BY EntryDate DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(ReadJournalEntry(reader));
            }
            return entries;
        }

        public void DeleteJournalEntry(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM JournalEntries WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        private JournalEntry ReadJournalEntry(SqliteDataReader reader)
        {
            var entry = new JournalEntry
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                EntryDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("EntryDate"))),
                Content = reader.GetString(reader.GetOrdinal("Content")),
                PrimaryMoodId = reader.GetInt32(reader.GetOrdinal("PrimaryMoodId")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")))
            };

            // Handle nullable and optional columns safely
            var titleOrd = reader.GetOrdinal("Title");
            entry.Title = reader.IsDBNull(titleOrd) ? null : reader.GetString(titleOrd);

            var formatOrd = reader.GetOrdinal("ContentFormat");
            entry.ContentFormat = reader.IsDBNull(formatOrd) ? "Markdown" : reader.GetString(formatOrd);

            var sec1Ord = reader.GetOrdinal("SecondaryMood1Id");
            entry.SecondaryMood1Id = reader.IsDBNull(sec1Ord) ? null : reader.GetInt32(sec1Ord);

            var sec2Ord = reader.GetOrdinal("SecondaryMood2Id");
            entry.SecondaryMood2Id = reader.IsDBNull(sec2Ord) ? null : reader.GetInt32(sec2Ord);

            var catOrd = reader.GetOrdinal("Category");
            entry.Category = reader.IsDBNull(catOrd) ? null : reader.GetString(catOrd);

            // Handle migrated columns that might be missing in very old schemas (though we migrate them now)
            try {
                var tagsOrd = reader.GetOrdinal("Tags");
                entry.Tags = reader.IsDBNull(tagsOrd) ? null : reader.GetString(tagsOrd);
            } catch { /* Column missing */ }

            try {
                var draftOrd = reader.GetOrdinal("IsDraft");
                entry.IsDraft = !reader.IsDBNull(draftOrd) && reader.GetInt32(draftOrd) == 1;
            } catch { /* Column missing */ }

            return entry;
        }

        // Mood operations
        public List<Mood> GetAllMoods()
        {
            var moods = new List<Mood>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Moods ORDER BY Category, Name";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                moods.Add(new Mood
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = (MoodCategory)reader.GetInt32(2),
                    Emoji = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }
            return moods;
        }

        // User Settings
        public UserSettings GetUserSettings()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM UserSettings WHERE Id = 1";

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new UserSettings
                {
                    Id = reader.GetInt32(0),
                    PinHash = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Theme = reader.IsDBNull(2) ? "light" : reader.GetString(2),
                    CustomThemeData = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }

            // Return default settings if none exist
            return new UserSettings { Id = 1, Theme = "light" };
        }

        public void SaveUserSettings(UserSettings settings)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO UserSettings (Id, PinHash, Theme, CustomThemeData)
                VALUES (1, @pinHash, @theme, @customTheme)";
            command.Parameters.AddWithValue("@pinHash", settings.PinHash ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@theme", settings.Theme ?? "light");
            command.Parameters.AddWithValue("@customTheme", settings.CustomThemeData ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        // New Helper Methods
        public void ClearUserPin()
        {
             using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE UserSettings SET PinHash = NULL WHERE Id = 1";
            command.ExecuteNonQuery();
        }

        public List<string> GetAllTags()
        {
            var allTags = new HashSet<string>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Tags FROM JournalEntries WHERE Tags IS NOT NULL AND Tags != ''";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var tags = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    foreach (var tag in tags.Split(','))
                    {
                        var trimmed = tag.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            allTags.Add(trimmed);
                        }
                    }
                }
            }
            return allTags.OrderBy(t => t).ToList();
        }
    }
}

