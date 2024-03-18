using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.RemindMe;

public class Settings : INotifyPropertyChanged {
    private bool _showReminderWindow;
    private bool _showReminderNotification = true;
    private bool _showNotificationsOnAddAndDelete = true;

    public bool ShowReminderWindow {
        get => _showReminderWindow;
        set {
            _showReminderWindow = value;
            OnPropertyChanged();
        }
    }

    public bool ShowReminderNotification {
        get => _showReminderNotification;
        set {
            _showReminderNotification = value;
            OnPropertyChanged();
        }
    }

    public bool ShowNotificationsOnAddAndDelete {
        get => _showNotificationsOnAddAndDelete;
        set {
            _showNotificationsOnAddAndDelete = value;
            OnPropertyChanged();
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}