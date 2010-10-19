// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2010.10.01

using System;
using NUnit.Framework;
using Xtensive;
using Xtensive.Storage.Configuration;
using System.Linq;

namespace Xtensive.Storage.Tests.Storage.RefTest
{
  [HierarchyRoot]
  public class Author : Entity
  {
    [Field, Key]
    public int Id { get; private set; }
  }

  [TestFixture]
  public class RefTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.Types.Register(typeof (Author).Assembly, typeof (Author).Namespace);
      return config;
    }

    [Test]
    public void CombinedTest()
    {
      Key authorKey;
      Ref<Author> authorRef;

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        var author = new Author();
        authorKey = author.Key;
        authorRef = (Ref<Author>) author;
        tx.Complete();
      }

      authorRef = Cloner.Default.Clone(authorRef);

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        Assert.AreEqual(authorKey, authorRef.Key);
        var author = authorRef.Value;
        Assert.IsNotNull(author);
        Assert.AreEqual(authorRef.Key, author.Key);
        tx.Complete();
      }

    }
  }
}