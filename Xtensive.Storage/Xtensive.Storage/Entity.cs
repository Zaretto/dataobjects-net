// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.01

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Xtensive.Core;
using Xtensive.Core.Aspects;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Parameters;
using Xtensive.Core.Reflection;
using Xtensive.Core.Threading;
using Xtensive.Core.Tuples;
using Xtensive.Integrity.Aspects;
using Xtensive.Integrity.Validation;
using Xtensive.Storage.Model;
using Xtensive.Storage.Resources;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Rse.Providers;
using Xtensive.Storage.Rse.Providers.Compilable;
using Xtensive.Storage.Serialization;

namespace Xtensive.Storage
{
  /// <summary>
  /// Base class for all entities in a model.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="Entity"/> class encapsulates infrastructure to store persistent transactional data.
  /// It has <see cref="Key"/> property that uniquely identifies the instance within its <see cref="Session"/>.
  /// </para>
  /// <para>All entities in a model should be inherited from this class.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// [HierarchyRoot]
  /// public class Customer : Entity
  /// {
  ///   [Field, Key]
  ///   public int Id { get; set; }
  ///   
  ///   [Field]
  ///   public string Name { get; set; }
  /// }
  /// </code>
  /// </example>
  /// <seealso cref="Structure">Structure class</seealso>
  /// <seealso cref="EntitySet{TItem}"><c>EntitySet</c> class</seealso>
  [SystemType]
  [DebuggerDisplay("{Key}")]
  public abstract class Entity : Persistent,
    IEntity, 
    ISerializable, 
    IDeserializationCallback
  {
    private static readonly ThreadSafeDictionary<Triplet<TypeInfo, LockMode, LockBehavior>, RecordSet>
      lockingQueryCache =
      ThreadSafeDictionary<Triplet<TypeInfo, LockMode, LockBehavior>, RecordSet>.Create(new object());
    private static readonly Parameter<Tuple> keyParameter = new Parameter<Tuple>(WellKnown.KeyFieldName);

    #region Internal properties

    internal EntityState State { get; set; }

    /// <exception cref="Exception">Property is already initialized.</exception>
    [Field]
    internal int TypeId
    {
      [DebuggerStepThrough]
      get { return GetFieldValue<int>(WellKnown.TypeIdFieldName); }
    }

    #endregion

    #region Properties: Key, Type, Tuple, PersistenceState

    /// <summary>
    /// Gets the <see cref="Key"/> that identifies this entity.
    /// </summary>
    [Infrastructure]
    public Key Key
    {
      [DebuggerStepThrough]
      get { return State.Key; }
    }

    /// <summary>
    /// Gets a value indicating whether this entity is removed.
    /// </summary>
    /// <seealso cref="Remove"/>
    public bool IsRemoved {
      get {
        if (Session.IsPersisting)
          // Removed = "already removed from storage" here
          return State.IsNotAvailable; 
        else
          // Removed = "either already removed, or marked as removed" here
          return State.IsNotAvailableOrMarkedAsRemoved;
      }
    }

    /// <inheritdoc/>
    public override sealed TypeInfo Type
    {
      [DebuggerStepThrough]
      get { return State.Type; }
    }

    /// <inheritdoc/>
    protected internal override sealed Tuple Tuple
    {
      [DebuggerStepThrough]
      get { return State.Tuple; }
    }

    /// <summary>
    /// Gets persistence state of the entity.
    /// </summary>
    [Infrastructure]
    public PersistenceState PersistenceState
    {
      [DebuggerStepThrough]
      get { return State.PersistenceState; }
    }

    #endregion

    #region IIdentifier members

    /// <inheritdoc/>
    [Infrastructure] // Proxy
    Key IIdentified<Key>.Identifier
    {
      [DebuggerStepThrough]
      get { return Key; }
    }

    /// <inheritdoc/>
    [Infrastructure] // Proxy
    object IIdentified.Identifier
    {
      [DebuggerStepThrough]
      get { return Key; }
    }

    #endregion

    #region Public members

    /// <summary>
    /// Removes this entity.
    /// </summary>
    /// <exception cref="ReferentialIntegrityException">
    /// Entity is associated with another entity with <see cref="OnRemoveAction.Deny"/> on-remove action.</exception>
    /// <seealso cref="IsRemoved"/>
    public void Remove()
    {
      EnsureNotRemoved();
      using (var region = Validation.Disable()) {
        NotifyRemoving();
        Session.NotifyRemovingEntity(this);

        if (Session.IsDebugEventLoggingEnabled)
          Log.Debug("Session '{0}'. Removing: Key = '{1}'", Session, Key);
        Session.RemovalProcessor.Remove(this);

        NotifyRemove();
        Session.NotifyRemoveEntity(this);

        region.Complete();
      }
    }

    /// <inheritdoc/>
    public override event PropertyChangedEventHandler PropertyChanged
    {
      add {Session.EntityEventBroker.AddSubscriber(Key, EntityEventBroker.PropertyChangedEventKey, value);}
      remove {Session.EntityEventBroker.RemoveSubscriber(Key, EntityEventBroker.PropertyChangedEventKey, value);}
    }

    /// <summary>
    /// Locks this instance in the storage.
    /// </summary>
    /// <param name="lockMode">The lock mode.</param>
    /// <param name="lockBehavior">The lock behavior.</param>
    public void Lock(LockMode lockMode, LockBehavior lockBehavior)
    {
      using (new ParameterContext().Activate()) {
        keyParameter.Value = Key.Value;
        var recordSet = lockingQueryCache
          .GetValue(new Triplet<TypeInfo, LockMode, LockBehavior>(Type, lockMode, lockBehavior), triplet =>
          IndexProvider.Get(triplet.First.Indexes.PrimaryIndex).Result.Seek(keyParameter.Value)
            .Lock(() => triplet.Second, () => triplet.Third).Select());
        recordSet.First();
      }
    }

    #endregion

    #region Protected event-like methods

    /// <inheritdoc/>
    protected internal override bool CanBeValidated
    {
      get { return !IsRemoved; }
    }

    /// <summary>
    /// Called when entity is about to be removed.
    /// </summary>
    /// <remarks>
    /// Override it to perform some actions when entity is about to be removed.
    /// </remarks>
    protected virtual void OnRemoving()
    {
    }

    /// <summary>
    /// Called when entity becomes removed.
    /// </summary>
    /// <remarks>
    /// Override this method to perform some actions when entity is removed.
    /// </remarks>
    protected virtual void OnRemove()
    {
    }

    #endregion

    #region Private \ internal methods

    /// <exception cref="InvalidOperationException">Entity is removed.</exception>
    private void EnsureNotRemoved()
    {
      if (IsRemoved)
        throw new InvalidOperationException(Strings.ExEntityIsRemoved);
    }

    internal override sealed void EnsureIsFetched(FieldInfo field)
    {
      var state = State;

      if (state.PersistenceState==PersistenceState.New)
        return;

      var tuple = state.Tuple;
      if (tuple.IsAvailable(field.MappingInfo.Offset))
        return;

      Session.Handler.FetchField(Key, field, tuple.GetFieldStateMap(TupleFieldState.Available));
    }

    internal override void PrepareToSetField()
    {
      if (PersistenceState!=PersistenceState.New) {
        var dTuple = State.DifferentialTuple;
      }
    }

    internal virtual void NotifyRemoving()
    {
      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.RemovingEntityEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key>) subscriptionInfo.Second)
          .Invoke(subscriptionInfo.First);
      OnRemoving();
    }

    internal virtual void NotifyRemove()
    {
      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.RemoveEntityEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key>) subscriptionInfo.Second).Invoke(subscriptionInfo.First);
      OnRemove();
    }

    #endregion

    #region NotifyXxx & GetSubscription members

    internal override sealed void NotifyInitializing()
    {
      if (!Session.IsSystemLogicOnly) {
        var subscriptionInfo = GetSubscription(EntityEventBroker.InitializingPersistentEventKey);
        if (subscriptionInfo.Second!=null)
          ((Action<Key>) subscriptionInfo.Second).Invoke(subscriptionInfo.First);
      }
      State.Entity = this;
      if (Session.IsDebugEventLoggingEnabled)
        Log.Debug("Session '{0}'. Materializing {1}: Key = '{2}'",
          Session, GetType().GetShortName(), State.Key);
    }

    internal override sealed void NotifyInitialize()
    {
      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.InitializePersistentEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key>) subscriptionInfo.Second)
          .Invoke(subscriptionInfo.First);
      OnInitialize();
    }

    internal override sealed void NotifyGettingFieldValue(FieldInfo fieldInfo)
    {
      EnsureNotRemoved();
      if (Session.IsDebugEventLoggingEnabled)
        Log.Debug("Session '{0}'. Getting value: Key = '{1}', Field = '{2}'", Session, Key, fieldInfo);
      EnsureIsFetched(fieldInfo);
      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.GettingFieldEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key, FieldInfo>) subscriptionInfo.Second)
          .Invoke(subscriptionInfo.First, fieldInfo);
      OnGettingFieldValue(fieldInfo);
    }

    internal override sealed void NotifyGetFieldValue(FieldInfo field, object value)
    {
      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.GetFieldEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key, FieldInfo, object>) subscriptionInfo.Second)
          .Invoke(subscriptionInfo.First, field, value);
      OnGetFieldValue(field, value);
    }

    internal override sealed void NotifySettingFieldValue(FieldInfo field, object value)
    {
      EnsureNotRemoved();
      if (Session.IsDebugEventLoggingEnabled)
        Log.Debug("Session '{0}'. Setting value: Key = '{1}', Field = '{2}'", Session, Key, field);
      if (field.IsPrimaryKey)
        throw new NotSupportedException(string.Format(Strings.ExUnableToSetKeyFieldXExplicitly, field.Name));
      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.SettingFieldEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key, FieldInfo, object>) subscriptionInfo.Second).Invoke(subscriptionInfo.First, field, value);
      OnSettingFieldValue(field, value);
    }

    internal override sealed void NotifySetFieldValue(FieldInfo field, object oldValue, object newValue)
    {
      if (PersistenceState!=PersistenceState.New && PersistenceState!=PersistenceState.Modified) {
        Session.EnforceChangeRegistrySizeLimit(); // Must be done before the next line 
                                                  // to avoid post-first property set flush.
        State.PersistenceState = PersistenceState.Modified;
      }

      if (Session.IsSystemLogicOnly)
        return;
      var subscriptionInfo = GetSubscription(EntityEventBroker.SetFieldEventKey);
      if (subscriptionInfo.Second!=null)
        ((Action<Key, FieldInfo, object, object>) subscriptionInfo.Second)
          .Invoke(subscriptionInfo.First, field, oldValue, newValue);
      OnSetFieldValue(field, oldValue, newValue);
      base.NotifySetFieldValue(field, oldValue, newValue);
    }

    protected internal override void NotifyPropertyChanged(FieldInfo field)
    {
      if (!Session.EntityEventBroker.HasSubscribers)
        return;
      var subscriber = GetSubscription(EntityEventBroker.PropertyChangedEventKey);
      if (subscriber.Second != null)
        ((PropertyChangedEventHandler)subscriber.Second).Invoke(this,
          new PropertyChangedEventArgs(field.Name));
    }

    protected Pair<Key, Delegate> GetSubscription(object eventKey)
    {
      var entityKey = Key;
      return new Pair<Key, Delegate>(entityKey, 
        Session.EntityEventBroker.GetSubscriber(entityKey, eventKey));
    }
    
    #endregion

    #region Serialization-related methods

    [Infrastructure]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      using (CoreServices.OpenSystemLogicOnlyRegion()) {
        SerializationContext.Demand().GetEntityData(this, info, context);
      }
    }

    [Infrastructure]
    void IDeserializationCallback.OnDeserialization(object sender)
    {
      using (CoreServices.OpenSystemLogicOnlyRegion()) {
        DeserializationContext.Demand().OnDeserialization();
      }
    }

    #endregion

    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    protected Entity()
    {
      Key key = Key.Create(GetTypeInfo());
      State = Session.CreateEntityState(key);
      NotifyInitializing();
      Session.NotifyCreateEntity(this);
      this.Validate();
    }

    // Is used for EntitySetItem<,> instance construction
    internal Entity(Tuple keyTuple)
    {
      ArgumentValidator.EnsureArgumentNotNull(keyTuple, "keyTuple");
      Key key = Key.Create(GetTypeInfo(), keyTuple, true);
      State = Session.CreateEntityState(key);
      NotifyInitializing();
      // TODO: Add Session.NotifyCreateEntity()?
      this.Validate();
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="values">The field values that will be used for key building.</param>
    /// <remarks>Use this kind of constructor when you need to explicitly set key for this instance.</remarks>
    /// <example>
    /// <code>
    /// [HierarchyRoot]
    /// public class Book : Entity
    /// {
    ///   [Field, KeyField]
    ///   public string ISBN { get; set; }
    ///   
    ///   public Book(string isbn) : base(isbn) { }
    /// }
    /// </code>
    /// </example>
    protected Entity(params object[] values)
    {
      ArgumentValidator.EnsureArgumentNotNull(values, "values");
      Key key = Key.Create(GetTypeInfo(), true, values);
      State = Session.CreateEntityState(key);
      NotifyInitializing();
      Session.NotifyCreateEntity(this);
      this.Validate();
    }

    /// <summary>
    /// <see cref="ClassDocTemplate()" copy="true"/>
    /// </summary>
    /// <param name="state">The initial state of this instance fetched from storage.</param>
    protected Entity(EntityState state)
    {
      State = state;
      NotifyInitializing();
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/>.</param>
    /// <param name="context">The <see cref="StreamingContext"/>.</param>
    protected Entity(SerializationInfo info, StreamingContext context)
    {
      using (CoreServices.OpenSystemLogicOnlyRegion()) {
        DeserializationContext.Demand().SetEntityData(this, info, context);
      }
    }    
  }
}