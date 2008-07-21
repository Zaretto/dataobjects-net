// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.01.05

using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Core.Collections;

namespace Xtensive.Integrity.Validation.Interfaces
{
  /// <summary>
  /// Implemented by objects supporting validation framework.
  /// </summary>
  public interface IValidationAware : 
    IContextBound<ValidationContextBase>
  {
    /// <summary>
    /// Validates the object state.
    /// </summary>
    /// <remarks>
    /// Throws an exception on validation failure.
    /// </remarks>
    void OnValidate(HashSet<string> regions);

    /// <summary>
    /// Determines whether the specified context is compatible 
    /// with the current <see cref="IValidationAware"/> object.
    /// </summary>
    /// <param name="context">The context to check for compatibility.</param>
    /// <returns>
    /// <see langword="true"/> if the specified context is compatible
    /// with the current <see cref="IValidationAware"/> object; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool IsCompatibleWith(ValidationContextBase context);
  }
}