using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ChronicNetCore;
using Flow.Launcher.Plugin.RemindMe.Views;

namespace Flow.Launcher.Plugin.RemindMe;

public partial class Main : IPlugin, ISettingProvider {
    private PluginInitContext _context;
    private Settings _settings;

    private List<TimerState> _timers = null!;

    private const string IcoPath = "icon.png";

    private readonly Regex _shortAddCommandRegex = ShortAddCommandRegex();

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string TimeUnitPattern =
        """
        (?<start_whitespace>^|\s)
        (?<sign>[+-]?)
        (?<number>\d+)
        (?<unit>
            s|sec|second|seconds|
            m|min|minute|minutes|
            h|hr|hrs|hour|hours|
            d|day|days|
            w|wk|wks|week|weeks|
            mo|mon|mth|mths|month|months|
            y|yr|yrs|year|years
        )
        (?<end_whitespace>$|\s)
        """;

    private readonly Parser _parser = new(new Options {
        Context = Pointer.Type.Future,
        FirstDayOfWeek = DayOfWeek.Monday,
    });

    public void Init(PluginInitContext context) {
        _context = context;
        _settings = _context.API.LoadSettingJsonStorage<Settings>();
        _timers = TimerPersistenceManager.ReadTimersFromFile() ?? new List<TimerState>();

        var now = DateTime.Now;
        foreach (var timerState in _timers) {
            if (timerState.DateTime <= now) {
                ShowReminder(timerState);
            } else {
                var timer = new Timer(state => {
                    if (state is not TimerState ts) return;

                    ShowReminder(ts);

                    ts.Timer.Dispose();
                    _timers.Remove(ts);
                    TimerPersistenceManager.WriteTimersToFile(_timers);
                }, timerState, timerState.DateTime - now, Timeout.InfiniteTimeSpan);

                timerState.Timer = timer;
            }
        }

        _timers = _timers.Where(v => v.DateTime > now).ToList();
        TimerPersistenceManager.WriteTimersToFile(_timers);
    }

    public List<Result> Query(Query query) {
        var command = query.SearchTerms.FirstOrDefault("").ToLower();

        return (query.SearchTerms.Length, command) switch {
            (0, _) => GetCommandList(query.ActionKeyword),
            (>= 1, "add")
                => GetAddReminderResponse(query.ActionKeyword, string.Join(" ", query.SearchTerms[1..])),
            (>= 1, "list")
                => GetReminderList(query.ActionKeyword, string.Join(" ", query.SearchTerms[1..])),
            (>= 1, _)
                when _settings.AcceptShortForm &&
                     _shortAddCommandRegex.IsMatch(query.Search) &&
                     !string.IsNullOrWhiteSpace(_shortAddCommandRegex.Replace(query.Search, ""))
                => GetShortAddReminderResponse(query.ActionKeyword, query.Search),
            (1, { } s) => GetCommandList(query.ActionKeyword, s),
            _ => new List<Result>(),
        };
    }

    private List<Result> GetShortAddReminderResponse(string queryKeyword, string querySearch) {
        var when = DateTime.Now;

        while (_shortAddCommandRegex.IsMatch(querySearch)) {
            var match = _shortAddCommandRegex.Match(querySearch);
            var startWhitespace = match.Groups["start_whitespace"].Value;
            var sign = match.Groups["sign"].Value;
            var number = int.Parse(match.Groups["number"].Value);
            var unit = match.Groups["unit"].Value;
            var endWhitespace = match.Groups["end_whitespace"].Value;

            if (sign == "-") {
                number *= -1;
            }

            when = unit switch {
                "s" or "sec" or "second" or "seconds" => when.AddSeconds(number),
                "m" or "min" or "minute" or "minutes" => when.AddMinutes(number),
                "h" or "hr" or "hrs" or "hour" or "hours" => when.AddHours(number),
                "d" or "day" or "days" => when.AddDays(number),
                "w" or "wk" or "wks" or "week" or "weeks" => when.AddDays(number * 7),
                "mo" or "mon" or "mth" or "mths" or "month" or "months" => when.AddMonths(number),
                "y" or "yr" or "yrs" or "year" or "years" => when.AddYears(number),
                _ => when,
            };

            querySearch = querySearch.Remove(match.Index + startWhitespace.Length, match.Length - startWhitespace.Length - endWhitespace.Length);
        }

        var reason = querySearch.Trim();
        var whenSpan = new Span(when, when);

        return new List<Result> {
            new() {
                Title = GetAddReminderTitle(reason, whenSpan),
                SubTitle = "Press Enter to add this reminder",
                IcoPath = IcoPath,
                Action = CreateAddReminderAction(queryKeyword, reason, whenSpan),
            },
        };
    }

