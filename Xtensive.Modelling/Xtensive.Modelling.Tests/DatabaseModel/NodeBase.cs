// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.03.20

using System;
using System.Diagnostics;

namespace Xtensive.Modelling.Tests.DatabaseModel
{
  [Serializable]
  public abstract class NodeBase<TParent> : Node<TParent, Server>
    where TParent : Node
  {
    protected override void PerformCreate()
    {
      base.PerformCreate();
      Log.Info("Created: {0}", this);
    }

    protected override void PerformMove(Node newParent, string newName, int newIndex)
    {
      using (Log.InfoRegion("Moving: {0}", this)) {
        if (Parent!=newParent)
          Log.Info("new Parent={0}", newParent);
        if (Name!=newName)
          Log.Info("new Name={0}", newName);
        if (Index!=newIndex)
          Log.Info("new Index={0}", newIndex);
        base.PerformMove(newParent, newName, newIndex);
      }
    }

    protected override void PerformShift(int offset)
    {
      Log.Info("Shifting: {0}, from {1} to {2}", this, Index, Index + offset);
      base.PerformShift(offset);
    }

    protected override void PerformRemove()
    {
      base.PerformRemove();
      Log.Info("Removed: {0}", this);
    }


    protected NodeBase(TParent parent, string name, int index)
      : base(parent, name, index)
    {
    }

    protected NodeBase(TParent parent, string name)
      : base(parent, name)
    {
    }
  }
}