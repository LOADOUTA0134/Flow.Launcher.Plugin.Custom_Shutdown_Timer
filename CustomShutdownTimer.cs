using System;
using System.Collections.Generic;
using System.Diagnostics;
using Flow.Launcher.Plugin;

namespace FlowLauncher.Plugin.CustomShutdownTimer
{
    public class CustomShutdownTimer : IPlugin
    {
        private PluginInitContext _context;
        public void Init(PluginInitContext context)
        {
            _context = context;
        }
        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            // If the search query is empty, show initial suggestions.
            if (string.IsNullOrWhiteSpace(query.Search))
            {
                results.Add(new Result
                {
                    Title = "Shutdown Computer",
                    SubTitle = "Type time (e.g., '10s', '5m', '2h', '1d') to schedule a shutdown.",
                    IcoPath = "Images\\shutdown.png",
                    Action = _ => false 
                });
                results.Add(new Result
                {
                    Title = "Cancel Scheduled Shutdown",
                    SubTitle = "Cancels any pending timed shutdown.",
                    IcoPath = "Images\\error.png",
                    Action = _ =>
                    {
                        CancelShutdown();
                        return true; // Hide Flow Launcher after action
                    }
                });
                return results;
            }

            // Normalize the search string for parsing (lowercase, no spaces)
            string searchLower = query.Search.ToLower().Replace(" ", "");

            int value;
            long totalSeconds = 0; // Use long for totalSeconds to avoid overflow

            // Check for different time unit suffixes
            if (searchLower.EndsWith("s") || searchLower.EndsWith("sec"))
            {
                string numPart = searchLower.Replace("s", "").Replace("sec", "");
                if (int.TryParse(numPart, out value))
                {
                    totalSeconds = value;
                }
            }
            else if (searchLower.EndsWith("m") || searchLower.EndsWith("min"))
            {
                string numPart = searchLower.Replace("m", "").Replace("min", "");
                if (int.TryParse(numPart, out value))
                {
                    totalSeconds = (long)value * 60;
                }
            }
            else if (searchLower.EndsWith("h") || searchLower.EndsWith("hour"))
            {
                string numPart = searchLower.Replace("h", "").Replace("hour", "");
                if (int.TryParse(numPart, out value))
                {
                    totalSeconds = (long)value * 60 * 60; 
                }
            }
            else if (searchLower.EndsWith("d") || searchLower.EndsWith("day"))
            {
                string numPart = searchLower.Replace("d", "").Replace("day", "");
                if (int.TryParse(numPart, out value))
                {
                    totalSeconds = (long)value * 24 * 60 * 60; 
                }
            }
            // If no suffix, assume minutes (for backward compatibility and common use case)
            else if (int.TryParse(searchLower, out value))
            {
                totalSeconds = (long)value * 60; // Default to minutes
            }

            // Handle invalid parsing or zero/negative time
            if (totalSeconds <= 0)
            {
                results.Add(new Result
                {
                    Title = "Invalid time format or value",
                    SubTitle = $"Could not parse '{query.Search}'. Please use numbers with units like '10s', '5m', '2h', '1d' or just '10' for minutes.",
                    IcoPath = "Images\\error.png",
                    Action = _ => false 
                });
                return results;
            }

            // Determine the display string for the time
            string timeUnitDisplay;
            if (totalSeconds < 60)
            {
                timeUnitDisplay = $"{totalSeconds} second{(totalSeconds == 1 ? "" : "s")}";
            }
            else if (totalSeconds < 3600)
            {
                timeUnitDisplay = $"{totalSeconds / 60} minute{(totalSeconds / 60 == 1 ? "" : "s")}";
            }
            else if (totalSeconds < 86400)
            {
                timeUnitDisplay = $"{totalSeconds / 3600} hour{(totalSeconds / 3600 == 1 ? "" : "s")}";
            }
            else
            {
                timeUnitDisplay = $"{totalSeconds / 86400} day{(totalSeconds / 86400 == 1 ? "" : "s")}";
            }


            results.Add(new Result
            {
                Title = $"Schedule shutdown in {timeUnitDisplay}",
                SubTitle = $"Click to shut down after {timeUnitDisplay}.",
                IcoPath = "Images\\shutdown.png",
                Action = _ =>
                {
                    ScheduleShutdown(totalSeconds);
                    return true; 
                }
            });

            return results;
        }
        private void ScheduleShutdown(long seconds)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = $"/s /t {seconds}",
                    UseShellExecute = false, // Important: Directly starts the process
                    CreateNoWindow = true    
                };
                Process.Start(startInfo);

                // Determine the display message based on seconds
                string displayTime;
                if (seconds < 60)
                {
                    displayTime = $"{seconds} second{(seconds == 1 ? "" : "s")}";
                }
                else if (seconds < 3600)
                {
                    displayTime = $"{seconds / 60} minute{(seconds / 60 == 1 ? "" : "s")}";
                }
                else if (seconds < 86400)
                {
                    displayTime = $"{seconds / 3600} hour{(seconds / 3600 == 1 ? "" : "s")}";
                }
                else
                {
                    displayTime = $"{seconds / 86400} day{(seconds / 86400 == 1 ? "" : "s")}";
                }

                _context.API.ShowMsg("Shutdown Scheduled", $"Your computer will shut down in {displayTime}.", "Images\\success.png");
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("Error", $"Could not schedule shutdown: {ex.Message}", "Images\\error.png");
            }
        }

        /// <summary>
        /// Cancels any pending scheduled shutdown.
        /// </summary>
        private void CancelShutdown()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/a", 
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
                _context.API.ShowMsg("Shutdown Canceled", "Any pending shutdown has been canceled.", "Images\\success.png");
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("Error", $"Could not cancel shutdown: {ex.Message}", "Images\\error.png");
            }
        }
    }
}
