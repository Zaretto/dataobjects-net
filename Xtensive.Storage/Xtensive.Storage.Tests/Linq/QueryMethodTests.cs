// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2010.01.15

using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Linq;
using Xtensive.Storage.Tests.ObjectModel;
using Xtensive.Storage.Tests.ObjectModel.NorthwindDO;

namespace Xtensive.Storage.Tests.Linq
{
  public class QueryMethodTests : NorthwindDOModelTest
  {
    [Test]
    public void SingleParameterTest()
    {
      var key = Session.Query.All<Customer>().First().Key;
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.Single(key));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.Single(key));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    [ExpectedException(typeof(QueryTranslationException))]
    public void SingleSubqueryNonGenericTest()
    {
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.Single(Session.Query.All<Customer>().FirstOrDefault().Key));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.Single(Session.Query.All<Customer>().FirstOrDefault().Key));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    public void SingleSubqueryKeyTest()
    {
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.Single<Customer>(Session.Query.All<Customer>().FirstOrDefault().Key));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.Single<Customer>(Session.Query.All<Customer>().FirstOrDefault().Key));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    [ExpectedException(typeof(QueryTranslationException))]
    public void SingleSubqueryTupleTest()
    {
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.Single<Customer>(Session.Query.All<Customer>().FirstOrDefault().Id));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.Single<Customer>(Session.Query.All<Customer>().FirstOrDefault().Id));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    public void SingleOrDefaultParameterTest()
    {
      var key = Session.Query.All<Customer>().First().Key;
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.SingleOrDefault(key));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.SingleOrDefault(key));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    [ExpectedException(typeof(QueryTranslationException))]
    public void SingleOrDefaultSubqueryNonGenericTest()
    {
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.SingleOrDefault(Session.Query.All<Customer>().FirstOrDefault().Key));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.SingleOrDefault(Session.Query.All<Customer>().FirstOrDefault().Key));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    public void SingleOrDefaultSubqueryKeyTest()
    {
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.SingleOrDefault<Customer>(Session.Query.All<Customer>().FirstOrDefault().Key));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.SingleOrDefault<Customer>(Session.Query.All<Customer>().FirstOrDefault().Key));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    [ExpectedException(typeof(QueryTranslationException))]
    public void SingleOrDefaultSubqueryTupleTest()
    {
      var query = Session.Query.All<Customer>().Where(c => c==Session.Query.SingleOrDefault<Customer>(Session.Query.All<Customer>().FirstOrDefault().Id));
      var expected = Session.Query.All<Customer>().AsEnumerable().Where(c => c==Session.Query.SingleOrDefault<Customer>(Session.Query.All<Customer>().FirstOrDefault().Id));
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    public void Store1Test()
    {
      var localCustomers = Session.Query.All<Customer>().Take(10).ToList();
      var query = Session.Query.All<Customer>().Join(Session.Query.Store(localCustomers), customer => customer, localCustomer => localCustomer, (customer, localCustomer) => new {customer, localCustomer});
      var expected = Session.Query.All<Customer>().AsEnumerable().Join(Session.Query.Store(localCustomers), customer => customer, localCustomer => localCustomer, (customer, localCustomer) => new {customer, localCustomer});
      Assert.AreEqual(0, expected.Except(query).Count());
    }

    [Test]
    public void Store2Test()
    {
      var query = Session.Query.All<Customer>().Join(Session.Query.Store(Session.Query.All<Customer>().Take(10)), customer => customer, localCustomer => localCustomer, (customer, localCustomer) => new {customer, localCustomer});
      var expected = Session.Query.All<Customer>().AsEnumerable().Join(Session.Query.Store(Session.Query.All<Customer>().Take(10)), customer => customer, localCustomer => localCustomer, (customer, localCustomer) => new {customer, localCustomer});
      Assert.AreEqual(0, expected.Except(query).Count());
    }
  }
}