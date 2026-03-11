using Microsoft.Extensions.DependencyInjection;
using PoweredSoft.DynamicQuery.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using PoweredSoft.DynamicQuery.System.Text.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
// using PoweredSoft.DynamicQuery.NewtonsoftJson;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Converters;
using Xunit;

namespace PoweredSoft.DynamicQuery.Test
{
    public class SerializationTests
    {
        [Fact]
        public void SimpleFilter()
        {
            var json = @"{""path"":""Title"",""value"":false,""type"":""In"",""and"":false}";

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddPoweredSoftDynamicQuery();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // var settings = new JsonSerializerSettings();
            //
            // settings.Converters.Add(new StringEnumConverter());
            // settings.Converters.Add(new DynamicQueryJsonConverter(serviceProvider));
            //
            // var data = JsonConvert.DeserializeObject<IQueryCriteria>(json, settings);

            var opts = new JsonSerializerOptions();
            // opts.PropertyNameCaseInsensitive = true;
            opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            // opts.WriteIndented = true;
            // opts.Converters.Add(new DynamicQuerySimpleFilterConverter(serviceProvider));
            opts.Converters.Add(new DynamicQueryFilterConverter(serviceProvider));

            var data = JsonSerializer.Deserialize<IFilter>(json, opts);
            Assert.NotNull(data);
            //Assert.Equal(data.Value, true);
        }


        [Fact]
        public void QueryCriteria()
        {
            var json = @"
                        {
                            ""page"":1,
                            ""pageSize"":20,
                            ""filters"":[
                                {""path"":""title"",""value"":false,""type"":""StartsWith"",""and"":true},
                                {""path"":""date"",""value"":""2020-04-01"",""type"":""Equal"",""and"":false},
                               
                                {""type"":""Composite"",""and"":false,""filters"":[
                                    {""path"":""date1"",""type"":""GreaterThan"",""value"":""2020-04-01""},
                                    {""path"":""date1"",""type"":""LessThan"",""value"":""2020-04-02""},
                                    {""type"":""Composite"",""and"":false,""filters"":[
                                        {""path"":""date2"",""type"":""GreaterThan"",""value"":""2020-05-01""},
                                        {""path"":""date2"",""type"":""LessThan"",""value"":""2020-05-02""}
                                    ]}
                                ]}
                            ],
                            ""sorts"":[{""path"":""title"",""ascending"":true}]
                        }
                      ";

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddPoweredSoftDynamicQuery();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            #region Newtonsoft.Json

            // var settings = new JsonSerializerSettings();
            //
            // settings.Converters.Add(new StringEnumConverter());
            // settings.Converters.Add(new DynamicQueryJsonConverter(serviceProvider));
            //
            // var data = JsonConvert.DeserializeObject<IQueryCriteria>(json, settings);

            #endregion

            #region Text.Json

            var opts = new JsonSerializerOptions();
            // opts.PropertyNameCaseInsensitive = true;
            opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.WriteIndented = true;

            opts.Converters.Add(new JsonStringEnumConverter());

            opts.Converters.Add(new DynamicQueryFilterConverter(serviceProvider));

            opts.Converters.Add(new DynamicQuerySortConverter(serviceProvider));
            opts.Converters.Add(new DynamicQueryJsonConverter(serviceProvider));

            #endregion

            var data = JsonSerializer.Deserialize<IQueryCriteria>(json, opts);
            Assert.NotNull(data);
            Assert.Equal(1, data.Page);
            Assert.Equal(20, data.PageSize);
            Assert.Equal(typeof(ICompositeFilter), data.Filters[2].GetType().GetInterface("ICompositeFilter"));
            Assert.NotEmpty(data.Filters);
            Assert.NotEmpty(data.Sorts);
        }

        [Fact]
        public void DeserializeNumberType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddPoweredSoftDynamicQuery();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var jsonFilters = @"
{
    ""filters"":
            [
                {""path"":""int"",""value"":2147483647,""type"":""Equal"",""and"":false},
                {""path"":""long"",""value"":122147483647,""type"":""Equal"",""and"":false},
                {""path"":""decimal"",""value"":23.54,""type"":""Equal"",""and"":false},
                {""path"":""bool_true"",""value"":true,""type"":""Equal"",""and"":false},
                {""path"":""bool_false"",""value"":false,""type"":""Equal"",""and"":false}
            ]
}
";
            
            var opts=new JsonSerializerOptions().AddPoweredSoftDynamicQueryTextJson(serviceProvider);

            var queryCriteria = JsonSerializer.Deserialize<IQueryCriteria>(jsonFilters, opts);
            var filters = queryCriteria.Filters;

            ISimpleFilter? GetFilter(int index)
            {
                var filter = filters[index];
                if (filter is ISimpleFilter simpleFilter) return simpleFilter;

                return null;
            }

            var index = 0;
            new List<Type> { typeof(int), typeof(long), typeof(decimal), typeof(bool) }.ForEach(type =>
            {
                Assert.True(GetFilter(index)?.Value?.GetType() == type);
                index += 1;
            });
        }
    }
}