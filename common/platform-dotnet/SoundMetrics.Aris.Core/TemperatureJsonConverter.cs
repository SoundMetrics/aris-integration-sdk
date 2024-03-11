// Copyright (c) 2021-2024 Sound Metrics Corp.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
    public sealed class TemperatureJsonConverter : JsonConverter<Temperature>
    {
        public override Temperature Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                return (Temperature)d;
            }
            else
            {
                throw new JsonException($"Could not parse {nameof(Temperature)} value");
            }
        }

        public override void Write(Utf8JsonWriter writer, Temperature value, JsonSerializerOptions options)
        {
            var s = value.DegreesCelsius.ToString(CultureInfo.InvariantCulture);
            writer.WriteStringValue(s);
        }
    }
}
