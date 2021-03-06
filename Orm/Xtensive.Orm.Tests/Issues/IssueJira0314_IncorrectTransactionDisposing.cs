﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Tests.Issues.IssueJira0314_IncorrectTransactionDisposingModel;

namespace Xtensive.Orm.Tests.Issues.IssueJira0314_IncorrectTransactionDisposingModel
{
  [HierarchyRoot]
  public sealed class Entity1 : Entity
  {
    [Field]
    [Key]
    public int Id { get; private set; }
  }

  [HierarchyRoot]
  public sealed class Entity2 : Entity
  {
    [Field]
    [Key]
    public int Id { get; private set; }
  }
}

namespace Xtensive.Orm.Tests.Issues
{
  public class IssueJira0314_IncorrectTransactionDisposing : AutoBuildTest
  {
    protected override void CheckRequirements()
    {
      Require.AllFeaturesNotSupported(ProviderFeatures.ExclusiveWriterConnection);
    }

    protected override DomainConfiguration BuildConfiguration()
    {
      DomainConfiguration config = base.BuildConfiguration();
      config.Sessions.Default.Options =
        SessionOptions.ServerProfile
        | SessionOptions.AutoActivation
        | SessionOptions.SuppressRollbackExceptions;
      config.Types.Register(typeof (Entity1).Assembly, typeof (Entity1).Namespace);
      return config;
    }

    protected override void PopulateData()
    {
      using (var session = Domain.OpenSession())
      using (var t = session.OpenTransaction()) {
        new Entity1();
        new Entity2();
      }
    }

    // ReSharper disable EmptyGeneralCatchClause

    [Test]
    public void Test()
    {
      var wait1 = new ManualResetEventSlim();
      var wait2 = new ManualResetEventSlim();
      Parallel.Invoke(
        () => {
          using (var session = Domain.OpenSession())
          using (session.OpenTransaction(TransactionOpenMode.New, IsolationLevel.Serializable))
          using (session.OpenTransaction(TransactionOpenMode.New, IsolationLevel.Serializable)) {
            try {
              Query.All<Entity1>().Lock(LockMode.Exclusive, LockBehavior.Wait).ToArray();
              wait1.Set();
              wait2.Wait();
              Query.All<Entity2>().Lock(LockMode.Exclusive, LockBehavior.Wait).ToArray();
            }
            catch {
              // catch deadlock here
              // we should complete normally
            }
          }
        },
        () => {
          using (var session = Domain.OpenSession())
          using (session.OpenTransaction(TransactionOpenMode.New, IsolationLevel.Serializable))
          using (session.OpenTransaction(TransactionOpenMode.New, IsolationLevel.Serializable)) {
            try {
              Query.All<Entity2>().Lock(LockMode.Exclusive, LockBehavior.Wait).ToArray();
              wait2.Set();
              wait1.Wait();
              Query.All<Entity1>().Lock(LockMode.Exclusive, LockBehavior.Wait).ToArray();
            }
            catch {
              // catch deadlock here
              // we should complete normally
            }
          }
        });
    }
  }
}
