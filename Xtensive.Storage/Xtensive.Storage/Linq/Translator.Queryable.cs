// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.02.27

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Linq;
using Xtensive.Core.Parameters;
using Xtensive.Core.Reflection;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Model;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Rse.Providers;
using Xtensive.Storage.Rse.Providers.Compilable;

namespace Xtensive.Storage.Linq
{
  internal sealed partial class Translator : MemberPathVisitor
  {
    private readonly TranslatorContext context;

    public ResultExpression Translate()
    {
      using (new ParameterScope()) {
        joinFinalEntity.Value = false;
        calculateExpressions.Value = false;
        return (ResultExpression) Visit(context.Query);
      }
    }

    public static Dictionary<string, Segment<int>> BuildFieldMapping(TypeInfo type, int offset)
    {
      var fieldMapping = new Dictionary<string, Segment<int>>();
      foreach (var field in type.Fields) {
        fieldMapping.Add(field.Name, new Segment<int>(offset + field.MappingInfo.Offset, field.MappingInfo.Length));
        if (field.IsEntity)
          fieldMapping.Add(field.Name + ".Key", new Segment<int>(offset + field.MappingInfo.Offset, field.MappingInfo.Length));
      }
      var keySegment = new Segment<int>(offset, type.Hierarchy.KeyInfo.Fields.Sum(pair => pair.Key.MappingInfo.Length));
      fieldMapping.Add("Key", keySegment);

      return fieldMapping;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
      if (c.Value==null)
        return c;
      var rootPoint = c.Value as IQueryable;
      if (rootPoint!=null)
        return ConstructQueryable(rootPoint);
      return base.VisitConstant(c);
    }

