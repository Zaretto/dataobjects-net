// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using NUnit.Framework;

namespace Xtensive.Orm.Tests.Sql.PostgreSql.v8_4
{
  [TestFixture]
  public class SqlDomTests : v8_3.SqlDomTests
  {
    protected override string Url { get { return TestUrl.PostgreSql84; } }
  }
}