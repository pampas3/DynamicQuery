using Microsoft.Extensions.DependencyInjection;
using PoweredSoft.Data;
using PoweredSoft.DynamicQuery.System.Text.Json;


namespace PoweredSoft.DynamicQuery.AspNetCore
{
    public static class MvcBuilderExtensions
    {
        private static IMvcBuilder AddPoweredSoftDynamicQuery(this IMvcBuilder builder)
        {
            builder.Services.AddPoweredSoftDataServices();
            builder.Services.AddPoweredSoftDynamicQuery();
            return builder;
        }

        public static IMvcBuilder AddPoweredSoftJsonNetDynamicQuery(this IMvcBuilder mvcBuilder,
            bool enableStringEnumConverter = true)
        {
            mvcBuilder.AddPoweredSoftDynamicQuery();
            var serviceProvider = mvcBuilder.Services.BuildServiceProvider();
            mvcBuilder.AddJsonOptions(cfg =>
            {
                cfg.JsonSerializerOptions.AddPoweredSoftDynamicQueryTextJson(serviceProvider,
                    enableStringEnumConverter);
            });
            return mvcBuilder;
        }
    }
}