    protected override Expression VisitQueryableMethod(MethodCallExpression mc, QueryableMethodKind methodKind)
    {
      switch (methodKind) {
        case QueryableMethodKind.AsEnumerable:
          break;
        case QueryableMethodKind.AsQueryable:
          break;
        case QueryableMethodKind.ToArray:
          break;
        case QueryableMethodKind.ToList:
          break;
        case QueryableMethodKind.Cast:
          break;
        case QueryableMethodKind.OfType:
          break;
        case QueryableMethodKind.Aggregate:
          break;
        case QueryableMethodKind.ElementAt:
          break;
        case QueryableMethodKind.ElementAtOrDefault:
          break;
        case QueryableMethodKind.Last:
          break;
        case QueryableMethodKind.LastOrDefault:
          break;
        case QueryableMethodKind.Except:
          break;
        case QueryableMethodKind.Intersect:
          break;
        case QueryableMethodKind.Concat:
          break;
        case QueryableMethodKind.Union:
          break;
        case QueryableMethodKind.Reverse:
          break;
        case QueryableMethodKind.SequenceEqual:
          break;
        case QueryableMethodKind.DefaultIfEmpty:
          break;
        case QueryableMethodKind.SkipWhile:
          break;
        case QueryableMethodKind.TakeWhile:
          break;
        case QueryableMethodKind.All:
          if (mc.Arguments.Count==2)
            return VisitAll(mc.Arguments[0], mc.Arguments[1].StripQuotes(), context.IsRoot(mc));
          break;
        case QueryableMethodKind.Any:
          if (mc.Arguments.Count==1)
            return VisitAny(mc.Arguments[0], null, context.IsRoot(mc));
          if (mc.Arguments.Count==2)
            return VisitAny(mc.Arguments[0], mc.Arguments[1].StripQuotes(), context.IsRoot(mc));
          break;
        case QueryableMethodKind.Contains:
          if (mc.Arguments.Count==2)
            return VisitContains(mc.Arguments[0], mc.Arguments[1], context.IsRoot(mc));
          break;
        case QueryableMethodKind.Distinct:
          if (mc.Arguments.Count==1)
            return VisitDistinct(mc.Arguments[0]);
          break;
        case QueryableMethodKind.First:
        case QueryableMethodKind.FirstOrDefault:
        case QueryableMethodKind.Single:
        case QueryableMethodKind.SingleOrDefault:
          if (mc.Arguments.Count==1) {
            return VisitFirst(mc.Arguments[0], null, mc.Method, context.IsRoot(mc));
          }
          if (mc.Arguments.Count==2) {
            LambdaExpression predicate = (mc.Arguments[1].StripQuotes());
            return VisitFirst(mc.Arguments[0], predicate, mc.Method, context.IsRoot(mc));
          }
          break;
        case QueryableMethodKind.GroupBy:
          if (mc.Arguments.Count==2) {
            return VisitGroupBy(
              mc.Method,
              mc.Arguments[0],
              mc.Arguments[1].StripQuotes(),
              null,
              null
              );
          }
          if (mc.Arguments.Count==3) {
            LambdaExpression lambda1 = mc.Arguments[1].StripQuotes();
            LambdaExpression lambda2 = mc.Arguments[2].StripQuotes();
            if (lambda2.Parameters.Count==1) {
              // second lambda is element selector
              return VisitGroupBy(
                mc.Method,
                mc.Arguments[0],
                lambda1,
                lambda2,
                null);
            }
            if (lambda2.Parameters.Count==2) {
              // second lambda is result selector
              return VisitGroupBy(
                mc.Method,
                mc.Arguments[0],
                lambda1,
                null,
                lambda2);
            }
          }
          else if (mc.Arguments.Count==4) {
            return VisitGroupBy(
              mc.Method,
              mc.Arguments[0],
              mc.Arguments[1].StripQuotes(),
              mc.Arguments[2].StripQuotes(),
              mc.Arguments[3].StripQuotes()
              );
          }
          break;
        case QueryableMethodKind.GroupJoin:
          return VisitGroupJoin(
            mc.Type, mc.Arguments[0], mc.Arguments[1],
            mc.Arguments[2].StripQuotes(),
            mc.Arguments[3].StripQuotes(),
            mc.Arguments[4].StripQuotes());
        case QueryableMethodKind.Join:
          return VisitJoin(mc.Arguments[0], mc.Arguments[1],
            mc.Arguments[2].StripQuotes(),
            mc.Arguments[3].StripQuotes(),
            mc.Arguments[4].StripQuotes());
        case QueryableMethodKind.OrderBy:
          return VisitOrderBy(mc.Arguments[0], mc.Arguments[1].StripQuotes(), Direction.Positive);
        case QueryableMethodKind.OrderByDescending:
          return VisitOrderBy(mc.Arguments[0], mc.Arguments[1].StripQuotes(), Direction.Negative);
        case QueryableMethodKind.Select:
          return VisitSelect(mc.Arguments[0], mc.Arguments[1].StripQuotes());
        case QueryableMethodKind.SelectMany:
          if (mc.Arguments.Count==2)
            return VisitSelectMany(
              mc.Type, mc.Arguments[0],
              mc.Arguments[1].StripQuotes(),
              null);
          if (mc.Arguments.Count==3)
            return VisitSelectMany(
              mc.Type, mc.Arguments[0],
              mc.Arguments[1].StripQuotes(),
              mc.Arguments[2].StripQuotes());
          break;
        case QueryableMethodKind.LongCount:
        case QueryableMethodKind.Count:
        case QueryableMethodKind.Max:
        case QueryableMethodKind.Min:
        case QueryableMethodKind.Sum:
        case QueryableMethodKind.Average:
          if (mc.Arguments.Count==1)
            return VisitAggregate(mc.Arguments[0], mc.Method, null, context.IsRoot(mc));
          if (mc.Arguments.Count==2)
            return VisitAggregate(mc.Arguments[0], mc.Method, mc.Arguments[1].StripQuotes(), context.IsRoot(mc));
          break;
        case QueryableMethodKind.Skip:
          if (mc.Arguments.Count==2)
            return VisitSkip(mc.Arguments[0], mc.Arguments[1]);
          break;
        case QueryableMethodKind.Take:
          if (mc.Arguments.Count==2)
            return VisitTake(mc.Arguments[0], mc.Arguments[1]);
          break;
        case QueryableMethodKind.ThenBy:
          return VisitThenBy(mc.Arguments[0], mc.Arguments[1].StripQuotes(), Direction.Positive);
        case QueryableMethodKind.ThenByDescending:
          return VisitThenBy(mc.Arguments[0], mc.Arguments[1].StripQuotes(), Direction.Negative);
        case QueryableMethodKind.Where:
          return VisitWhere(mc.Arguments[0], mc.Arguments[1].StripQuotes());
        default:
          throw new ArgumentOutOfRangeException("methodKind");
      }
      throw new NotSupportedException();
    }

