// Copyright (C) 2016-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2016.10.19

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Tests.Upgrade.HugeModelUpgrade.TwoPartsModel;

namespace Xtensive.Orm.Tests.Upgrade.HugeModelUpgrade
{
  /// <summary>
  /// The test takes unnormal count of databases and time.
  /// Run it on local machine only!
  /// </summary>
  [Explicit]
  public sealed class TwoDatabasesPerNodeTest : HugeModelUpgradeTestBase
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.DefaultDatabase = "DO-Tests";
      configuration.DefaultSchema = "dbo";

      var partOneType = typeof(TwoPartsModel.PartOne.TestEntityOne0);
      var partTwoType = typeof(TwoPartsModel.PartTwo.TestEntityTwo0);
      configuration.Types.Register(partOneType.Assembly, partOneType.Namespace);
      configuration.Types.Register(partTwoType.Assembly, partTwoType.Namespace);

      configuration.MappingRules
        .Map(partOneType.Assembly, partOneType.Namespace)
        .ToDatabase("DO-Tests");
      configuration.MappingRules
        .Map(partTwoType.Assembly, partTwoType.Namespace)
        .ToDatabase("DO-Tests-1");
      return configuration;
    }

    protected override void PopulateData(Domain domain)
    {
      var nodes = new[] {
        WellKnown.DefaultNodeId,
        "Node1", "Node2", "Node3", "Node4", "Node5",
      };

      foreach (var node in nodes) {
        var selectedNode = domain.StorageNodeManager.GetNode(node);
        using (var session = selectedNode.OpenSession())
        using (var transaction = session.OpenTransaction()) {
          var populator = new ModelPopulator();
          populator.Run();
          transaction.Complete();
        }
      }
    }

    protected override void CheckIfQueriesWork(Domain domain)
    {
      var nodes = new[] {
        WellKnown.DefaultNodeId,
        "Node1", "Node2", "Node3", "Node4", "Node5",
      };

      foreach (var node in nodes) {
        var selectedNode = domain.StorageNodeManager.GetNode(node);
        using (var session = selectedNode.OpenSession())
        using (var transaction = session.OpenTransaction()) {
          var checker = new ModelChecker();
          checker.Run(session);
        }
      }
    }

    protected override IEnumerable<NodeConfiguration> GetAdditionalNodeConfigurations(DomainUpgradeMode upgradeMode)
    {
      var databases = new[] {
        "DO-Tests-2", "DO-Tests-3",
        "DO-Tests-4", "DO-Tests-5",
        "DO-Tests-6", "DO-Tests-7",
        "DO-Tests-8", "DO-Tests-9",
        "DO-Tests-10", "DO-Tests-11",
      };

      for (int index = 0, nodeIndex=1 ; index < 10; index += 2, nodeIndex++) {
        var node = new NodeConfiguration("Node" + nodeIndex);
        node.UpgradeMode = upgradeMode;
        node.DatabaseMapping.Add("DO-Tests", databases[index]);
        node.DatabaseMapping.Add("DO-Tests-1", databases[index+1]);
        yield return node;
      }
    }
  }
}
