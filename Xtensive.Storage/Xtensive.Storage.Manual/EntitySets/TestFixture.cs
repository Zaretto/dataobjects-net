// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2009.06.29

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Core.Testing;
using Xtensive.Storage.Configuration;

namespace Xtensive.Storage.Manual.EntitySets
{
  [TestFixture]
  public class TestFixture
  {
    [HierarchyRoot]
    public class WebPage : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field(Length = 200)]
      public string Title { get; set; }

      [Field(Length = 200)]
      public string Url { get; set; }
    }

    [HierarchyRoot]
    public class User : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field]
      public Account Account { get; set; }

      [Field(Length = 200)]
      public string Name { get;  set; }

      [Field]
      public EntitySet<WebPage> FavoritePages { get; private set; }

      [Field]
      [Association(
        OnOwnerRemove = OnRemoveAction.Cascade,
        OnTargetRemove = OnRemoveAction.Clear)]
      public WebPage PersonalPage { get; set; }

      [Field]
      [Association(PairTo = "Author")]
      public EntitySet<BlogPost> BlogPosts { get; private set; }

      [Field]
      [Association(PairTo = "Friends")]
      public EntitySet<User> Friends { get; private set; }

      [Field]
      [Association(PairTo = "Participants")]
      public EntitySet<Meeting> Meetings { get; private set; }
    }

    [HierarchyRoot]
    public class Meeting : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field]
      public DateTime Date { get; set; }

      [Field(Length = 100)]
      public string Description { get; set; }

      [Field]
      public EntitySet<User> Participants { get; private set; }
    }

    [HierarchyRoot]
    public class BlogPost : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field(Length = 100)]
      public string Title { get; set; }

      [Field(Length = 1000)]
      public string Content { get; set; }

      [Field]
      public User Author { get; set;}
    }

    [HierarchyRoot]
    public class Account : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field, Association(PairTo = "Account", 
        OnOwnerRemove = OnRemoveAction.Deny, 
        OnTargetRemove = OnRemoveAction.Cascade)]
      public User User { get; set; }
    }

    [Test]
    public void OneToManyTest()
    {
      using (BuildDomain().OpenSession()) {
        using (Transaction.Open()) {

          User user = new User();

          var firstPost = new BlogPost {Title = "First post"};
          user.BlogPosts.Add(firstPost);

          Assert.AreEqual(user, firstPost.Author);

          var secondPost = new BlogPost {Title = "Second post"};
          secondPost.Author = user;

          Assert.IsTrue(user.BlogPosts.Contains(secondPost));

          user.BlogPosts.Remove(secondPost);

          Assert.IsNull(secondPost.Author);
        }
      }
    }

    [Test]
    public void OneToOneTest()
    {
      using (BuildDomain().OpenSession()) {
        using (Transaction.Open()) {

          User user = new User();
          Account account = new Account();
          user.Account = account;

          Assert.AreEqual(user, account.User);

          user.Account = null;
          Assert.IsNull(account.User);

          account.User = user;
          Assert.AreEqual(user, account.User);

          AssertEx.Throws<ReferentialIntegrityException>(account.Remove);

          user.Remove();
          Assert.IsTrue(account.IsRemoved);
        }
      }
    }

    [Test]
    public void EntitySetTest()
    {
      using (BuildDomain().OpenSession()) {
        using (Transaction.Open()) {
          User user = new User {Name = "Alex"};

          WebPage xtensive = new WebPage {Title = "Xtensive company", Url = "http://www.x-tensive.com"};
          WebPage dataobjects = new WebPage {Title = "DataObjects.Net", Url = "http://www.dataobjects.net"};

          user.FavoritePages.Add(xtensive);
          user.FavoritePages.Add(dataobjects);

          foreach (var page in user.FavoritePages)
            Console.WriteLine("{0} ( {1} )", page.Title, page.Url);

          Assert.AreEqual(2, user.FavoritePages.Count);

          Assert.AreEqual(1, SelectPages(user).Count());

          user.FavoritePages.Add(xtensive);
          Assert.AreEqual(2, user.FavoritePages.Count);
        }
      }
    }

    [Test]
    public IEnumerable<string> SelectPages(User user)
    {
      return
        from page in user.FavoritePages
        where page.Url.StartsWith("http://www.x-tensive.com")
        select page.Title;
    }

    [Test]
    public void MainTest()
    {
      BuildDomain();
    }

    private Domain BuildDomain()
    {
      var config = new DomainConfiguration("mssql2005://localhost/DO40-Tests");
      config.UpgradeMode = DomainUpgradeMode.Recreate;
      config.Types.Register(typeof(User).Assembly, typeof(User).Namespace);
      return Domain.Build(config);
    }
  }
}