    private List<Result> GetAddReminderResponse(string queryKeyword, string search) {
        var split = search.Split(" to ", 2);
        Span when;
        try {
            when = _parser.Parse(split[0]);
        } catch {
            return new List<Result> {
                new() {
                    Title = "Couldn't parse the time",
                    IcoPath = IcoPath,
                },
            };
        }

        if (string.IsNullOrEmpty(search)) {
            return new List<Result> {
                new() {
                    Title = "in 5 minutes to go get some food",
                    SubTitle = """Type when to remind you, "to" and the name of the reminder""",
                    IcoPath = IcoPath,
                    Action = CreatePutExamplePromptAction(queryKeyword),
                },
            };
        }
        if (when?.Start is null) {
            return new List<Result> {
                new() {
                    Title = "Couldn't parse the time",
                    IcoPath = IcoPath,
                }
            };
        }

        var reason = split.Length > 1 ? split[1] : "";

        return new List<Result> {
            new() {
                Title = GetAddReminderTitle(reason, when),
                SubTitle = "Press Enter to add this reminder",
                IcoPath = IcoPath,
                Action = CreateAddReminderAction(queryKeyword, reason, when),
            },
        };
    }

    private Func<ActionContext,bool> CreatePutExamplePromptAction(string queryKeyword) {
        return _ => {
            var prefix = GetQueryKeywordPrefix(queryKeyword);
            _context.API.ChangeQuery($"{prefix}add in 5 minutes to go get some food", true);

            return false;
        };
    }

    private static string GetAddReminderTitle(string reason, Span when) {
        return reason switch {
            null or "" => $"Add a reminder at {when.Start}",
            _ => $"Add a reminder to {reason} at {when.Start}",
        };
    }

    private static string GetReminderTitle(TimerState timerState) {
        return timerState.Name switch {
            null or "" => $"Reminder at {timerState.DateTime}",
            _ => $"Reminder to {timerState.Name} at {timerState.DateTime}",
        };
    }

    private List<Result> GetReminderList(string queryKeyword, string search) {
        if (_timers.Count == 0) {
            return new List<Result> {
                new Result {
                    Title = "No active reminders",
                    IcoPath = IcoPath,
                },
            };
        }

        return _timers
            .Select(v => {
                var title = GetReminderTitle(v);
                var searchHighlight = search switch {
                    null or "" => new List<int>(),
                    _ => _context.API.FuzzySearch(search, title).MatchData,
                };
                return new Result {
                    Title = title,
                    SubTitle = "Press Enter to remove this reminder",
                    IcoPath = IcoPath,
                    TitleHighlightData = searchHighlight,
                    Action = _ => {
                        v.Timer.Dispose();
                        _timers.Remove(v);
                        TimerPersistenceManager.WriteTimersToFile(_timers);

                        if (_settings.ShowNotificationsOnAddAndDelete) {
                            var name = string.IsNullOrEmpty(v.Name) ? "" : $"{v.Name} ";
                            var time = v.DateTime.ToString(CultureInfo.CurrentCulture);
                            _context.API.ShowMsg("Reminder removed:", $"{name}at {time}", IcoPath);
                        }

                        _context.API.ChangeQuery(GetQueryKeywordPrefix(queryKeyword) + "list ", true);

                        return true;
                    },
                };
            })
            .Where(v => string.IsNullOrEmpty(search) || v.TitleHighlightData.Count > 0)
            .ToList();
    }

