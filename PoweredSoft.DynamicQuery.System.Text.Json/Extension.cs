using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace PoweredSoft.DynamicQuery.System.Text.Json
{
    public static class Extension
    {
        public static JsonSerializerOptions AddPoweredSoftDynamicQueryTextJson(this JsonSerializerOptions opts,
            ServiceProvider serviceProvider, bool enableStringEnumConverter = true)
        {
            if (enableStringEnumConverter)
                opts.Converters.Add(new JsonStringEnumConverter());

            opts.PropertyNameCaseInsensitive = true;
            opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.WriteIndented = true;

            opts.Converters.Add(new DynamicQueryFilterConverter(serviceProvider));

            opts.Converters.Add(new DynamicQuerySortConverter(serviceProvider));
            opts.Converters.Add(new DynamicQueryJsonConverter(serviceProvider));
            return opts;
        }

    }
}