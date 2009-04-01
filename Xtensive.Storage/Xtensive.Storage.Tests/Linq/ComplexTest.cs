// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.02.25

using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Tests.ObjectModel;
using Xtensive.Storage.Tests.ObjectModel.NorthwindDO;

namespace Xtensive.Storage.Tests.Linq
{
  [TestFixture]
  public class ComplexTest : NorthwindDOModelTest
  {
    [Test]
    public void CorrelatedQueryTest()
    {
      var products = Query<Product>.All;
      var suppliers = Query<Supplier>.All;
      var result = from p in products
      select new {Product = p, Suppliers = suppliers.Where(s => s.Id==p.Supplier.Id).Select(s => s.CompanyName)};
      var list = result.ToList();
      Assert.Greater(list.Count, 0);
      foreach (var p in list)
        foreach (var companyName in p.Suppliers)
          Assert.IsNotNull(companyName);
    }

    [Test]
    public void CorrelatedOrderByTest()
    {
      var result =
        from c in Query<Customer>.All
        orderby Query<Order>.All.Where(o => o.Customer==c).Count()
        select c;
      var list = result.ToList();
      Assert.Greater(list.Count, 0);
    }

    [Test]
    public void NestedCorrelationTest()
    {
      var result =
        from c in Query<Customer>.All
        where Query<Order>.All.Where(o => o.Customer==c)
          .All(o => Query<Employee>.All.Where(e => o.Employee==e).Any(e => e.FirstName.StartsWith("A")))
        select c;
      var list = result.ToList();
      Assert.AreEqual(list.Count, 2);
    }
  }
}