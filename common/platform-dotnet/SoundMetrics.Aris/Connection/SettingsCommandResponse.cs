using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class SettingsCommandResponse : CommandResponse
    {
        internal SettingsCommandResponse(bool isSuccessful, List<string> response)
            : base(isSuccessful, response)
        {
            SettingsCookie = isSuccessful ? GetSettingsCookie(response) : null;
            Log.Debug("settings-cookie parsed as [{settingsCookie}]", SettingsCookie);
        }

        public int? SettingsCookie { get; }

        private static int? GetSettingsCookie(IEnumerable<string> response) =>
            response
                .Where(line => line.StartsWith("settings-cookie"))
                .Select(line =>
                {
                    // The shape of the line is "settings-cookie 42"
                    var splits = line.Split(
                                    new[] { ' ', '\t' },
                                    StringSplitOptions.RemoveEmptyEntries);
                    return int.Parse(splits[1]);
                })
                .SingleOrDefault();
    }
}