    private Expression VisitContains(Expression source, Expression match, bool isRoot)
    {
      var p = Expression.Parameter(match.Type, "p");
      var le = Expression.Lambda(Expression.Equal(p, match), p);

      if (isRoot)
        return VisitRootExists(source, le, false);

      if (source.IsQuery())
        return VisitExists(source, le, false);

      throw new NotImplementedException();
    }

    private Expression VisitAll(Expression source, LambdaExpression predicate, bool isRoot)
    {
      predicate = Expression.Lambda(Expression.Not(predicate.Body), predicate.Parameters[0]);

      if (isRoot)
        return VisitRootExists(source, predicate, true);

      if (source.IsQuery())
        return VisitExists(source, predicate, true);

      throw new NotImplementedException();
    }

    private Expression VisitAny(Expression source, LambdaExpression predicate, bool isRoot)
    {
      if (isRoot)
        return VisitRootExists(source, predicate, false);

      if (source.IsQuery())
        return VisitExists(source, predicate, false);

      throw new NotImplementedException();
    }

    private Expression VisitFirst(Expression source, LambdaExpression predicate, MethodInfo method, bool isRoot)
    {
      if (!isRoot)
        throw new NotImplementedException();
      ResultExpression result = predicate!=null
        ? (ResultExpression) VisitWhere(source, predicate)
        : (ResultExpression) Visit(source);
      RecordSet recordSet = null;
      switch (method.Name) {
        case WellKnown.Queryable.First:
        case WellKnown.Queryable.FirstOrDefault:
          recordSet = result.RecordSet.Take(1);
          break;
        case WellKnown.Queryable.Single:
        case WellKnown.Queryable.SingleOrDefault:
          recordSet = result.RecordSet.Take(2);
          break;
      }
      var enumerableType = typeof (Enumerable);
      MethodInfo enumerableMethod = enumerableType
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .First(m => m.Name==method.Name && m.GetParameters().Length==1)
        .MakeGenericMethod(method.ReturnType);
      var lambda = BuildProjector(result.ItemProjector, false);
      var projector = Expression.Lambda(
        Expression.Convert(Expression.Call(null, enumerableMethod, lambda.Body), typeof (object)),
        lambda.Parameters.ToArray());
      return new ResultExpression(method.ReturnType, recordSet, result.Mapping, (Expression<Func<RecordSet,object>>)projector, null);
    }

    private Expression VisitTake(Expression source, Expression take)
    {
      var projection = (ResultExpression) Visit(source);
      var parameter = context.ParameterExtractor.ExtractParameter<int>(take);
      var rs = projection.RecordSet.Take(parameter.Compile());
      return new ResultExpression(projection.Type, rs, projection.Mapping, projection.Projector, projection.ItemProjector);
    }

    private Expression VisitSkip(Expression source, Expression skip)
    {
      var projection = (ResultExpression) Visit(source);
      var parameter = context.ParameterExtractor.ExtractParameter<int>(skip);
      var rs = projection.RecordSet.Skip(parameter.Compile());
      return new ResultExpression(projection.Type, rs, projection.Mapping, projection.Projector, projection.ItemProjector);
    }

    private Expression VisitDistinct(Expression expression)
    {
      var result = (ResultExpression) Visit(expression);
      var rs = result.RecordSet.Distinct();
      return new ResultExpression(result.Type, rs, result.Mapping, result.Projector, result.ItemProjector);
    }

