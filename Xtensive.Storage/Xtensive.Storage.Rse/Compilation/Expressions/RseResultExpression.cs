// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.12.11

using System;

namespace Xtensive.Storage.Rse.Compilation.Expressions
{
  [Serializable]
  public sealed class RseResultExpression : ExtendedExpression
  {
    public RecordSet RecordSet { get; private set; }
    public bool IsMultipleResults { get; private set; }
    public Func<RecordSet, object> Shaper { get; private set; }


    // Constructors

    public RseResultExpression(Type type, RecordSet recordSet, Func<RecordSet,object> shaper, bool isMultiple)
      : base(ExtendedExpressionType.RseResult, type)
    {
      RecordSet = recordSet;
      Shaper = shaper;
      IsMultipleResults = isMultiple;
    }
  }
}