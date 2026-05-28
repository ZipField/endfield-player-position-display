using System;
using System.Collections.Generic;
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
        public Dictionary<string, FollowOffsetSettings> FollowOffsets { get; set; }
        public string Hotkey { get; set; }
        public bool? RecordCaptureData { get; set; }

        public static UserSettings Default()
        {
            return new UserSettings
            {
                IsCoordinateWindowOpen = false,
                FollowGameWindow = false,
                FollowPosition = "正上",
                FollowOffsets = FollowOffsetSettings.CreateDefaults(),
                Hotkey = Key.F12.ToString(),
#if DEBUG
                RecordCaptureData = true
#else
                RecordCaptureData = false
#endif
            };
        }
    }

    public sealed class FollowOffsetSettings
    {
        public double Horizontal { get; set; }
        public double Vertical { get; set; }

        public static Dictionary<string, FollowOffsetSettings> CreateDefaults()
        {
            var result = new Dictionary<string, FollowOffsetSettings>(StringComparer.Ordinal)
            {
                { "正上", new FollowOffsetSettings() },
                { "右上", new FollowOffsetSettings() },
                { "正右", new FollowOffsetSettings() },
                { "右下", new FollowOffsetSettings() },
                { "正下", new FollowOffsetSettings() },
                { "左下", new FollowOffsetSettings() },
                { "正左", new FollowOffsetSettings() },
                { "左上", new FollowOffsetSettings() }
            };
            return result;
        }
    }
}
