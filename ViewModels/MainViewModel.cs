using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using endfield_player_position_display.Models;
using endfield_player_position_display.Services;

namespace endfield_player_position_display.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private bool isCoordinateWindowOpen;
        private bool followGameWindow;
        private string followPosition;
        private Key hotkey;
        private string statusText;
        private string warningText;
        private PositionSnapshot currentPosition;
        private CredentialResult credential;
        private RoleBinding roleBinding;
        private string mapId;
        private ZiplineLookupResult ziplineResult;
        private bool isError;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsCoordinateWindowOpen
        {
            get { return isCoordinateWindowOpen; }
            set { Set(ref isCoordinateWindowOpen, value); }
        }

        public bool FollowGameWindow
        {
            get { return followGameWindow; }
            set { Set(ref followGameWindow, value); }
        }

        public string FollowPosition
        {
            get { return followPosition; }
            set { Set(ref followPosition, value); }
        }

        public Key Hotkey
        {
            get { return hotkey; }
            set { Set(ref hotkey, value); }
        }

        public string StatusText
        {
            get { return statusText; }
            set { Set(ref statusText, value); }
        }

        public string WarningText
        {
            get { return warningText; }
            set { Set(ref warningText, value); }
        }

        public PositionSnapshot CurrentPosition
        {
            get { return currentPosition; }
            set { Set(ref currentPosition, value); }
        }

        public CredentialResult Credential
        {
            get { return credential; }
            set { Set(ref credential, value); }
        }

        public RoleBinding RoleBinding
        {
            get { return roleBinding; }
            set { Set(ref roleBinding, value); }
        }

        public string MapId
        {
            get { return mapId; }
            set { Set(ref mapId, value); }
        }

        public ZiplineLookupResult ZiplineResult
        {
            get { return ziplineResult; }
            set { Set(ref ziplineResult, value); }
        }

        public bool IsError
        {
            get { return isError; }
            set { Set(ref isError, value); }
        }

        public void ApplySettings(UserSettings settings)
        {
            UserSettings actual = settings ?? UserSettings.Default();
            IsCoordinateWindowOpen = actual.IsCoordinateWindowOpen;
            FollowGameWindow = actual.FollowGameWindow;
            FollowPosition = string.IsNullOrWhiteSpace(actual.FollowPosition) ? "正上" : actual.FollowPosition;
            Key parsed;
            Hotkey = Enum.TryParse(actual.Hotkey, out parsed) ? parsed : Key.F12;
        }

        public UserSettings ToSettings()
        {
            return new UserSettings
            {
                IsCoordinateWindowOpen = IsCoordinateWindowOpen,
                FollowGameWindow = FollowGameWindow,
                FollowPosition = FollowPosition,
                Hotkey = Hotkey.ToString()
            };
        }

        public void ApplyMonitorUpdate(MonitorUpdate update)
        {
            if (update == null)
            {
                return;
            }

            IsError = update.IsError;
            if (!string.IsNullOrWhiteSpace(update.Status))
            {
                StatusText = update.Status;
            }

            if (update.Position != null)
            {
                CurrentPosition = update.Position;
                StatusText = "坐标已更新";
            }

            if (update.SessionState != null)
            {
                Credential = update.SessionState.Credential ?? Credential;
                RoleBinding = update.SessionState.RoleBinding ?? RoleBinding;
                if (!string.IsNullOrWhiteSpace(update.SessionState.MapId))
                {
                    MapId = update.SessionState.MapId;
                }
            }
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
