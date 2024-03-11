// Copyright (c) 2024 Sound Metrics Corp.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
    public sealed class RateJsonConverter : JsonConverter<Rate>
    {
        public override Rate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (Rate.TryParseSerializationString(s, out var rate))
            {
                return rate;
            }
            else
            {
                throw new JsonException($"Could not parse {nameof(Rate)} value");
            }
        }

        public override void Write(Utf8JsonWriter writer, Rate value, JsonSerializerOptions options)
        {
            var s = value.ToSerializationString();
            writer.WriteStringValue(s);
        }
    }
}
