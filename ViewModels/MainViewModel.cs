using System;
using System.Collections.Generic;
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
        private Dictionary<string, FollowOffsetSettings> followOffsets = FollowOffsetSettings.CreateDefaults();
        private bool recordCaptureData;
        private Key hotkey;
        private string statusText;
        private string warningText;
        private PositionSnapshot currentPosition;
        private CredentialResult credential;
        private RoleBinding roleBinding;
        private IReadOnlyList<RoleSession> availableRoles = new List<RoleSession>();
        private string selectedRoleKey;
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

        public Dictionary<string, FollowOffsetSettings> FollowOffsets
        {
            get { return followOffsets; }
            set { Set(ref followOffsets, value ?? FollowOffsetSettings.CreateDefaults()); }
        }

        public bool RecordCaptureData
        {
            get { return recordCaptureData; }
            set { Set(ref recordCaptureData, value); }
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

        public IReadOnlyList<RoleSession> AvailableRoles
        {
            get { return availableRoles; }
            set { Set(ref availableRoles, value ?? new List<RoleSession>()); }
        }

        public string SelectedRoleKey
        {
            get { return selectedRoleKey; }
            set { Set(ref selectedRoleKey, value); }
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
            FollowOffsets = MergeOffsets(actual.FollowOffsets);
            RecordCaptureData = actual.RecordCaptureData.HasValue
                ? actual.RecordCaptureData.Value
                : UserSettings.Default().RecordCaptureData.GetValueOrDefault();
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
                FollowOffsets = FollowOffsets,
                RecordCaptureData = RecordCaptureData,
                Hotkey = Hotkey.ToString()
            };
        }

        public FollowOffsetSettings GetFollowOffset(string position)
        {
            string key = string.IsNullOrWhiteSpace(position) ? "正上" : position;
            FollowOffsetSettings offset;
            if (!FollowOffsets.TryGetValue(key, out offset) || offset == null)
            {
                offset = new FollowOffsetSettings();
                FollowOffsets[key] = offset;
            }

            return offset;
        }

        public void SetFollowOffset(string position, double horizontal, double vertical)
        {
            FollowOffsetSettings offset = GetFollowOffset(position);
            offset.Horizontal = horizontal;
            offset.Vertical = vertical;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FollowOffsets"));
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
                if (update.SessionState.AvailableRoles != null)
                {
                    AvailableRoles = update.SessionState.AvailableRoles;
                }

                if (update.SessionState.ActiveRole != null)
                {
                    SelectedRoleKey = update.SessionState.ActiveRole.Key;
                }

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

        private static Dictionary<string, FollowOffsetSettings> MergeOffsets(Dictionary<string, FollowOffsetSettings> stored)
        {
            Dictionary<string, FollowOffsetSettings> result = FollowOffsetSettings.CreateDefaults();
            if (stored == null)
            {
                return result;
            }

            foreach (var pair in stored)
            {
                if (pair.Value != null)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }
    }
}
