// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.05.19

namespace Xtensive.Storage.Providers.Memory
{
  /// <summary>
  /// <see cref="Domain"/>-level handler for memory index storage.
  /// </summary>
  public class DomainHandler : Index.DomainHandler
  {
    /// <inheritdoc/>
    protected override Index.IndexStorage CreateLocalStorage(string name)
    {
      return new MemoryIndexStorage(name);
    }
  }
}