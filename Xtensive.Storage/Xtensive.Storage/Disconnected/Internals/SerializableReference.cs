// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.11.16

using System;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Disconnected
{
  [Serializable]
  internal sealed class SerializableReference
  {
    public FieldInfoRef Field { get; set; }

    public string[] Items { get; set; }
    
    public string[] AddedItems { get; set; }

    public string[] RemovedItems { get; set; }
  }
}