// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2009.06.17

using Xtensive.Storage.Configuration;

namespace Xtensive.Storage.Manual
{
  public class DomainAndSessionSample
  {
    [HierarchyRoot]
    public class Person : Entity
    {
      [Field, Key]
      public int Id { get; set; }

      [Field]
      public string Name { get; set; }
    }

    #region Domain sample
    public void Main()
    {
      var configuration = new DomainConfiguration("sqlserver://localhost/MyDatabase");
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      configuration.Types.Register(typeof (Person));

      var domain = Domain.Build(configuration);

      using (Session.Open(domain)) {
        using (var transactionScope = Transaction.Open()) {

          var person = new Person();
          person.Name = "Barack Obama";

          transactionScope.Complete();
        }
      }
    }
    #endregion

    #region Session sample
    public void SessionSample(Domain domain)
    {
      using (Session.Open(domain)) {
        using (var transactionScope = Transaction.Open()) {

          var person = new Person();
          person.Name = "Barack Obama";

          transactionScope.Complete();
        }
      }
    }
    #endregion
  }
}