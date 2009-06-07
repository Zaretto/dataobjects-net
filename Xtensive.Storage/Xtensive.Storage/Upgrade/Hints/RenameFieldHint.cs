// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2009.04.29

using System;
using Xtensive.Core;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Storage.Upgrade
{
  /// <summary>
  /// Rename field hint.
  /// </summary>
  [Serializable]
  public sealed class RenameFieldHint : UpgradeHint
  {
    private const string ToStringFormat = "Rename field: {0} {1} -> {2}";

    /// <summary>
    /// Gets or sets the type of the target.
    /// </summary>
    public Type TargetType { get; private set; }

    /// <summary>
    /// Gets the old field name.
    /// </summary>    
    public string OldFieldName { get; private set; }

    /// <summary>
    /// Gets new field name.
    /// </summary>
    public string NewFieldName { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
      return string.Format(ToStringFormat, TargetType.FullName, OldFieldName, NewFieldName);
    }

    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="targetType">The current type.</param>
    /// <param name="oldFieldName">Old name of the field.</param>
    /// <param name="newFieldName">New name of the field.</param>
    public RenameFieldHint(Type targetType, string oldFieldName, string newFieldName)
    {
      ArgumentValidator.EnsureArgumentNotNull(targetType, "targetType");
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(oldFieldName, "oldFieldName");
      ArgumentValidator.EnsureArgumentNotNull(newFieldName, "newFieldName");

      TargetType = targetType;
      OldFieldName = oldFieldName;
      NewFieldName = newFieldName;
    }
  }
}