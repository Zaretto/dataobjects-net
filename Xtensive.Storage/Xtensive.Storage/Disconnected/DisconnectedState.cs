// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.10.23

using System.Collections.Generic;
using System.Runtime.Serialization;
using Xtensive.Core;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;
using Xtensive.Storage.Model;
using Xtensive.Storage.Internals;
using System;
using Xtensive.Storage.Operations;
using Xtensive.Storage.Resources;
using Xtensive.Core.Disposing;
using System.Linq;

namespace Xtensive.Storage.Disconnected
{
  /// <summary>
  /// Disconnected state.
  /// </summary>
  [Serializable]
  public sealed class DisconnectedState
  {
    private OperationSet serializedSet;
    private KeyValuePair<string, VersionInfo>[] serializedVersions;
    private SerializableEntityState[] serializedRegistry;
    private SerializableEntityState[] serializedGlobalRegistry;
    
    [NonSerialized]
    private Dictionary<Key, VersionInfo> versionCache;
    [NonSerialized]
    private StateRegistry transactionalRegistry;
    [NonSerialized]
    private StateRegistry registry;
    [NonSerialized]
    private StateRegistry globalRegistry;
    [NonSerialized]
    private IDisposable disposable;
    [NonSerialized]
    private DisconnectedSessionHandler handler;
    [NonSerialized]
    private Session session;
    [NonSerialized]
    private Logger logger;
    [NonSerialized]
    private readonly ModelHelper modelHelper = new ModelHelper();
    
    internal ModelHelper ModelHelper
    {
      get { return modelHelper; }
    }

    internal bool IsConnected { get; set; }
    internal IOperationSet GetOperationLog()
    {
      return registry.OperationSet;
    }
    
    # region Public API

    /// <summary>
    /// Gets the session this instance attached to.
    /// </summary>
    public Session Session { get { return session; } }

    /// <summary>
    /// Gets a value indicating whether this instance is attached to session.
    /// </summary>
    public bool IsAttached { get { return disposable!=null; } }

    /// <summary>
    /// Attach to specified session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns>Scope.</returns>
    public IDisposable Attach(Session session)
    {
      ArgumentValidator.EnsureArgumentNotNull(session, "session");
      EnsureIsNotAttached();
      if (session.Transaction!=null)
        throw new InvalidOperationException(Strings.ExActiveTransactionIsPresent);
      
      AttachInternal(session);
      return new Disposable<DisconnectedState>(this, (b, _this) => _this.Detach());
    }

    /// <summary>
    /// Opens connection.
    /// </summary>
    /// <returns>Scope.</returns>
    public IDisposable Connect()
    {
      EnsureIsAttached();
      if (IsConnected)
        return null;
      
      IsConnected = true;
      return new Disposable<DisconnectedState>(this, (b, _this) => _this.CloseConnection());
    }

    /// <summary>
    /// Saves all the changes to storage.
    /// </summary>
    public KeyMapping SaveChanges()
    {
      EnsureIsAttached();
      KeyMapping keyMapping;
      var tempSession = Session;
      Detach();
      try {
        using (new VersionValidator(tempSession, GetStoredVerion)) {
          using (var transactionScope = Transaction.Open(tempSession)) {
            keyMapping = registry.OperationSet.Apply(tempSession);
            transactionScope.Complete();
          }
          registry.Commit();
        }
      }
      finally {
        AttachInternal(tempSession);
      }
      return keyMapping;
    }

    /// <summary>
    /// Clears all changes.
    /// </summary>
    public void ClearChanges()
    {
      registry = new StateRegistry(globalRegistry);
      if (transactionalRegistry!=null)
        transactionalRegistry = new StateRegistry(registry);
    }
    
    # endregion
    
    # region Internal API

    internal VersionInfo GetStoredVerion(Key key)
    {
      VersionInfo storedVersion;
      if (versionCache.TryGetValue(key, out storedVersion))
        return storedVersion;
      return new VersionInfo();
    }

    internal void OnTransactionStarted()
    {
      transactionalRegistry = new StateRegistry(registry);
      logger = new Logger(Session, transactionalRegistry.OperationSet);
    }

    internal void OnTransactionCommited()
    {
      transactionalRegistry.Commit();
      transactionalRegistry = null;
      logger.DisposeSafely();
      logger = null;
    }

    internal void OnTransactionRollbacked()
    {
      logger.DisposeSafely();
      transactionalRegistry = null;
    }

    internal DisconnectedEntityState GetState(Key key)
    {
      EnsureIsAttached();
      return transactionalRegistry.GetState(key);
    }

