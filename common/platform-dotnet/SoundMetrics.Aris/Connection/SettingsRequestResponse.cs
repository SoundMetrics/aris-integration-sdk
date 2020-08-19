using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.Aris.Connection
{
    public sealed class SettingsRequestResponse : CommandResponse
    {
        internal SettingsRequestResponse(bool isSuccessful, List<string> response)
            : base(isSuccessful, response)
        {
            SettingsCookie = isSuccessful ? GetSettingsCookie(response) : null;
            Log.Debug("settings_cookie parsed as [{settingsCookie}]", SettingsCookie);
        }

        public int? SettingsCookie { get; }

        private static int? GetSettingsCookie(IEnumerable<string> response) =>
            response
                .Where(line => line.StartsWith("settings_cookie"))
                .Select(line =>
                {
                    // The shape of the line is "settings_cookie 42"
                    var splits = line.Split(
                                    new[] { ' ', '\t' },
                                    StringSplitOptions.RemoveEmptyEntries);
                    return int.Parse(splits[1]);
                })
                .SingleOrDefault();
    }
}
