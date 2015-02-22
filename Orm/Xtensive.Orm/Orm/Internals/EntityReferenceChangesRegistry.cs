﻿// Copyright (C) 2014 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kulakov
// Created:    2014.09.17

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xtensive.Collections;

namespace Xtensive.Orm.Internals
{
  internal class EntityReferenceChangesRegistry : SessionBound
  {
    private const int InitialReferencesCount = 1;
    private object lockableObject = new object();

    /// <summary>
    /// For removed references and added references:
    /// TKey is entity which referenced;
    /// TValue is dictionary of entities where TKey is referencing entity and TValue is count of references
    /// </summary>
    private readonly IDictionary<EntityState, IDictionary<EntityState, int>> removedReferences = new Dictionary<EntityState, IDictionary<EntityState, int>>();
    private readonly IDictionary<EntityState, IDictionary<EntityState, int>> addedReferences = new Dictionary<EntityState, IDictionary<EntityState, int>>();

    /// <summary>
    /// Gets count of removed references in registry.
    /// </summary>
    public int RemovedReferencesCount { get { return removedReferences.Values.Sum(el => el.Values.Sum()); } }

    /// <summary>
    /// Gets count of added references in registry.
    /// </summary>
    public int AddedReferencesCount { get { return addedReferences.Values.Sum(el => el.Values.Sum()); } }

    /// <summary>
    /// Checks that reference from <paramref name="referencingEntity"/> was removed since last saving.
    /// </summary>
    /// <param name="referencedEntity">Referenced entity.</param>
    /// <param name="referencingEntity">Referensing entity.</param>
    /// <returns><see langword="true"/> if registry contains information about removed reference, otherwise, <see langword="false"/>.</returns>
    public bool IsReferenceDeleted(EntityState referencedEntity, EntityState referencingEntity)
    {
      if (referencingEntity.PersistenceState==PersistenceState.Removed)
        return true;
      IDictionary<EntityState, int> foundEntities;
      if (!removedReferences.TryGetValue(referencedEntity, out foundEntities))
        return false;
      var isExistsInRemoved = foundEntities.ContainsKey(referencingEntity) && foundEntities.Count>0;
      return isExistsInRemoved;
    }

    /// <summary>
    /// Gets all states which no longer referencing to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">Referenced state</param>
    /// <returns>Read-only list of states which no longer referencing to <paramref name="target"/>.</returns>
    public ReadOnlyDictionary<EntityState, int> GetRemovedReferences(EntityState target)
    {
      IDictionary<EntityState, int> referencesToTarget;
      if(this.removedReferences.TryGetValue(target, out referencesToTarget))
        return new ReadOnlyDictionary<EntityState, int>(referencesToTarget);
      var dictionary = new Dictionary<EntityState, int>();
      return new ReadOnlyDictionary<EntityState, int>(dictionary);
    }

    /// <summary>
    /// Gets all states which add reference to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">Referenced state.</param>
    /// <returns>Read-only list of states which add reference to <paramref name="target"/>.</returns>
    public ReadOnlyDictionary<EntityState, int> GetAddedReferences(EntityState target)
    {
      IDictionary<EntityState, int> referencesToTarget;
      if (this.addedReferences.TryGetValue(target, out referencesToTarget))
        return new ReadOnlyDictionary<EntityState, int>(referencesToTarget);
      var dictionary = new Dictionary<EntityState, int>();
      return new ReadOnlyDictionary<EntityState, int>(dictionary);
    }

    /// <summary>
    /// Registers removing of reference between <paramref name="referencedEntityState"/> and <paramref name="referencedEntityState"/>.
    /// </summary>
    /// <param name="referencedEntityState">Referenced <see cref="EntityState"/>.</param>
    /// <param name="noLongerReferncingEntityState"><see cref="EntityState"/> which no longer referencing to <paramref name="referencedEntityState"/>.</param>
    public void RegisterRemovedReference(EntityState referencedEntityState, EntityState noLongerReferncingEntityState)
    {
      lock (lockableObject) {
        IDictionary<EntityState, int> foundStates;
        if (addedReferences.TryGetValue(referencedEntityState, out foundStates)) {
          if (foundStates.ContainsKey(noLongerReferncingEntityState)) {
            if (foundStates[noLongerReferncingEntityState]==InitialReferencesCount) {
              foundStates.Remove(noLongerReferncingEntityState);
              return;
            }
            if (foundStates[noLongerReferncingEntityState] > 0) {
              foundStates[noLongerReferncingEntityState]--;
              return;
            }
          }
        }

        if (removedReferences.TryGetValue(referencedEntityState, out foundStates)) {
          if (foundStates.ContainsKey(noLongerReferncingEntityState)) {
            foundStates[noLongerReferncingEntityState]++;
          }
          else {
            foundStates.Add(noLongerReferncingEntityState, InitialReferencesCount);
          }
        }
        else {
          var dictionary = new Dictionary<EntityState, int>();
          dictionary.Add(noLongerReferncingEntityState, InitialReferencesCount);
          removedReferences.Add(referencedEntityState, dictionary);
        }
      }
    }

    /// <summary>
    /// Registers addition of reference between <paramref name="referencedEntityState"/> and <paramref name="referencedEntityState"/>.
    /// </summary>
    /// <param name="referencedEntityState">Referenced <see cref="EntityState"/>.</param>
    /// <param name="newReferencingEntity">Referencing <see cref="EntityState"/>.</param>
    public void RegisterAddedReference(EntityState referencedEntityState, EntityState newReferencingEntity)
    {
      lock (lockableObject) {

        IDictionary<EntityState, int> foundStates;
        if (removedReferences.TryGetValue(referencedEntityState, out foundStates)) {
          if (foundStates.ContainsKey(newReferencingEntity)) {
            if (foundStates[newReferencingEntity]==InitialReferencesCount) {
              foundStates.Remove(newReferencingEntity);
              return;
            }
            if (foundStates[newReferencingEntity] > 0) {
              foundStates[newReferencingEntity]--;
              return;
            }
          }
        }

        if (addedReferences.TryGetValue(referencedEntityState, out foundStates)) {
          if (foundStates.ContainsKey(newReferencingEntity)) {
            foundStates[newReferencingEntity]++;
          }
          else
            foundStates.Add(newReferencingEntity, InitialReferencesCount);
        }
        else {
          var dictionary = new Dictionary<EntityState, int>();
          dictionary.Add(newReferencingEntity, InitialReferencesCount);
          addedReferences.Add(referencedEntityState, dictionary);
        }
      }
    }
    
    /// <summary>
    /// Clears registry.
    /// </summary>
    public void Clear()
    {
      lock (lockableObject) {
        removedReferences.Clear();
        addedReferences.Clear();
      }
    }

    internal EntityReferenceChangesRegistry(Session session)
      :base(session)
    {
    }
  }
}
