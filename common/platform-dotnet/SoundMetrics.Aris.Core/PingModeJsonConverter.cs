// Copyright (c) 2024 Sound Metrics Corp.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
    public sealed class PingModeJsonConverter : JsonConverter<PingMode>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "Internal.")]
        public override PingMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                && PingMode.TryGet(i, out var pingMode))
            {
                return pingMode;
            }
            else
            {
                throw new JsonException("Could not parse PingMode value");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods",
            Justification = "Caller is JSON serialization")]
        public override void Write(Utf8JsonWriter writer, PingMode value, JsonSerializerOptions options)
        {
            var s = value.IntegralValue.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(s);
        }
    }
}