    internal DisconnectedEntityState RegisterState(Key key, Tuple tuple)
    {
      EnsureIsAttached();

      // Store object version
      if (key.TypeRef.Type.VersionExtractor!=null && !versionCache.ContainsKey(key)) {
        var versionTuple = key.TypeRef.Type.VersionExtractor.Apply(TupleTransformType.Tuple, tuple);
        versionCache.Add(key, new VersionInfo(versionTuple));
      }

      globalRegistry.Register(key, tuple);
      return GetState(key);
    }

    internal DisconnectedEntitySetState RegisterSetState(Key key, FieldInfo fieldInfo, bool isFullyLoaded, 
      List<Key> entities, List<Pair<Key, Tuple>> auxEntities)
    {
      EnsureIsAttached();

      if (auxEntities!=null)
        foreach (var entity in auxEntities)
          RegisterState(entity.First, entity.Second);

      var state = registry.GetForUpdate(key);
      var setState = state.GetEntitySetState(fieldInfo);
      setState.IsFullyLoaded = isFullyLoaded;
      foreach (var entity in entities)
        if (!setState.Items.ContainsKey(entity))
          setState.Items.Add(entity, entity);
      return transactionalRegistry.GetState(key).GetEntitySetState(fieldInfo);
    }

    internal void Persist(EntityState entityState, PersistActionKind persistAction)
    {
      EnsureIsAttached();

      switch (persistAction) {
        case PersistActionKind.Insert:
          transactionalRegistry.Insert(entityState.Key, entityState.Tuple.ToRegular());
          break;
        case PersistActionKind.Update:
          transactionalRegistry.Update(entityState.Key, entityState.DifferentialTuple.Difference.ToRegular());
          break;
        case PersistActionKind.Remove:
          transactionalRegistry.Remove(entityState.Key);
          break;
        default:
          throw new ArgumentOutOfRangeException("persistAction");
      }
    }

    # endregion
    
    private void CloseConnection()
    {
      if (!IsConnected)
        return;
      IsConnected = false;
      handler.CommitChainedTransaction();
    }

    private void EnsureIsNotAttached()
    {
      if (IsAttached)
        throw new InvalidOperationException(Strings.ExDisconnectedStateIsAlreadyAttachedToSession);
    }

    private void EnsureIsAttached()
    {
      if (!IsAttached)
        throw new InvalidOperationException(Strings.ExDisconnectedStateIsDetached);
    }

    private void AttachInternal(Session session)
    {
      this.session = session;
      handler = new DisconnectedSessionHandler(Session.Handler, this);
      disposable = Session.CoreServices.ChangeSessionHandler(handler);
    }

    private void Detach()
    {
      EnsureIsAttached();
      if (IsConnected)
        CloseConnection();
      session = null;
      handler = null;
      disposable.Dispose();
      disposable = null;
    }

    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    public DisconnectedState()
    {
      modelHelper = new ModelHelper();
      globalRegistry = new StateRegistry(this);
      registry = new StateRegistry(globalRegistry);
      versionCache = new Dictionary<Key, VersionInfo>();
    }


    // Serialization

    [OnSerializing]
    protected void OnSerializing(StreamingContext context)
    {
      serializedVersions = versionCache.Select(pair => 
        new KeyValuePair<string, VersionInfo>(pair.Key.ToString(true), pair.Value))
        .ToArray();
      serializedSet = registry.OperationSet;
      serializedRegistry = registry.States.Select(state => state.Serialize()).ToArray();
      serializedGlobalRegistry = globalRegistry.States.Select(state => state.Serialize()).ToArray();
    }

    [OnSerialized]
    protected void OnSerialized(StreamingContext context)
    {
      serializedRegistry = null;
      serializedGlobalRegistry = null;
      serializedSet = null;
      serializedVersions = null;
    }

    [OnDeserialized]
    protected void OnDeserialized(StreamingContext context)
    {
      var domain = Session.Demand().Domain;

      versionCache = new Dictionary<Key, VersionInfo>();
      foreach (var pair in serializedVersions)
        versionCache.Add(Key.Parse(domain, pair.Key), pair.Value);

      globalRegistry = new StateRegistry(this);
      foreach (var state in serializedGlobalRegistry)
        globalRegistry.AddState(DisconnectedEntityState.Deserialize(state, globalRegistry, domain));
      serializedGlobalRegistry = null;

      registry = new StateRegistry(globalRegistry);
      registry.OperationSet = serializedSet;
      foreach (var state in serializedRegistry)
        registry.AddState(DisconnectedEntityState.Deserialize(state, registry, domain));
      serializedRegistry = null;
      serializedSet = null;
    }
  }
}