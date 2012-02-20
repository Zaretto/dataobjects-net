// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2010.03.01

using System;
using Xtensive.Core.Collections;
using Xtensive.Storage.Operations;

namespace Xtensive.Storage
{
  /// <summary>
  /// Operation sequence contract.
  /// </summary>
  public interface IOperationSequence : ICountable<IOperation>,
    Core.IOperationSequence
  {
    /// <summary>
    /// Applies this operation sequence to the <see cref="Session.Current"/> session.
    /// </summary>
    /// <returns>Key mapping.</returns>
    KeyMapping Replay();

    /// <summary>
    /// Applies this operation sequence to the specified session.
    /// </summary>
    /// <param name="session">The session to apply the sequence to.</param>
    /// <returns>Key mapping.</returns>
    KeyMapping Replay(Session session);
  }
}