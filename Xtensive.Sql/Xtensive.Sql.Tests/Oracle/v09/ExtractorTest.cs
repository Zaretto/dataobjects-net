// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.07.29

using NUnit.Framework;

namespace Xtensive.Sql.Tests.Oracle.v09
{
  [TestFixture, Explicit]
  public class ExtractorTest : Oracle.ExtractorTest
  {
    protected override string Url { get { return TestUrl.Oracle9; } }
  }
}