    private Expression VisitAggregate(Expression source, MethodInfo method, LambdaExpression argument, bool isRoot)
    {
      if (!isRoot)
        throw new NotImplementedException();
      AggregateType type = AggregateType.Count;
      Expression<Func<RecordSet, object>> shaper;
      ResultExpression result;
      int aggregateColumn = 0;
      if (method.Name==WellKnown.Queryable.Count || method.Name==WellKnown.Queryable.LongCount) {
        if (method.ReturnType==typeof (int))
          shaper = set => (int) (set.First().GetValue<long>(0));
        else
          shaper = set => (set.First().GetValue<long>(0));
        if (argument!=null)
          result = (ResultExpression) VisitWhere(source, argument);
        else
          result = (ResultExpression) Visit(source);
      }
      else {
        result = (ResultExpression) Visit(source);
        var columnList = new List<int>();
        if (argument==null) {
          if (result.Mapping.Segment.Length > 1 || result.ItemProjector.Body.Type!=result.RecordSet.Header.Columns[result.Mapping.Segment.Offset].Type)
            throw new NotSupportedException();
          columnList.Add(result.Mapping.Segment.Offset);
        }
        else {
          using (context.Bind(argument.Parameters[0], result))
          using (new ParameterScope()) {
            resultMapping.Value = new ResultMapping();
            Visit(argument);
            columnList = resultMapping.Value.GetColumns().ToList();
            result = context.GetBound(argument.Parameters[0]);
          }
        }

        if (columnList.Count!=1)
          throw new NotSupportedException();
        aggregateColumn = columnList[0];
        shaper = set => set.First().GetValueOrDefault(0);
        switch (method.Name) {
          case WellKnown.Queryable.Min:
            type = AggregateType.Min;
            break;
          case WellKnown.Queryable.Max:
            type = AggregateType.Max;
            break;
          case WellKnown.Queryable.Sum:
            type = AggregateType.Sum;
            break;
          case WellKnown.Queryable.Average:
            type = AggregateType.Avg;
            break;
        }
      }

      var recordSet = result.RecordSet.Aggregate(null, new AggregateColumnDescriptor(context.GetNextColumnAlias(), aggregateColumn, type));
      return new ResultExpression(result.Type, recordSet, null, shaper, null);
    }

    private Expression VisitGroupBy(MethodInfo method, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector)
    {
      var result = (ResultExpression) Visit(source);

      List<int> columnList;
      var newResultMapping = new ResultMapping();
      LambdaExpression remappedExpression;
      LambdaExpression originalCompiledKeyExpression;
      using (context.Bind(keySelector.Parameters[0], result))
      using (new ParameterScope()) {
        resultMapping.Value = new ResultMapping();
        originalCompiledKeyExpression = (LambdaExpression)Visit(keySelector);
        columnList = resultMapping.Value.GetColumns().ToList();
        // Remap 

        var tupleAccessProcessor = new TupleAccessProcessor();
        // var groupMapping = new List<int>(Enumerable.Repeat(-1, outer.RecordSet.Header.ColumnGroups.Count).Concat(Enumerable.Range(0, inner.RecordSet.Header.ColumnGroups.Count)));
        remappedExpression = (LambdaExpression) tupleAccessProcessor.ReplaceMappings(originalCompiledKeyExpression, columnList, null);

        result = context.GetBound(keySelector.Parameters[0]);

        if (!resultMapping.Value.MapsToPrimitive) {
          var keyMapping = new ResultMapping();
          newResultMapping.JoinedRelations.Add("Key", keyMapping);
          foreach (var field in resultMapping.Value.Fields) {
            var segment = new Segment<int>(columnList.IndexOf(field.Value.Offset), field.Value.Length);
            newResultMapping.RegisterFieldMapping("Key." + field.Key, segment);
            keyMapping.RegisterFieldMapping(field.Key, segment);
          }
        }
        else
          newResultMapping.RegisterFieldMapping("Key", new Segment<int>(columnList.IndexOf(resultMapping.Value.Segment.Offset), resultMapping.Value.Segment.Length));
      }

      var recordSet = result.RecordSet.Aggregate(columnList.ToArray());

      var resultGroupingType = method.ReturnType.GetGenericArguments()[0];
      Type[] groupingArguments = resultGroupingType.GetGenericArguments();
      var keyType = groupingArguments[0];
      var elementType = groupingArguments[1];
      var parameterGroupingType = typeof (Grouping<,>).MakeGenericType(keyType, elementType);
      var constructor = parameterGroupingType.GetConstructor(new[] {keyType, typeof (IEnumerable<>).MakeGenericType(elementType)});


      // record => new Grouping<TKey, TElement>(record.Key, source.Where(groupingItem => groupingItem.Key == record.Key))
      var pRecord = Expression.Parameter(typeof (Record), "record");
      var pTuple = Expression.Parameter(typeof (Tuple), "tuple");
      var parameterRewriter = new ParameterRewriter(pTuple, pRecord);
      var recordKeyExpression = parameterRewriter.Rewrite(remappedExpression.Body);

      Expression leftKeySelector = keySelector.Body;
      Expression rightKeySelector = recordKeyExpression.First;
      Expression groupingKeyResolver = rightKeySelector;
      if (rightKeySelector.Type == typeof(Key) && keySelector.Body.Type.IsSubclassOf(typeof(Entity))) {
        leftKeySelector = Expression.MakeMemberAccess(keySelector.Body, WellKnownMethods.IEntityKey);
        var keyResolveMethod = WellKnownMethods.KeyResolveOfT.MakeGenericMethod(keySelector.Body.Type);
        groupingKeyResolver = Expression.Call(rightKeySelector, keyResolveMethod);
      }

      var predicateExpression = Expression.Lambda(Expression.Equal(leftKeySelector, rightKeySelector), keySelector.Parameters.ToArray());

      var callMehtod = WellKnownMethods.QueryableWhere.MakeGenericMethod(elementType);

      var queryExpression = Expression.Call(callMehtod, source, predicateExpression);
      var projectorBody = Expression.New(constructor, groupingKeyResolver, queryExpression);
      var itemProjector = Expression.Lambda(projectorBody, recordKeyExpression.Second
        ? new[] {pTuple, pRecord}
        : new[] {pTuple});

      var rs = Expression.Parameter(typeof (RecordSet), "rs");
      Expression<Func<RecordSet, object>> projector;
      if (itemProjector.Parameters.Count > 1) {
        var makeProjectionMethod = typeof (Translator)
          .GetMethod("MakeProjection", BindingFlags.NonPublic | BindingFlags.Static)
          .MakeGenericMethod(itemProjector.Body.Type);
        projector = Expression.Lambda<Func<RecordSet, object>>(
          Expression.Convert(
            Expression.Call(makeProjectionMethod, rs, itemProjector),
            typeof (object)),
          rs);
      }
      else {
        var makeProjectionMethod = WellKnownMethods.EnumerableSelect.MakeGenericMethod(typeof (Tuple), itemProjector.Body.Type);
        projector = Expression.Lambda<Func<RecordSet, object>>(Expression.Convert(Expression.Call(makeProjectionMethod, rs, itemProjector), typeof (object)), rs);
      }

      return new ResultExpression(method.ReturnType, recordSet, newResultMapping, projector, itemProjector); //      Expression result = null;
    }

