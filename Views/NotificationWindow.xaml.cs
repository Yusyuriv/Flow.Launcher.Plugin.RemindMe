using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.RemindMe.Views;

public partial class NotificationWindow {
    public string NotificationTitle { get; }
    public string NotificationSubtitle { get; }

    public NotificationWindow(string title, string subtitle) {
        NotificationTitle = title switch {
            null or "" => "Reminder",
            _ => title
        };
        NotificationSubtitle = subtitle;

        InitializeComponent();

        try {
            var dllDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var iconPath = Path.Combine(dllDirectory ?? ".", "icon.png");
            Icon = new BitmapImage(new Uri(iconPath));
        } catch {
            // Ignore exceptions, use default icon
        }
    }
}