using System;
using System.Text.Json.Serialization;
using System.Threading;

namespace Flow.Launcher.Plugin.RemindMe;

public class TimerState {
    public string Name { get; set; }
    public DateTime DateTime { get; set; }
    [JsonIgnore]
    public Timer Timer { get; set; }
}
