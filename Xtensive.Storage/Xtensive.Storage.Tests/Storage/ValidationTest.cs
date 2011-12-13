// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2008.08.31

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Core.Testing;
using Xtensive.Integrity.Aspects.Constraints;
using Xtensive.Integrity.Validation;
using Xtensive.Storage.Configuration;
using AggregateException = Xtensive.Core.AggregateException;

namespace Xtensive.Storage.Tests.Storage.Validation
{ 
  public class ValidationTest : AutoBuildTest
  {
    [ThreadStatic]
    private static int validationCallsCount;

    [Serializable]
    [HierarchyRoot]
    public class Referrer : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field, NotNullConstraint]
      public Reference Reference { get; set; }

      [Field, NotNullOrEmptyConstraint]
      public string Title { get; set; }

      public Referrer(Reference reference, string title)
      {
        Reference = reference;
        Title = title;
      }

      public Referrer()
      {
      }
    }

    [Serializable]
    [HierarchyRoot]
    public class Reference : Entity
    {
      [Key, Field]
      public int Id { get; private set; }

      [Field, NotNullOrEmptyConstraint]
      public string Title { get; set; }

      public Reference(string title)
      {
        Title = title;
      }

      public Reference()
      {
      }
    }

    [Serializable]
    [HierarchyRoot]
    public class Mouse : Entity
    {
      [Field, Key]
      public int ID { get; private set; }
        
      [Field]
      public int ButtonCount { get; set; }

      [Field]
      public int ScrollingCount { get; set; }

      [Field]
      public Led Led { get; set; }

      protected override void OnValidate()
      {
        validationCallsCount++;

        base.OnValidate();
        
        if (ButtonCount<1)
          throw new InvalidOperationException("Button count can't be less than one.");

        if (ScrollingCount > ButtonCount)
          throw new InvalidOperationException("Scrolling count can't be greater then button count.");

        if (Led.Brightness > 10)
          throw new InvalidOperationException("Led in the mouse is too bright.");
      }
    }

    [Serializable]
    public class Led : Structure
    {
      [Field]
      public double Brightness { get; set;}

      [Field]
      public double Precision { get; set;}

      protected override void OnValidate()
      {
        base.OnValidate();

        if (Brightness <= 0)
          throw new InvalidOperationException("Led brightness should be greater then 0.");

        if (Precision <= 0)
          throw new InvalidOperationException("Led precision should be greater then 0.");

        if (Precision > Brightness * 10)
          throw new InvalidOperationException("Invalid precision-brightness ratio.");
      }
    }

    [HierarchyRoot]
    public class ValidationTarget : Entity
    {
      [Field, Key]
      public int Id { get; private set; }

      [Field, NotNullOrEmptyConstraint(Mode = ConstrainMode.OnSetValue)]
      public string Name { get; set; }

      [Field, NotNullOrEmptyConstraint(Mode = ConstrainMode.OnSetValue)]
      public string Description { get; set; }
    }

    protected override DomainConfiguration BuildConfiguration()
    {
      DomainConfiguration config = base.BuildConfiguration();
      config.Types.Register(Assembly.GetExecutingAssembly(), typeof(Mouse).Namespace);
      return config;
    }

    [Test]
    public void OnSetValueModeTest()
    {
      using (var session = Session.Open(Domain)) {
        using (var t = Transaction.Open(session)) {

          var target = new ValidationTarget();
          target.Name = "name";
        }
      }
    }

    [Test]
    public void ValidationCallsCountTest()
    { 
      int mouseId;
      validationCallsCount = 0;
      using (Session.Open(Domain)) {
        using (var transactionScope = Transaction.Open()) {
          using (var region = Xtensive.Storage.Validation.Disable()) {
            var mouse = new Mouse {
              ButtonCount = 2,
              ScrollingCount = 1
            };
            mouse.Led.Brightness = 1.5;
            mouse.Led.Precision = 1.5;
            mouseId = mouse.ID;

            region.Complete();
          }
          transactionScope.Complete();
        }
      }
      Assert.AreEqual(1, validationCallsCount);

      validationCallsCount = 0;
      using (Session.Open(Domain)) {
        using (Transaction.Open()) {
          var mouse = Query.All<Mouse>().Where(m => m.ID==mouseId).First();
        }
      }
      // No validation calls on meterialization
      Assert.AreEqual(0, validationCallsCount); 
    }

    [Test]
    public void ValidateAllTest()
    {
      validationCallsCount = 0;

      using (Session.Open(Domain)) {
        Mouse mouse;

        using (var tx = Transaction.Open()) {
          using (var region = Xtensive.Storage.Validation.Disable()) {
            mouse = new Mouse {
              ButtonCount = 2,
              ScrollingCount = 1,
              Led = new Led {Brightness = 7.3, Precision = 33}
            };
            mouse.Led.Brightness = 4.3;

            Xtensive.Storage.Validation.Enforce();
            Assert.AreEqual(1, validationCallsCount);

            mouse.Led.Brightness = 2.3;

            // Fails here!
            AssertEx.Throws<AggregateException>(Xtensive.Storage.Validation.Enforce);

            region.Complete();
            AssertEx.Throws<AggregateException>(region.Dispose);
          } // Second .Dispose should do nothing!

          tx.Complete();
          AssertEx.Throws<InvalidOperationException>(tx.Dispose);
        } // Second .Dispose should do nothing!
      }
    }
    
    [Test]
    public void EntityValidation()
    {
      using (Session.Open(Domain)) {
        using (Transaction.Open()) {

          // Created and modified invalid object. (ScrollingCount > ButtonCount)
          AssertEx.Throws<AggregateException>(
            () => {
              using (var region = Xtensive.Storage.Validation.Disable()) {
                new Mouse {ButtonCount = 2, ScrollingCount = 3, Led = new Led {Brightness = 1, Precision = 1}};
                region.Complete();
              }
            });
        }
        using (Transaction.Open()) {

          // Created, but not modified invalid object.
          AssertEx.Throws<AggregateException>(() =>
            new Mouse());
        }
        using (Transaction.Open()) {

          // Invalid modification of existing object.
          AssertEx.Throws<AggregateException>(
            () => {
              Mouse m;
              using (var region = Xtensive.Storage.Validation.Disable()) {
                m = new Mouse {ButtonCount = 1, ScrollingCount = 1, Led = new Led {Brightness = 1, Precision = 1}};
                region.Complete();
              }
              m.ScrollingCount = 2;
            });
        }
        using (Transaction.Open()) {

          Mouse mouse;
          // Valid object - ok.
          using (var region = Xtensive.Storage.Validation.Disable()) {
            mouse = new Mouse {ButtonCount = 5, ScrollingCount = 3};
            mouse.Led.Precision = 1;
            mouse.Led.Brightness = 2;
            region.Complete();
          }

          // Valid modification with invalid intermediate state - ok.
          using (var region = Xtensive.Storage.Validation.Disable()) {
            mouse.ButtonCount = 2;
            mouse.ScrollingCount = 1;
            region.Complete();
          }

          // Invalid object is removed - ok.
          using (var region = Xtensive.Storage.Validation.Disable()) {
            mouse.ScrollingCount = 3;
            mouse.Remove();
            region.Complete();
          }
        }
      }
    }

    [Test]
    public void StructureValidation()
    {
      using (Session.Open(Domain)) {
        Mouse mouse;

        using (var transactionScope = Transaction.Open()) {

          // Valid mouse is created.
          using (var region = Xtensive.Storage.Validation.Disable()) {
            mouse = new Mouse {ButtonCount = 2, ScrollingCount = 1};
            mouse.Led = new Led {Brightness = 7.3, Precision = 33};
            region.Complete();
          }
          transactionScope.Complete();
        }

        // Structure become invalid.
        using (var transactionScope = Transaction.Open()) {
          AssertEx.Throws<AggregateException>(
            delegate {
              mouse.Led.Brightness = 2.3;
              transactionScope.Complete();
            });
        }
          
        // Changed structure make entity invalid.
        using (var transactionScope = Transaction.Open()) {
          AssertEx.Throws<AggregateException>(
            delegate {
              mouse.Led.Brightness = 11;
              transactionScope.Complete();
            });
        }
      }
    }

    [Test]
    public void TransactionsValidation()
    {
      using (Session.Open(Domain)) {

        // Inconsistent region can not be opened without transaction.
        AssertEx.ThrowsInvalidOperationException(() =>
          Xtensive.Storage.Validation.Disable());

        // Transaction can not be committed while validation context is in inconsistent state.
          AssertEx.ThrowsInvalidOperationException(() => {
          using (var t = Transaction.Open()) {
            Xtensive.Storage.Validation.Disable();
            t.Complete();
          }
        });

        using (var transactionScope = Transaction.Open()) {
          try {
            using (var region = Xtensive.Storage.Validation.Disable()) {
              var mouse = new Mouse();
              throw new Exception("Test");
              // region.Complete();
            }
          }
          catch (Exception exception) {
            Assert.AreEqual("Test", exception.Message);
          }
//          Assert.IsFalse(Session.Current.ValidationContext.IsValid);
        }
      }
    }

    [Test]
    public void ValidationInConstructor()
    {
      using (Session.Open(Domain)) 
      using (Transaction.Open()) {
        AssertEx.Throws<Exception>(() => {
          var reference1 = new Reference();
        });
        using (var region = Xtensive.Storage.Validation.Disable()) {
          var reference2 = new Reference {
            Title = "Test"
          };
          region.Complete();
        }
        var reference = new Reference("Test");

        AssertEx.Throws<Exception>(() => {
          var referrer1 = new Referrer();
        });
        using (var region = Xtensive.Storage.Validation.Disable()) {
          var referrer2 = new Referrer {
            Title = "Test", 
            Reference = reference
          };
          region.Complete();
        }
        var referrer = new Referrer(reference, "Test");
      }
    }
  }
}