// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.01.11

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Storage.Model
{
  [Serializable]
  public sealed class HierarchyInfo : Node
  {
    /// <summary>
    /// Gets the root of the hierarchy.
    /// </summary>
    public TypeInfo Root { get; private set; }

    /// <summary>
    /// Gets the <see cref="Model.InheritanceSchema"/> for this hierarchy.
    /// </summary>
    public InheritanceSchema InheritanceSchema { get; private set; }

    /// <summary>
    /// Gets the types of the current <see cref="HierarchyInfo"/>.
    /// </summary>
    public ReadOnlyList<TypeInfo> Types { get; private set; }

    /// <summary>
    /// Gets the <see cref="Key"/> for this instance.
    /// </summary>
    public KeyInfo Key { get; private set; }

    /// <summary>
    /// Gets the type discriminator.
    /// </summary>
    public TypeDiscriminatorMap TypeDiscriminatorMap { get; private set; }

    /// <inheritdoc/>
    public override void UpdateState(bool recursive)
    {
      base.UpdateState(recursive);
      Key.UpdateState(recursive);
      var list = new List<TypeInfo> {Root};
      list.AddRange(Root.GetDescendants(true));
      Types = new ReadOnlyList<TypeInfo>(list);
      if (Types.Count == 1)
        InheritanceSchema = InheritanceSchema.ConcreteTable;
      if (TypeDiscriminatorMap != null)
        TypeDiscriminatorMap.UpdateState();
    }

    /// <inheritdoc/>
    public override void Lock(bool recursive)
    {
      base.Lock(recursive);
      Key.Lock(recursive);
      if (TypeDiscriminatorMap != null)
        TypeDiscriminatorMap.Lock();
    }


    // Constructors

    /// <summary>
    /// 	<see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="root">The hierarchy root.</param>
    /// <param name="key">The key info.</param>
    /// <param name="inheritanceSchema">The inheritance schema.</param>
    /// <param name="typeDiscriminatorMap">The type discriminator map.</param>
    public HierarchyInfo(
      TypeInfo root, 
      KeyInfo key, 
      InheritanceSchema inheritanceSchema, 
      TypeDiscriminatorMap typeDiscriminatorMap)
    {
      Root = root;
      Key = key;
      InheritanceSchema = inheritanceSchema;
      TypeDiscriminatorMap = typeDiscriminatorMap;
    }
  }
}