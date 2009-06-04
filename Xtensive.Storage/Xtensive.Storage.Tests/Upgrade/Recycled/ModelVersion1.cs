// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.05.20

namespace Xtensive.Storage.Tests.Upgrade.Recycled.Model.Version1
{
  [HierarchyRoot]
  public class Customer : Entity
  {
    [Field, KeyField]
    public int Id { get; private set; }

    [Field(Length = 256)]
    public string Address { get; set; }

    [Field(Length = 24)]
    public string Phone { get; set; }

    [Field(Length = 30)]
    public string Name{ get; set;}
  }

  [HierarchyRoot]
  public class Employee : Entity
  {
    [Field, KeyField]
    public int Id { get; private set; }

    [Field(Length = 30)]
    public string CompanyName { get; set; }

    [Field(Length = 30)]
    public string Name { get; set; }
  }

  [HierarchyRoot]
  public class Order : Entity
  {
    [Field, KeyField]
    public int Id { get; private set; }

    [Field]
    public Employee Employee { get; set; }

    [Field]
    public Customer Customer { get; set; }

    [Field(Length = 128)]
    public string ProductName { get; set; }
    
  }
}