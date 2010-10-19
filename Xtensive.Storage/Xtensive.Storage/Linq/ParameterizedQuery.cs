// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.27

using System;
using System.Collections.Generic;
using Xtensive;
using Xtensive.Parameters;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Storage.Linq
{
  [Serializable]
  internal sealed class ParameterizedQuery<TResult> : TranslatedQuery<TResult>
  {
    public readonly Parameter QueryParameter;

    public ParameterizedQuery(TranslatedQuery<TResult> translatedQuery, Parameter parameter)
      : base(translatedQuery.DataSource, translatedQuery.Materializer)
    {
      QueryParameter = parameter;
    }
  }
}