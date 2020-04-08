﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoweredSoft.DynamicLinq;
using PoweredSoft.DynamicLinq.Fluent;
using PoweredSoft.DynamicQuery.Core;

namespace PoweredSoft.DynamicQuery
{
    public class QueryHandler : QueryHandlerBase, IQueryHandler
    {
        public QueryHandler(IEnumerable<IQueryInterceptorProvider> queryableInterceptorProviders) : base(queryableInterceptorProviders)
        {
        }

        protected virtual IQueryExecutionResult<TSource> FinalExecute<TSource>()
        {
            CommonBeforeExecute<TSource>();
            return ExecuteNoGrouping<TSource>();
            // return HasGrouping ? ExecuteGrouping<TSource, TRecord>() : ExecuteNoGrouping<TSource, TRecord>();
        }

        /*protected virtual IQueryExecutionResult<TRecord> ExecuteGrouping<TSource, TRecord>()
        {
            var result = new QueryExecutionGroupResult<TRecord>();

            // preserve queryable.
            var queryableAfterFilters = CurrentQueryable;

            result.TotalRecords = queryableAfterFilters.LongCount();
            CalculatePageCount(result);

            // intercept groups in advance to avoid doing it more than once :)
            var finalGroups = Criteria.Groups.Select(g => InterceptGroup<TSource>(g)).ToList();

            // get the aggregates.
            var aggregateResults = FetchAggregates<TSource>(finalGroups);

            // sorting.
            finalGroups.ReversedForEach(fg => Criteria.Sorts.Insert(0, new Sort(fg.Path, fg.Ascending)));

            // apply sorting and paging.
            ApplySorting<TSource>();
            ApplyPaging<TSource>();

            if (Options.GroupByInMemory)
                CurrentQueryable = CurrentQueryable.ToObjectList().Cast<TSource>().AsQueryable(); 

            CurrentQueryable = CurrentQueryable.GroupBy(QueryableUnderlyingType, gb =>
            {
                gb.NullChecking(Options.GroupByInMemory ? Options.GroupByInMemoryNullCheck : false);
                finalGroups.ForEach((fg, index) => gb.Path(fg.Path, $"Key_{index}"));
            });

            CurrentQueryable = CurrentQueryable.Select(sb =>
            {
                sb.NullChecking(Options.GroupByInMemory ? Options.GroupByInMemoryNullCheck : false);
                finalGroups.ForEach((fg, index) => sb.Key($"Key_{index}", $"Key_{index}"));
                sb.ToList("Records");
            });


            // loop through the grouped records.
            var groupRecords = CurrentQueryable.ToDynamicClassList();

            // now join them into logical collections
            var lastLists = new List<(List<TSource> source, IGroupQueryResult<TRecord> group)>();
            result.Groups = RecursiveRegroup<TSource, TRecord>(groupRecords, aggregateResults, Criteria.Groups.First(), lastLists);

            // intercept grouped by.
            QueryInterceptToGrouped<TSource, TRecord>(lastLists).Wait();

            result.Aggregates = CalculateTotalAggregate<TSource>(queryableAfterFilters);
            return result;
        }*/
        protected virtual List<IAggregateResult> CalculateTotalAggregate<TSource>(IQueryable queryableAfterFilters)
        {
            if (!Criteria.Aggregates.Any())
                return null;

            IQueryable selectExpression = CreateTotalAggregateSelectExpression<TSource>(queryableAfterFilters);
            var aggregateResult = selectExpression.ToDynamicClassList().FirstOrDefault();
            return MaterializeCalculateTotalAggregateResult(aggregateResult);
        }
        
        protected virtual List<List<DynamicClass>> FetchAggregates<TSource>(List<IGroup> finalGroups)
        {
            if (!Criteria.Aggregates.Any())
                return null;
            
            var previousGroups = new List<IGroup>();
            var ret = finalGroups.Select(fg =>
            {
                IQueryable selectExpression = CreateFetchAggregateSelectExpression<TSource>(fg, previousGroups);
                var aggregateResult = selectExpression.ToDynamicClassList();
                previousGroups.Add(fg);
                return aggregateResult;
            }).ToList();
            return ret;
        }

        protected virtual IQueryExecutionResult<TSource> ExecuteNoGrouping<TSource>()
        {
            var result = new QueryExecutionResult<TSource>();

            // after filter queryable
            var afterFilterQueryable = CurrentQueryable;

            // total records.
            result.TotalRecords = afterFilterQueryable.LongCount();
            CalculatePageCount(result);

            // sorts and paging.
            ApplySorting<TSource>();
            ApplyPaging<TSource>();

            // data.
            // var entities = ((IQueryable<TSource>)CurrentQueryable).ToList();
            // var records = InterceptConvertTo<TSource, TRecord>(entities).Result;
            result.Data = (IQueryable<TSource>) CurrentQueryable;

            // aggregates.
            result.Aggregates = CalculateTotalAggregate<TSource>(afterFilterQueryable);

            return result;
        }

        public IQueryExecutionResult<TSource> Execute<TSource>(IQueryable<TSource> queryable, IQueryCriteria criteria)
        {
            Reset(queryable, criteria, new QueryExecutionOptions());
            return FinalExecute<TSource>();
        }

        /* public IQueryExecutionResult<TRecord> Execute<TSource, TRecord>(IQueryable<TSource> queryable, IQueryCriteria criteria)
         {
             Reset<TSource, TRecord>(queryable, criteria, new QueryExecutionOptions());
             return FinalExecute<TSource, TRecord>();
         }
 
         public IQueryExecutionResult<TSource> Execute<TSource>(IQueryable<TSource> queryable, IQueryCriteria criteria, IQueryExecutionOptions options)
         {
             Reset<TSource, TSource>(queryable, criteria, options);
             return FinalExecute<TSource, TSource>();
         }
 
         public IQueryExecutionResult<TRecord> Execute<TSource, TRecord>(IQueryable<TSource> queryable, IQueryCriteria criteria, IQueryExecutionOptions options)
         {
             Reset<TSource, TRecord>(queryable, criteria, options);
             return FinalExecute<TSource, TRecord>();
         }*/
    }
}
