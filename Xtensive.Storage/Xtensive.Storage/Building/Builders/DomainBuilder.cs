// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.03

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using log4net.Config;
using Xtensive.Core;
using Xtensive.Core.Tuples;
using Xtensive.Integrity.Transactions;
using Xtensive.PluginManager;
using Xtensive.Storage.Building.Builders;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Model;
using Xtensive.Storage.Providers;
using Xtensive.Storage.Resources;
using Xtensive.Core.Reflection;
using FieldInfo=Xtensive.Storage.Model.FieldInfo;
using TypeInfo=Xtensive.Storage.Model.TypeInfo;

namespace Xtensive.Storage.Building.Builders
{
  /// <summary>
  /// Utility class for <see cref="Storage"/> building.
  /// </summary>
  public static class DomainBuilder
  {
    private static readonly PluginManager<ProviderAttribute> pluginManager =
      new PluginManager<ProviderAttribute>(typeof (HandlerFactory), Environment.CurrentDirectory);

    /// <summary>
    /// Builds the new <see cref="Domain"/> according to specified configuration.
    /// </summary>
    /// <param name="configuration">The storage configuration.</param>
    /// <returns>Newly created <see cref="Domain"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="configuration"/> is null.</exception>
    /// <exception cref="DomainBuilderException">When at least one error have been occurred 
    /// during storage building process.</exception>
    public static Domain Build(DomainConfiguration configuration)
    {
      ArgumentValidator.EnsureArgumentNotNull(configuration, "configuration");

      if (!configuration.IsLocked)
        configuration.Lock(true);

      using (Log.InfoRegion(Strings.LogValidatingX, typeof(DomainConfiguration).GetShortName()))
        Validate(configuration);

      var context = new BuildingContext(configuration);

      using (Log.InfoRegion(Strings.LogBuildingX, typeof(Domain).GetShortName())) {
        using (new BuildingScope(context)) {
          try {
            CreateDomain();
            CreateHandlerFactory();
            CreateNameBuilder();
            BuildModel();
            BuildPrototypes();
            CreateDomainHandler();
            using (context.Domain.Handler.OpenSession(SessionType.System)) {
              using (var transactionScope = Transaction.Open()) {

                BuildingScope.Context.SystemSessionHandler = Session.Current.Handler;
                using (Log.InfoRegion(String.Format(Strings.LogBuildingX, typeof (DomainHandler).GetShortName())))
                  context.Domain.Handler.Build();
                CreateKeyManager();
                CreateGenerators();

                transactionScope.Complete();
              } 
            }
          }
          catch (DomainBuilderException e) {
            context.RegisterError(e);
          }

          context.EnsureBuildSucceed();
        }
      }
      return context.Domain;
    }

    #region ValidateXxx methods

    private static void Validate(DomainConfiguration configuration)
    {
      if (configuration.Builders.Count > 0)
        foreach (Type type in configuration.Builders)
          ValidateBuilder(type);
    }

    private static void ValidateBuilder(Type type)
    {
      ArgumentValidator.EnsureArgumentNotNull(type, "type");

      ConstructorInfo constructor =
        type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);

      if (constructor==null)
        throw new DomainBuilderException(
          string.Format(Strings.ExTypeXMustHavePublicInstanceParameterlessConstructorInOrderToBeUsedAsStorageDefinitionBuilder, type.FullName));

      if (!typeof (IDomainBuilder).IsAssignableFrom(type))
        throw new DomainBuilderException(
          string.Format(CultureInfo.CurrentCulture,
            Strings.ExTypeXDoesNotImplementYInterface, type.FullName, typeof (IDomainBuilder).FullName));
    }

    #endregion