    private Expression VisitOrderBy(Expression expression, LambdaExpression le, Direction direction)
    {
      using (context.Bind(le.Parameters[0], (ResultExpression) Visit(expression)))
      using (new ParameterScope()) {
        resultMapping.Value = new ResultMapping();
        calculateExpressions.Value = true;
        Visit(le);
        var orderItems = resultMapping.Value.GetColumns()
          .Select(ci => new KeyValuePair<int, Direction>(ci, direction));
        var dc = new DirectionCollection<int>(orderItems);
        var result = context.GetBound(le.Parameters[0]);
        var rs = result.RecordSet.OrderBy(dc);
        return new ResultExpression(result.Type, rs, result.Mapping, result.Projector, result.ItemProjector);
      }
    }

    private Expression VisitThenBy(Expression expression, LambdaExpression le, Direction direction)
    {
      using (context.Bind(le.Parameters[0], (ResultExpression) Visit(expression)))
      using (new ParameterScope()) {
        resultMapping.Value = new ResultMapping();
        calculateExpressions.Value = true;
        Visit(le);
        var orderItems = resultMapping.Value.GetColumns()
          .Select(ci => new KeyValuePair<int, Direction>(ci, direction));
        var result = context.GetBound(le.Parameters[0]);
        var dc = ((SortProvider) result.RecordSet.Provider).Order;
        foreach (var item in orderItems) {
          if (!dc.ContainsKey(item.Key))
            dc.Add(item);
        }
        return result;
      }
    }

