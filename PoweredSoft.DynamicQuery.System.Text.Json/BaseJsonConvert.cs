using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PoweredSoft.DynamicQuery.Core;

namespace PoweredSoft.DynamicQuery.System.Text.Json
{
    public abstract class BaseJsonConverterFactory : JsonConverterFactory
    {
        private readonly ServiceProvider _serviceProvider;

        protected BaseJsonConverterFactory(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => CreateConverter(_serviceProvider, typeToConvert, options);

        protected abstract JsonConverter CreateConverter(ServiceProvider serviceProvider, Type typeToConvert,
            JsonSerializerOptions options);
    }

    public abstract class BaseJsonConverter<T> : JsonConverter<T>
    {
        private readonly ServiceProvider _serviceProvider;

        protected BaseJsonConverter(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected TService GetService<TService>() => _serviceProvider.GetService<TService>();
    }

    public abstract class BaseFilterJsonConverter<T> : BaseJsonConverter<T>
    {
        protected BaseFilterJsonConverter(ServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected FilterType GetFilterType(ref Utf8JsonReader reader)
        {
            var enumValue = reader.TokenType == JsonTokenType.String
                ? reader.GetString()
                : $"{reader.GetInt32()}";
            return Enum.Parse<FilterType>(enumValue);
        }

        protected object GetValue(JsonElement elm)
        {
            object value = null;
            switch (elm.ValueKind)
            {
                case JsonValueKind.String:
                    value = elm.GetString();
                    break;

                case JsonValueKind.Number:
                    // Try to preserve the most appropriate CLR numeric type.
                    // Use the raw text to detect fractional/exponent parts and parse using invariant culture.
                    var raw = elm.GetRawText();
                    // If the number contains a decimal point or exponent, prefer decimal for precision, then double.
                    if (raw.IndexOf('.') >= 0 || raw.IndexOf('e') >= 0 || raw.IndexOf('E') >= 0)
                    {
                        if (decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec))
                        {
                            value = dec;
                        }
                        else if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var dbl))
                        {
                            value = dbl;
                        }
                        else
                        {
                            // fallback to the element's GetDouble (may throw if out of range)
                            try
                            {
                                value = elm.GetDouble();
                            }
                            catch
                            {
                                value = raw;
                            }
                        }
                    }
                    else
                    {
                        // Integer number: try int -> long -> decimal
                        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                        {
                            value = i;
                        }
                        else if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                        {
                            value = l;
                        }
                        else if (decimal.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                     out var dec2))
                        {
                            value = dec2;
                        }
                        else
                        {
                            // last resort, return the raw text
                            value = raw;
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    value = elm.GetBoolean();
                    break;

                case JsonValueKind.Array:
                    var values = new List<object>();
                    var enumerateArray = elm.EnumerateArray();
                    while (enumerateArray.MoveNext())
                    {
                        values.Add(GetValue(enumerateArray.Current));
                    }

                    value = values;
                    break;
            }

            return value;
        }
    }
}