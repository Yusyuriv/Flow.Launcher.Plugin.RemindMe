using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Flow.Launcher.Plugin.RemindMe;

#nullable enable

public static class TimerManager {
    private static string? SaveFileLocation {
        get {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (location is null) return null;
            return Path.Combine(location, "reminders.json");
        }
    }

    public static List<TimerState>? ReadTimersFromFile() {
        if (SaveFileLocation is not { } fileLocation) return null;
        if (!File.Exists(fileLocation)) return null;
        var remindersJson = File.ReadAllText(fileLocation);
        return JsonSerializer.Deserialize<List<TimerState>>(remindersJson);
    }

    public static void WriteTimersToFile(List<TimerState> timers) {
        if (SaveFileLocation is not { } fileLocation) return;
        var remindersJson = JsonSerializer.Serialize(timers);
        File.WriteAllText(fileLocation, remindersJson);
    }
}