    private Expression VisitJoin(Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
    {
      var outerParameter = outerKey.Parameters[0];
      var innerParameter = innerKey.Parameters[0];
      using (context.Bind(outerParameter, (ResultExpression) Visit(outerSource)))
      using (context.Bind(innerParameter, (ResultExpression) Visit(innerSource))) {
        var outerMapping = new ResultMapping();
        var innerMapping = new ResultMapping();
        using (new ParameterScope()) {
          resultMapping.Value = outerMapping;
          Visit(outerKey);
          resultMapping.Value = innerMapping;
          Visit(innerKey);
        }
        var keyPairs = outerMapping.GetColumns().ZipWith(innerMapping.GetColumns(), (o, i) => new Pair<int>(o, i)).ToArray();

        var outer = context.GetBound(outerParameter);
        var inner = context.GetBound(innerParameter);
        var recordSet = outer.RecordSet.Join(inner.RecordSet.Alias(context.GetNextAlias()), keyPairs);
        return CombineResultExpressions(outer, inner, recordSet, resultSelector);
      }
    }

    private Expression CombineResultExpressions(ResultExpression outer, ResultExpression inner,
      RecordSet recordSet, LambdaExpression resultSelector)
    {
      var outerLength = outer.RecordSet.Header.Length;
      var innerLength = inner.RecordSet.Header.Length;

      var tupleAccessProcessor = new TupleAccessProcessor();
      var tupleMapping = new List<int>(
        Enumerable.Repeat(-1, outerLength).Concat(Enumerable.Range(0, innerLength))
        );
      var groupMapping = new List<int>(
        Enumerable.Repeat(-1, outer.RecordSet.Header.ColumnGroups.Count)
          .Concat(Enumerable.Range(0, inner.RecordSet.Header.ColumnGroups.Count))
        );

      outer = new ResultExpression(outer.Type, recordSet, outer.Mapping, outer.Projector, outer.ItemProjector);
      var innerProjector = (Expression<Func<RecordSet, object>>) tupleAccessProcessor.ReplaceMappings(inner.Projector, tupleMapping, groupMapping);
      var innerItemProjector = (LambdaExpression) tupleAccessProcessor.ReplaceMappings(inner.ItemProjector, tupleMapping, groupMapping);
      inner = new ResultExpression(inner.Type, recordSet, inner.Mapping.ShiftOffset(outerLength), innerProjector, innerItemProjector);

      using (context.Bind(resultSelector.Parameters[0], outer))
      using (context.Bind(resultSelector.Parameters[1], inner)) {
        return BuildProjection(resultSelector);
      }
    }

    private Expression VisitGroupJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
    {
      throw new NotImplementedException();
    }

    private Expression VisitSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
    {
      using (context.Bind(collectionSelector.Parameters[0], (ResultExpression) Visit(source))) {
        var parameter = collectionSelector.Parameters[0];
        var outerResult = context.GetBound(parameter);
        bool isOuter = false;
        if (collectionSelector.Body.NodeType==ExpressionType.Call) {
          var call = (MethodCallExpression) collectionSelector.Body;
          isOuter = call.Method.IsGenericMethod
            && call.Method.GetGenericMethodDefinition()==WellKnownMethods.QueryableDefaultIfEmpty;
          if (isOuter)
            collectionSelector = Expression.Lambda(call.Arguments[0], parameter);
        }
        ResultExpression innerResult;
        Parameter<Tuple> applyParameter;
        context.SubqueryParameterBindings.Bind(collectionSelector.Parameters);
        try {
          innerResult = (ResultExpression) Visit(collectionSelector.Body);
          applyParameter = context.SubqueryParameterBindings.GetBound(parameter);
        }
        finally {
          context.SubqueryParameterBindings.Unbind(collectionSelector.Parameters);
        }
        var recordSet = outerResult.RecordSet.Apply(applyParameter,
          innerResult.RecordSet.Alias(context.GetNextAlias()),
          isOuter ? ApplyType.Outer : ApplyType.Cross);
        return CombineResultExpressions(outerResult, innerResult, recordSet, resultSelector);
      }
    }

    private Expression VisitSelect(Expression expression, LambdaExpression le)
    {
      using (context.Bind(le.Parameters[0], (ResultExpression) Visit(expression))) {
        return BuildProjection(le);
      }
    }

    private Expression BuildProjection(LambdaExpression le)
    {
      using (new ParameterScope()) {
        resultMapping.Value = new ResultMapping();
        joinFinalEntity.Value = true;
        calculateExpressions.Value = true;
        var itemProjector = (LambdaExpression) Visit(le);
        var projector = (Expression<Func<RecordSet, object>>)BuildProjector(itemProjector, true);
        var source = context.GetBound(le.Parameters[0]);
        return new ResultExpression(
          typeof (IQueryable<>).MakeGenericType(le.Body.Type),
          source.RecordSet,
          resultMapping.Value,
          projector,
          itemProjector);
      }
    }

    private LambdaExpression BuildProjector(LambdaExpression itemProjector, bool castToObject)
    {
      var rs = Expression.Parameter(typeof (RecordSet), "rs");
      LambdaExpression projector;
      MethodInfo method;
      if (itemProjector.Parameters.Count > 1) {
        method = typeof (Translator)
          .GetMethod("MakeProjection", BindingFlags.NonPublic | BindingFlags.Static)
          .MakeGenericMethod(itemProjector.Body.Type);
      }
      else
        method = WellKnownMethods.EnumerableSelect.MakeGenericMethod(typeof (Tuple), itemProjector.Body.Type);
      projector = Expression.Lambda(
          castToObject
            ? (Expression)Expression.Convert(
              Expression.Call(method, rs, itemProjector),
              typeof(object))
            : (Expression)Expression.Call(method, rs, itemProjector),
          rs);
      return projector;
    }

    private Expression VisitWhere(Expression expression, LambdaExpression le)
    {
      var parameter = le.Parameters[0];
      using (context.Bind(parameter, (ResultExpression) Visit(expression)))
      using (new ParameterScope()) {
        resultMapping.Value = new ResultMapping();
        var predicate = Visit(le);
        var source = context.GetBound(parameter);
        var recordSet = source.RecordSet.Filter((Expression<Func<Tuple, bool>>) predicate);
        return new ResultExpression(
          expression.Type,
          recordSet,
          source.Mapping,
          source.Projector,
          source.ItemProjector);
      }
    }

    private Expression VisitRootExists(Expression source, LambdaExpression predicate, bool notExists)
    {
      var elementType = TypeHelper.GetElementType(source.Type);
      source = Expression.Call(WellKnownMethods.QueryableTake.MakeGenericMethod(elementType), source, Expression.Constant(1));

      MethodInfo realCountMethod;
      ResultExpression result;

      if (predicate!=null) {
        realCountMethod = WellKnownMethods.QueryableCountWithPredicate.MakeGenericMethod(elementType);
        result = (ResultExpression) VisitAggregate(source, realCountMethod, null, true);
      }
      else {
        realCountMethod = WellKnownMethods.QueryableCount.MakeGenericMethod(elementType);
        result = (ResultExpression) VisitAggregate(source, realCountMethod, null, true);
      }

      Expression<Func<RecordSet, object>> shaper;

      if (notExists)
        shaper = rs => rs.First().GetValue<long>(0)==0;
      else
        shaper = rs => rs.First().GetValue<long>(0) > 0;

      return new ResultExpression(typeof (bool), result.RecordSet, null, shaper, null);
    }

    private Expression VisitExists(Expression source, LambdaExpression predicate, bool notExists)
    {
      var subquery = predicate==null
        ? (ResultExpression) Visit(source)
        : (ResultExpression) VisitWhere(source, predicate);
      var lambdaParameter = context.SubqueryParameterBindings.CurrentParameter;
      var applyParameter = context.SubqueryParameterBindings.GetBound(lambdaParameter);
      var oldResult = context.GetBound(lambdaParameter);
      var columnName = context.GetNextColumnAlias();
      int columnIndex = oldResult.RecordSet.Header.Length;
      var newMapping = new ResultMapping();
      newMapping.Replace(oldResult.Mapping);
      newMapping.RegisterFieldMapping(columnName, new Segment<int>(columnIndex, 1));
      var newRecordSet = oldResult.RecordSet
        .Apply(applyParameter, subquery.RecordSet.Existence(columnName), ApplyType.Cross);
      var newResult = new ResultExpression(
        oldResult.Type, newRecordSet, newMapping, oldResult.Projector, oldResult.ItemProjector);
      context.ReplaceBound(lambdaParameter, newResult);
      Expression filter = MakeTupleAccess(lambdaParameter, typeof (bool), Expression.Constant(columnIndex));
      if (notExists)
        filter = Expression.Not(filter);
      return filter;
    }

    // Constructor

    /// <exception cref="InvalidOperationException">There is no current <see cref="Session"/>.</exception>
    internal Translator(TranslatorContext context)
      : base(context.Model)
    {
      this.context = context;
    }
  }
}