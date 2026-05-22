using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Input;

namespace endfield_player_position_display.Services
{
    public sealed class UserSettingsStore
    {
        private readonly string path;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public UserSettingsStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "endfield-player-position-display",
                "settings.json"))
        {
        }

        internal UserSettingsStore(string path)
        {
            this.path = path;
        }

        public UserSettings Load()
        {
            if (!File.Exists(path))
            {
                return UserSettings.Default();
            }

            try
            {
                return serializer.Deserialize<UserSettings>(File.ReadAllText(path)) ?? UserSettings.Default();
            }
            catch
            {
                return UserSettings.Default();
            }
        }

        public void Save(UserSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, serializer.Serialize(settings ?? UserSettings.Default()));
        }
    }

    public sealed class UserSettings
    {
        public bool IsCoordinateWindowOpen { get; set; }
        public bool FollowGameWindow { get; set; }
        public string FollowPosition { get; set; }
        public string Hotkey { get; set; }

        public static UserSettings Default()
        {
            return new UserSettings
            {
                IsCoordinateWindowOpen = false,
                FollowGameWindow = false,
                FollowPosition = "正上",
                Hotkey = Key.F12.ToString()
            };
        }
    }
}
