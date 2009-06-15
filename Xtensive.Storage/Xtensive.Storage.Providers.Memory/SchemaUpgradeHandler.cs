// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.04.09

using System;
using Xtensive.Modelling.Actions;
using Xtensive.Storage.Building;
using Xtensive.Storage.Indexing.Model;
using Xtensive.Storage.Providers.Index;

namespace Xtensive.Storage.Providers.Memory
{
  /// <summary>
  /// Upgrades storage schema.
  /// </summary>
  [Serializable]
  public class SchemaUpgradeHandler : Index.SchemaUpgradeHandler
  {
    /// <summary>
    /// Gets the storage view.
    /// </summary>
    protected IndexStorageView StorageView {
      get {
        var session = BuildingContext.Demand().SystemSessionHandler;
        var view = ((SessionHandler) session).StorageView;
        return view as IndexStorageView;
      }
    }
    
    /// <inheritdoc/>
    public override StorageInfo GetExtractedSchema()
    {
      return (StorageInfo) StorageView.Model.Clone(null, StorageInfo.DefaultName);
    }

    /// <inheritdoc/>
    public override void UpgradeSchema(ActionSequence upgradeActions, StorageInfo sourceSchema, StorageInfo targetSchema)
    {
      StorageView.ClearSchema();
      StorageView.CreateNewSchema(targetSchema);
    }
  }
}