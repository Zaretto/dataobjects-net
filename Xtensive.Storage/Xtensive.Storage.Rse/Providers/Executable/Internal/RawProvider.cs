// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.05.08

using System;
using System.Collections.Generic;
using Xtensive.Core.Tuples;
using Tuple = Xtensive.Core.Tuples.Tuple;

namespace Xtensive.Storage.Rse.Providers.Executable
{
  /// <summary>
  /// Enumerates specified array of <see cref="Tuple"/> instances.
  /// </summary>
  [Serializable]
  public sealed class RawProvider : ExecutableProvider<Compilable.RawProvider>
  {
    #region Cached properties

    private const string CachedSourceName = "CachedSource";

    private IEnumerable<Tuple> CachedSource {
      get { return GetCachedValue<IEnumerable<Tuple>>(EnumerationContext.Current, CachedSourceName); }
      set { SetCachedValue(EnumerationContext.Current, CachedSourceName, value); }
    }

    #endregion

    /// <inheritdoc/>
    protected internal override void OnBeforeEnumerate(EnumerationContext context)
    {
      base.OnBeforeEnumerate(context);
      CachedSource = Origin.CompiledSource.Invoke();
    }

    /// <inheritdoc/>
    protected internal override IEnumerable<Tuple> OnEnumerate(EnumerationContext context)
    {
      return CachedSource;
    }


    // Constructors

    public RawProvider(Compilable.RawProvider origin)
      : base(origin)
    {
      AddService<IListProvider>();
    }
  }
}