namespace Flow.Launcher.Plugin.RemindMe.Views;

public partial class SettingsControl {
    public Settings Settings { get; }
    
    public SettingsControl(Settings settings) {
        Settings = settings;
        
        InitializeComponent();
    }
}