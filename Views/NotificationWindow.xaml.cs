using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.RemindMe.Views;

public partial class NotificationWindow {
    public string NotificationTitle { get; }
    public string NotificationSubtitle { get; }
    public double HeaderFontSize { get; }
    public double BodyFontSize { get; }

    public NotificationWindow(string title, string subtitle, Settings settings) {
        NotificationTitle = title switch {
            null or "" => "Reminder",
            _ => title
        };
        NotificationSubtitle = subtitle;
        HeaderFontSize = settings.ReminderWindowHeaderFontSize;
        BodyFontSize = settings.ReminderWindowBodyFontSize;

        InitializeComponent();
        ApplySizing(settings);

        try {
            var dllDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var iconPath = Path.Combine(dllDirectory ?? ".", "icon.png");
            Icon = new BitmapImage(new Uri(iconPath));
        } catch {
            // Ignore exceptions, use default icon
        }
    }

    private void ApplySizing(Settings settings) {
        var width = settings.ReminderWindowWidth;
        var height = settings.ReminderWindowHeight;

        SizeToContent = (width > 0, height > 0) switch {
            (true, true) => SizeToContent.Manual,
            (true, false) => SizeToContent.Height,
            (false, true) => SizeToContent.Width,
            _ => SizeToContent.WidthAndHeight,
        };

        Width = width > 0 ? width : double.NaN;
        Height = height > 0 ? height : double.NaN;
    }
}