using JUpdate.Models;
using Microsoft.JSInterop;

namespace JUpdate.Services
{
    public class ThemeService
    {
        private readonly DatabaseService _dbService;
        private readonly IJSRuntime _jsRuntime;
        private string _currentTheme = "dark";
        private bool _themeLoaded = false;

        public ThemeService(DatabaseService dbService, IJSRuntime jsRuntime)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        public string CurrentTheme => _currentTheme;

        public event Action<string>? ThemeChanged;

        public void LoadTheme()
        {
            if (_themeLoaded) return;
            
            try
            {
                var settings = _dbService.GetUserSettings();
                _currentTheme = settings.Theme;
                _themeLoaded = true;
                _ = ApplyTheme(_currentTheme);
            }
            catch
            {
                _currentTheme = "dark";
                _themeLoaded = true;
            }
        }

        public async Task SetTheme(string theme)
        {
            _currentTheme = theme;
            var settings = _dbService.GetUserSettings();
            settings.Theme = theme;
            _dbService.SaveUserSettings(settings);
            
            await ApplyTheme(theme);
            ThemeChanged?.Invoke(theme);
        }

        private async Task ApplyTheme(string theme)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("applyTheme", theme);
            }
            catch
            {
                // JS not ready yet, will be applied on next render
            }
        }
    }
}
