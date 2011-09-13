// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.07.06

using System;
using System.Collections.Generic;
using System.Transactions;
using Xtensive.Core;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Tuple = Xtensive.Core.Tuples.Tuple;
using Xtensive.Storage.Internals.Prefetch;
using Xtensive.Storage.Linq;
using Xtensive.Storage.Providers;

namespace Xtensive.Storage.Internals
{
  /// <summary>
  /// The base class for <see cref="SessionHandler"/>s which support the chaining 
  /// with another handler.
  /// </summary>
  public abstract class ChainingSessionHandler : SessionHandler
  {
    /// <summary>
    /// The chained handler.
    /// </summary>
    protected readonly SessionHandler chainedHandler;

    /// <inheritdoc/>
    public override QueryProvider QueryProvider { get { return chainedHandler.QueryProvider; } }

    internal override int PrefetchTaskExecutionCount {
      get { return chainedHandler.PrefetchTaskExecutionCount; }
    }

    /// <inheritdoc/>
    public override bool TransactionIsStarted { get { return chainedHandler.TransactionIsStarted; } }

    public override void SetCommandTimeout(int? commandTimeout)
    {
      chainedHandler.SetCommandTimeout(commandTimeout);
    }

    /// <inheritdoc/>
    public override void BeginTransaction(IsolationLevel isolationLevel)
    {
      chainedHandler.BeginTransaction(isolationLevel);
    }

    /// <inheritdoc/>
    public override void CommitTransaction()
    {
      chainedHandler.CommitTransaction();
    }

    /// <inheritdoc/>
    public override void RollbackTransaction()
    {
      chainedHandler.RollbackTransaction();
    }

    /// <inheritdoc/>
    public override void CreateSavepoint(string name)
    {
      chainedHandler.CreateSavepoint(name);
    }

    /// <inheritdoc/>
    public override void RollbackToSavepoint(string name)
    {
      chainedHandler.RollbackToSavepoint(name);
    }

    /// <inheritdoc/>
    public override void ReleaseSavepoint(string name)
    {
      chainedHandler.ReleaseSavepoint(name);
    }

    /// <inheritdoc/>
    public override void ExecuteQueryTasks(IEnumerable<QueryTask> queryTasks, bool allowPartialExecution)
    {
      chainedHandler.ExecuteQueryTasks(queryTasks, allowPartialExecution);
    }

    /// <inheritdoc/>
    public override void Persist(EntityChangeRegistry registry, bool allowPartialExecution)
    {
      chainedHandler.Persist(registry, allowPartialExecution);
    }

    /// <inheritdoc/>
    public override void Persist(IEnumerable<PersistAction> persistActions, bool allowPartialExecution)
    {
      chainedHandler.Persist(persistActions, allowPartialExecution);
    }

    /// <inheritdoc/>
    public override StrongReferenceContainer Prefetch(Key key, Model.TypeInfo type,
      FieldDescriptorCollection descriptors)
    {
      return chainedHandler.Prefetch(key, type, descriptors);
    }

    /// <inheritdoc/>
    public override StrongReferenceContainer ExecutePrefetchTasks(bool skipPersist)
    {
      return chainedHandler.ExecutePrefetchTasks(skipPersist);
    }

    /// <inheritdoc/>
    public override EntityState FetchEntityState(Key key)
    {
      return chainedHandler.FetchEntityState(key);
    }

    /// <inheritdoc/>
    public override void FetchField(Key key, Model.FieldInfo field)
    {
      chainedHandler.FetchField(key, field);
    }

    /// <inheritdoc/>
    public override void FetchEntitySet(Key ownerKey, Model.FieldInfo field, int? itemCountLimit)
    {
      chainedHandler.FetchEntitySet(ownerKey, field, itemCountLimit);
    }

    internal override EntitySetState RegisterEntitySetState(Key key, Model.FieldInfo fieldInfo,
      bool isFullyLoaded, List<Key> entities, List<Pair<Key, Tuple>> auxEntities)
    {
      return chainedHandler.RegisterEntitySetState(key, fieldInfo, isFullyLoaded, entities, auxEntities);
    }

    internal override EntityState RegisterEntityState(Key key, Tuple tuple)
    {
      return chainedHandler.RegisterEntityState(key, tuple);
    }

    internal override bool TryGetEntitySetState(Key key, Model.FieldInfo fieldInfo,
      out EntitySetState entitySetState)
    {
      return chainedHandler.TryGetEntitySetState(key, fieldInfo, out entitySetState);
    }

    internal override bool TryGetEntityState(Key key, out EntityState entityState)
    {
      return chainedHandler.TryGetEntityState(key, out entityState);
    }

    /// <inheritdoc/>
    public override T GetService<T>()
    {
      return chainedHandler.GetService<T>();
    }

    /// <inheritdoc/>
    public override Rse.Providers.EnumerationContext CreateEnumerationContext()
    {
      return chainedHandler.CreateEnumerationContext();
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="chainedHandler">The handler to be chained.</param>
    protected ChainingSessionHandler(SessionHandler chainedHandler)
    {
      ArgumentValidator.EnsureArgumentNotNull(chainedHandler, "chainedHandler");
      this.chainedHandler = chainedHandler;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
      chainedHandler.Dispose();
    }
  }
}