// Copyright (c) 2024 Sound Metrics Corp.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
    public sealed class DistanceJsonConverter : JsonConverter<Distance>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "Internal.")]
        public override Distance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                return (Distance)f;
            }
            else
            {
                throw new JsonException("Could not parse SystemType value");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods",
            Justification = "Caller is JSON serialization")]
        public override void Write(Utf8JsonWriter writer, Distance value, JsonSerializerOptions options)
        {
            var s = value.Meters.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(s);
        }
    }
}
