// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.05.26

using NUnit.Framework;

namespace Xtensive.Storage.Tests.Upgrade
{
  [TestFixture]
  [Explicit("Requires PostgreSQL")]
  public sealed class ConvertPgSqlModelTest : ConvertDomainModelTest
  {
    [SetUp]
    public override void SetUp()
    {
      Schema = BuildDomain("pgsql").ExtractedSchema;
    }

    [Test]
    public override void IncludedColumnsTest()
    {
      Assert.AreEqual(0,
        Schema.Tables["A"].SecondaryIndexes[0].IncludedColumns.Count);
    }
  }
}