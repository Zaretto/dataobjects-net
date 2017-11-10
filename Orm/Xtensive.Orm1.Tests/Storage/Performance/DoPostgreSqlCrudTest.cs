// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.04.30

using NUnit.Framework;
using Xtensive.Orm.Configuration;

namespace Xtensive.Orm.Tests.Storage.Performance
{
  [TestFixture]
  [Explicit]
  public class DoPostgreSqlCrudTest : DoCrudTest
  {
    protected override DomainConfiguration CreateConfiguration()
    {
      return DomainConfigurationFactory.CreateForCrudTest("pgsql84");
    }
  }
}