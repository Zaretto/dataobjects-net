// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive;
using Xtensive.Parameters;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Storage.Linq.Expressions.Visitors;
using Xtensive.Storage.Linq.Materialization;
using Xtensive.Storage.Linq.Rewriters;
using Xtensive.Storage.Rse;
using Xtensive.Linq;
using Xtensive.Storage.Rse.Providers.Compilable;

namespace Xtensive.Storage.Linq.Expressions
{
  internal class ItemProjectorExpression : ExtendedExpression
  {
    public RecordQuery DataSource { get; set; }
    public TranslatorContext Context { get; private set; }
    public Expression Item { get; private set; }

    public bool IsPrimitive
    {
      get
      {
        var expression = Item.StripCasts();
        var extendedExpression = expression as ExtendedExpression;
        if (extendedExpression==null)
          return false;
        return extendedExpression.ExtendedType==ExtendedExpressionType.Column ||
          extendedExpression.ExtendedType==ExtendedExpressionType.Field;
      }
    }

    public List<int> GetColumns(ColumnExtractionModes columnExtractionModes)
    {
      return ColumnGatherer.GetColumns(Item, columnExtractionModes);
    }

    public ItemProjectorExpression Remap(RecordQuery dataSource, int offset)
    {
      if (offset==0)
        return new ItemProjectorExpression(Item, dataSource, Context);
      var item = GenericExpressionVisitor<IMappedExpression>.Process(Item, mapped => mapped.Remap(offset, new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, dataSource, Context);
    }

    public ItemProjectorExpression Remap(RecordQuery dataSource, int[] columnMap)
    {
      var item = GenericExpressionVisitor<IMappedExpression>.Process(Item, mapped => mapped.Remap(columnMap, new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, dataSource, Context);
    }

    public LambdaExpression ToLambda(TranslatorContext context)
    {
      return ExpressionMaterializer.MakeLambda(Item, context);
    }

    public MaterializationInfo Materialize(TranslatorContext context, IEnumerable<Parameter<Tuple>> tupleParameters)
    {
      return ExpressionMaterializer.MakeMaterialization(this, context, tupleParameters);
    }

    public ItemProjectorExpression BindOuterParameter(ParameterExpression parameter)
    {
      var item = GenericExpressionVisitor<IMappedExpression>.Process(Item, mapped => mapped.BindParameter(parameter, new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, DataSource, Context);
    }

    public ItemProjectorExpression RemoveOuterParameter()
    {
      var item = GenericExpressionVisitor<IMappedExpression>.Process(Item, mapped => mapped.RemoveOuterParameter(new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, DataSource, Context);
    }

    public ItemProjectorExpression RemoveOwner()
    {
      var item = OwnerRemover.RemoveOwner(Item);
      return new ItemProjectorExpression(item, DataSource, Context);
    }

    public ItemProjectorExpression SetDefaultIfEmpty()
    {
      var item = GenericExpressionVisitor<ParameterizedExpression>.Process(Item, mapped => {
        mapped.DefaultIfEmpty = true;
        return mapped;
      });
      return new ItemProjectorExpression(item, DataSource, Context);
    }

    public ItemProjectorExpression RewriteApplyParameter(ApplyParameter oldParameter, ApplyParameter newParameter)
    {
      var newDataSource = ApplyParameterRewriter.Rewrite(
        DataSource.Provider,
        oldParameter,
        newParameter)
        .Result;
      var newItemProjectorBody = ApplyParameterRewriter.Rewrite(
        Item,
        oldParameter,
        newParameter);
      return new ItemProjectorExpression(newItemProjectorBody, newDataSource, Context);
    }

    public ItemProjectorExpression EnsureEntityIsJoined()
    {
      var dataSource = DataSource;
      var newItem = new ExtendedExpressionReplacer(e => {
        if (e is EntityExpression) {
          var entityExpression = (EntityExpression) e;
          var typeInfo = entityExpression.PersistentType;
          if (typeInfo.Fields.All(fieldInfo => entityExpression.Fields.Any(entityField => entityField.Name==fieldInfo.Name)))
            return entityExpression;
          var joinedIndex = typeInfo.Indexes.PrimaryIndex;
          var joinedRs = IndexProvider.Get(joinedIndex).Result.Alias(Context.GetNextAlias());
          var keySegment = entityExpression.Key.Mapping;
          var keyPairs = keySegment.GetItems()
            .Select((leftIndex, rightIndex) => new Pair<int>(leftIndex, rightIndex))
            .ToArray();
          var offset = dataSource.Header.Length;
          dataSource = entityExpression.IsNullable
            ? dataSource.LeftJoin(joinedRs, JoinAlgorithm.Default, keyPairs)
            : dataSource.Join(joinedRs, JoinAlgorithm.Default, keyPairs);
          EntityExpression.Fill(entityExpression, offset);
          return entityExpression;
        }
        if (e is EntityFieldExpression) {
          var entityFieldExpression = (EntityFieldExpression)e;
          if (entityFieldExpression.Entity != null)
            return entityFieldExpression.Entity;
          var typeInfo = entityFieldExpression.PersistentType;
          var joinedIndex = typeInfo.Indexes.PrimaryIndex;
          var joinedRs = IndexProvider.Get(joinedIndex).Result.Alias(Context.GetNextAlias());
          var keySegment = entityFieldExpression.Mapping;
          var keyPairs = keySegment.GetItems()
            .Select((leftIndex, rightIndex) => new Pair<int>(leftIndex, rightIndex))
            .ToArray();
          var offset = dataSource.Header.Length;
          dataSource = entityFieldExpression.IsNullable 
            ? dataSource.LeftJoin(joinedRs, JoinAlgorithm.Default, keyPairs)
            : dataSource.Join(joinedRs, JoinAlgorithm.Default, keyPairs);
          entityFieldExpression.RegisterEntityExpression(offset);
          return entityFieldExpression.Entity;
        }
        if (e is FieldExpression) {
          var fe = (FieldExpression) e;
          if (fe.ExtendedType==ExtendedExpressionType.Field)
            return fe.RemoveOwner();
        }
        return null;
      })
        .Replace(Item);
      return new ItemProjectorExpression(newItem, dataSource, Context);
    }

    public override string ToString()
    {
      return string.Format("ItemProjectorExpression: IsPrimitive = {0} Item = {1}, DataSource = {2}", IsPrimitive, Item, DataSource);
    }


    // Constructors

    public ItemProjectorExpression(Expression expression, RecordQuery dataSource, TranslatorContext context)
      : base(ExtendedExpressionType.ItemProjector, expression.Type)
    {
      DataSource = dataSource;
      Context = context;
      var newApplyParameter = Context.GetApplyParameter(dataSource);
      var applyParameterReplacer = new ExtendedExpressionReplacer(ex =>
        ex is SubQueryExpression
          ? ((SubQueryExpression) ex).ReplaceApplyParameter(newApplyParameter)
          : null);
      Item = applyParameterReplacer.Replace(expression);
    }
  }
}