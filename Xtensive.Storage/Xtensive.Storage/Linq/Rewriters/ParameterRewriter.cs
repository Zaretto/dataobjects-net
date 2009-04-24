// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.01.31

using System;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Core.Linq;
using Xtensive.Core.Tuples;

namespace Xtensive.Storage.Linq.Rewriters
{
  internal class ParameterRewriter : QueryableVisitor
  {
    private readonly ParameterExpression tuple;
    private readonly ParameterExpression record;
    private bool recordIsUsed;

    public Pair<Expression,bool> Rewrite(Expression e)
    {
      recordIsUsed = false;
      var result = Visit(e);
      return new Pair<Expression, bool>(result, recordIsUsed);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
      if (p.Type == typeof(Tuple))
        return tuple;
      if (p.Type == typeof(Record)) {
        recordIsUsed = true;
        return record;
      }
      throw new NotSupportedException(String.Format("Parameter of type '{0}' isn't supported.", p.Type.Name));
    }


    // Constructors

    public ParameterRewriter(ParameterExpression tuple, ParameterExpression record)
    {
      this.tuple = tuple;
      this.record = record;
    }

    protected override Expression VisitQueryableMethod(MethodCallExpression mc, QueryableMethodKind methodKind)
    {
      return mc;
    }
  }
}