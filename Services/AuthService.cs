using System.Security.Cryptography;
using System.Text;
using JUpdate.Models;

namespace JUpdate.Services
{
    public class AuthService
    {
        private readonly DatabaseService _dbService;
        private bool _isAuthenticated = false;

        public AuthService(DatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        public bool IsAuthenticated => _isAuthenticated;

        public void SetAuthenticated(bool value)
        {
            _isAuthenticated = value;
        }

        public bool HasPin()
        {
            var settings = _dbService.GetUserSettings();
            return !string.IsNullOrEmpty(settings.PinHash);
        }

        public bool VerifyPin(string pin)
        {
            var settings = _dbService.GetUserSettings();
            if (string.IsNullOrEmpty(settings.PinHash))
                return false;

            var hash = HashPin(pin);
            return hash == settings.PinHash;
        }

        public void SetPin(string pin)
        {
            var settings = _dbService.GetUserSettings();
            settings.PinHash = HashPin(pin);
            _dbService.SaveUserSettings(settings);
        }

        private string HashPin(string pin)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}

