// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2009.12.11

using System;
using NUnit.Framework;
using Xtensive;
using Xtensive.IoC;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Manual.Attributes
{
  #region Custom key generator

  // Custom int key generator
  [Service(typeof(CustomInt32KeyGenerator), Name = "Int32")]
  public class CustomInt32KeyGenerator : KeyGenerator<int>
  {
    private int counter;

    public override Tuple TryGenerateKey(bool temporaryKey)
    {
      return Tuple.Create(counter++);
    }
  }

  #endregion

  #region Model

  [Serializable]
  // All descendant entities will be placed in the same table.
  [HierarchyRoot(InheritanceSchema = InheritanceSchema.SingleTable)]
  // Index on field "ISBN" that includes additiona "Title" field along with keyfield.
  [Index("ISBN", IncludedFields = new[] {"Title"})]
  // "Book" keys will be generated by custom "CustomKeyGenerator" key generator. 
  // KeyGenerator will generate 1024 keys per iteration.
  [KeyGenerator(typeof (CustomInt32KeyGenerator), Name = "Int32")]
  public abstract class Book : Entity
  {
    // This field will be used for descendant entities type information.
    [Field, TypeDiscriminator]
    public bool BookType { get; private set; }

    [Key(Direction = Direction.Negative), Field]
    public int Id { get; private set; }

    [Field, Version]
    public int Version { get; private set; }

    [Field]
    public string ISBN { get; set; }

    // Field will be mapped to "BookTitle" table column.
    [Field, FieldMapping("BookTitle")]
    public int Title { get; set; }

    // Field associated to "Books" field int the "Author" entity
    [Association(PairTo = "Books")]
    [Field]
    public Author Author { get; set; }
  }

  [Serializable]
  // If "BookType" field contains "true" value, the entity is "SciFi"
  [TypeDiscriminatorValue(true)]
  // Index on field "SciFiDescription" that includes additional "SciFiDescriptionDate" field along with keyfield.
  [Index("SciFiDescription", IncludedFields = new[] {"SciFiDescriptionDate"})]
  public class SciFi : Book
  {
    [Field(LazyLoad = true, Length = 4000)]
    public string SciFiDescription { get; set; }

    [Field(LazyLoad = true)]
    public DateTime SciFiDescriptionDate { get; set; }
  }

  [Serializable]
  // If "BookType" field contains "false" value, the entity is "Horror"
  [TypeDiscriminatorValue(false)]
  public class Horror : Book
  {
  }

  [Serializable]
  // Entity "Author" will be mapped to "Persons" table.
  [TableMapping("Persons")]
  // Key will include "TypeId" field.
  [HierarchyRoot(IncludeTypeId = true)]
  public class Author : Entity
  {
    // Key field. The default key generator will be used 
    // because [HierarchyRoot] attribute does not specify custom key generator
    [Key]
    [Field]
    public int Id { get; private set; }

    [Field(LazyLoad = true)]
    public string Name { get; set; }

    // Then author removed, all its books will be removed too.
    [Field, Association(OnOwnerRemove = OnRemoveAction.Cascade)]
    public EntitySet<Book> Books { get; set; }
  }

  #endregion

  [TestFixture]
  public class AttributesTest
  {
    [Test]
    public void MainTest()
    {
      var config = new DomainConfiguration("sqlserver://localhost/DO40-Tests") {
        UpgradeMode = DomainUpgradeMode.Recreate
      };
      config.Types.Register(typeof (Author).Assembly, typeof (Author).Namespace);
      var domain = Domain.Build(config);

      using (var session = domain.OpenSession()) {
        using (var transactionScope = session.OpenTransaction()) {

          var author = new Author();
          var sciFi = new SciFi {Author = author};
          var horror = new Horror {Author = author};

          transactionScope.Complete();
        }
      }
    }
  }
}