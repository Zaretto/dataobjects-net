// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.03.31

namespace Xtensive.Storage.Indexing
{
  /// <summary>
  /// Enumerates possible command types.
  /// </summary>
  public enum CommandType
  {
    /// <summary>
    /// Query command.
    /// </summary>
    Query,
    /// <summary>
    /// Update command.
    /// </summary>
    Update,
    /// <summary>
    /// Set option command.
    /// </summary>
    SetOption,
  }
}