    private static void CreateDomain()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, typeof(Domain).GetShortName())) {
        var domain = new Domain(BuildingContext.Current.Configuration);
        BuildingContext.Current.Domain = domain;
      }
    }

    private static void CreateHandlerFactory()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, typeof(HandlerFactory).GetShortName())) {
        string protocol = BuildingContext.Current.Configuration.ConnectionInfo.Protocol;
        Type handlerProviderType;
        lock (pluginManager) {
          handlerProviderType = pluginManager[new ProviderAttribute(protocol)];
          if (handlerProviderType==null)
            throw new DomainBuilderException(
              string.Format(Strings.ExStorageProviderNotFound,
                protocol,
                Environment.CurrentDirectory));
        }
        var handlerFactory = (HandlerFactory) Activator.CreateInstance(handlerProviderType, new object[] {BuildingContext.Current.Domain});
        var handlerAccessor = BuildingContext.Current.Domain.Handlers;
        handlerAccessor.HandlerFactory = handlerFactory;
      }
    }

    private static void CreateNameBuilder()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, typeof(NameBuilder).GetShortName())) {
        var handlerAccessor = BuildingContext.Current.Domain.Handlers;
        handlerAccessor.NameBuilder = handlerAccessor.HandlerFactory.CreateHandler<NameBuilder>();
        handlerAccessor.NameBuilder.Initialize(handlerAccessor.Domain.Configuration.NamingConvention);
      }
    }

    private static void CreateDomainHandler()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, typeof(DomainHandler).GetShortName())) {
        var handlerAccessor = BuildingContext.Current.Domain.Handlers;
        handlerAccessor.DomainHandler = handlerAccessor.HandlerFactory.CreateHandler<DomainHandler>();
        handlerAccessor.DomainHandler.Initialize();
      }
    }

    private static void BuildModel()
    {
      using (Log.InfoRegion(Strings.LogBuildingX, Strings.Model)) {
        ModelBuilder.Build();
        var domain = BuildingContext.Current.Domain;
        domain.Model = BuildingContext.Current.Model;
      }
    }

    private static void CreateKeyManager()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, typeof(KeyManager).GetShortName())) {
        var domain = BuildingContext.Current.Domain;
        var handlerAccessor = BuildingContext.Current.Domain.Handlers;
        handlerAccessor.KeyManager = new KeyManager(domain);
      }
    }

    private static void CreateGenerators()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, Strings.Generators)) {
        var handlerAccessor = BuildingContext.Current.Domain.Handlers;
        Registry<HierarchyInfo, Generator> generators = BuildingContext.Current.Domain.KeyManager.Generators;
        GeneratorFactory factory = handlerAccessor.HandlerFactory.CreateHandler<GeneratorFactory>();
        foreach (HierarchyInfo hierarchy in BuildingContext.Current.Model.Hierarchies) {
          Generator generator;
          if (hierarchy.Generator == null)
            continue;
          if (hierarchy.Generator==typeof (Generator))
            generator = factory.CreateGenerator(hierarchy);
          else
            generator = (Generator) Activator.CreateInstance(hierarchy.Generator, new object[] { hierarchy });
          generator.Initialize();
          generators.Register(hierarchy, generator);
        }
        generators.Lock();
      }
    }

    private static void BuildPrototypes()
    {
      using (Log.InfoRegion(Strings.LogCreatingX, "persistent objects prototypes")) {
        var domain = BuildingContext.Current.Domain;
        foreach (TypeInfo type in domain.Model.Types.Where(t => !t.IsInterface)) {
          BitArray nullableMap = new BitArray(type.TupleDescriptor.Count);
          int i = 0;
          foreach (ColumnInfo column in type.Columns)
            nullableMap[i++] = column.IsNullable;
          Tuple prototype = Tuple.Create(type.TupleDescriptor);
          prototype.Initialize(nullableMap);
          if (type.IsEntity) {
            FieldInfo typeIdField = type.Fields[NameBuilder.TypeIdFieldName];
            prototype.SetValue(typeIdField.MappingInfo.Offset, type.TypeId);
          }
          Log.Info("Type '{0}': {1}", type, prototype);
          domain.Prototypes[type] = prototype;
        }
      }
    }
  }
}