    private List<Result> GetCommandList(string queryKeyword, string search = "") {
        var addSearch = _context.API.FuzzySearch(search, "add");
        var listSearch = _context.API.FuzzySearch(search, "list");

        var results = new List<Result>();

        var bothNotFound = !addSearch.IsSearchPrecisionScoreMet() && !listSearch.IsSearchPrecisionScoreMet();

        if (bothNotFound || addSearch.IsSearchPrecisionScoreMet()) {
            results.Add(new Result {
                Title = "add",
                TitleHighlightData = addSearch.MatchData,
                SubTitle = "Add a new reminder",
                IcoPath = IcoPath,
                Action = CreateSelectCommandAction(queryKeyword, "add"),
            });
        }

        if (bothNotFound || listSearch.IsSearchPrecisionScoreMet()) {
            results.Add(new Result {
                Title = "list",
                TitleHighlightData = listSearch.MatchData,
                SubTitle = "List all reminders",
                IcoPath = IcoPath,
                Action = CreateSelectCommandAction(queryKeyword, "list"),
            });
        }

        return results;
    }

    private static string GetQueryKeywordPrefix(string queryKeyword) {
        if (!string.IsNullOrEmpty(queryKeyword) && queryKeyword != "*") {
            return queryKeyword + " ";
        }

        return "";
    }

    private Func<ActionContext, bool> CreateSelectCommandAction(string queryKeyword, string command) {
        return _ => {
            _context.API.ChangeQuery(GetQueryKeywordPrefix(queryKeyword) + command + " ");
            return false;
        };
    }

    private void ShowReminder(TimerState ts) {
        if (_settings.ShowReminderNotification) {
            _context.API.ShowMsg($"Reminder: {ts.Name}", "at " + ts.DateTime.ToString(CultureInfo.CurrentCulture), IcoPath);
        } else {
            Application.Current.Dispatcher.Invoke(() => {
                new NotificationWindow(ts.Name, ts.DateTime.ToString(CultureInfo.CurrentCulture)).Show();
            });
        }
    }

    private Func<ActionContext, bool> CreateAddReminderAction(string queryKeyword, string reason, Span when) {
        return _ => {
            if (when.Start is not { } start) {
                _context.API.ShowMsg("Incorrect start time", "", IcoPath);
                return true;
            }

            var delay = start - DateTime.Now;
            if (delay < TimeSpan.Zero) {
                _context.API.ShowMsg("Can't set a reminder in the past", "", IcoPath);
                return true;
            }

            var timerState = new TimerState {
                Name = reason,
                Timer = null,
                DateTime = start,
            };
            var timer = new Timer(state => {
                if (state is not TimerState ts) return;

                ShowReminder(ts);

                ts.Timer.Dispose();
                _timers.Remove(ts);
                TimerPersistenceManager.WriteTimersToFile(_timers);
            }, timerState, delay, Timeout.InfiniteTimeSpan);

            timerState.Timer = timer;

            _timers.Add(timerState);
            TimerPersistenceManager.WriteTimersToFile(_timers);

            if (_settings.ShowNotificationsOnAddAndDelete) {
                var prefix = string.IsNullOrEmpty(timerState.Name) ? "" : $"{timerState.Name} ";
                var whenString = timerState.DateTime.ToString(CultureInfo.CurrentCulture);
                _context.API.ShowMsg($"Reminder set: {timerState.Name}", $"{prefix}at {whenString}", IcoPath);
            }

            _context.API.ChangeQuery(GetQueryKeywordPrefix(queryKeyword) + "add ", true);

            return true;
        };
    }

    public Control CreateSettingPanel() {
        return new SettingsControl(_settings);
    }

    [GeneratedRegex(TimeUnitPattern, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ShortAddCommandRegex();
}
