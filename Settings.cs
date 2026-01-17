using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.RemindMe;

public class Settings : INotifyPropertyChanged {
    private bool _showReminderWindow;
    private bool _showReminderNotification = true;
    private bool _showNotificationsOnAddAndDelete = true;
    private bool _acceptShortForm = false;
    private double _reminderWindowWidth = 300;
    private double _reminderWindowHeight = 200;
    private double _reminderWindowHeaderFontSize = 24;
    private double _reminderWindowBodyFontSize = 16;

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

    public bool AcceptShortForm {
        get => _acceptShortForm;
        set {
            _acceptShortForm = value;
            OnPropertyChanged();
        }
    }

    public double ReminderWindowWidth {
        get => _reminderWindowWidth;
        set {
            _reminderWindowWidth = value;
            OnPropertyChanged();
        }
    }

    public double ReminderWindowHeight {
        get => _reminderWindowHeight;
        set {
            _reminderWindowHeight = value;
            OnPropertyChanged();
        }
    }

    public double ReminderWindowHeaderFontSize {
        get => _reminderWindowHeaderFontSize;
        set {
            _reminderWindowHeaderFontSize = value;
            OnPropertyChanged();
        }
    }

    public double ReminderWindowBodyFontSize {
        get => _reminderWindowBodyFontSize;
        set {
            _reminderWindowBodyFontSize = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
