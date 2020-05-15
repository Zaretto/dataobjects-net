// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2008.08.14

using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Xtensive.Core
{
  /// <summary>
  /// Provides storing context-specific <see cref="Parameter{TValue}"/>'s values.
  /// </summary>
  public sealed class ParameterContext// : Context<ParameterScope>
  {
    private readonly ParameterContext outerContext;

    private readonly Dictionary<Parameter, object> values =
      new Dictionary<Parameter, object>();
    private readonly bool isExpectedValuesContext;
    private static readonly ParameterContext expectedValues = new ParameterContext(true);

    // /// <summary>
    // /// Gets the current <see cref="ParameterContext"/>.
    // /// </summary>
    // public static ParameterContext Current {
    //   [DebuggerStepThrough]
    //   get { return Scope<ParameterContext>.CurrentContext; }
    // }

    /// <summary>
    /// Gets the special singleton <see cref="ParameterContext"/> instance 
    /// returning <see cref="Parameter.ExpectedValue"/> instead of <see cref="Parameter.Value"/> 
    /// if <see cref="Parameter.ExpectedValue"/> is set.
    /// </summary>        
    public static ParameterContext ExpectedValues {
      [DebuggerStepThrough]
      get { return expectedValues; }
    }

    // #region IContext<...> methods
    //
    // /// <inheritdoc/>
    // public override bool IsActive {
    //   [DebuggerStepThrough]
    //   get { return Current == this; }
    // }
    //
    // /// <inheritdoc/>
    // [DebuggerStepThrough]
    // protected override ParameterScope CreateActiveScope()
    // {
    //   return new ParameterScope(this);
    // }
    //
    // #endregion

    #region Private \ internal methods

    [DebuggerStepThrough]
    internal bool TryGetValue(Parameter parameter, out object value)
    {
      if (isExpectedValuesContext && parameter.IsExpectedValueSet) {
        value = parameter.ExpectedValue;
        return true;
      }

      return values.TryGetValue(parameter, out value)
        || outerContext?.TryGetValue(parameter, out value) == true;
    }
    
    public TValue GetValue<TValue>(Parameter<TValue> parameter)
    {
      if (TryGetValue(parameter, out var result)) {
        return (TValue) result;
      }

      throw new InvalidOperationException(string.Format(Strings.ExValueForParameterXIsNotSet, parameter));
    }

    [DebuggerStepThrough]
    internal void SetValue(Parameter parameter, object value)
    {
      EnsureIsRegular();
      values[parameter] = value;
    }

    [DebuggerStepThrough]
    internal void Clear(Parameter parameter)
    {
      EnsureIsRegular();
      values.Remove(parameter);
    }

    [DebuggerStepThrough]
    internal bool HasValue(Parameter parameter)
    {
      EnsureIsRegular();
      return values.ContainsKey(parameter);
    }

    [DebuggerStepThrough]
    internal void NotifyParametersOnDisposing()
    {
    }

    /// <exception cref="InvalidOperationException">Context is <see cref="ExpectedValues"/> context.</exception>
    [DebuggerStepThrough]
    private void EnsureIsRegular()
    {
      if (isExpectedValuesContext)
        throw new InvalidOperationException(
          Strings.ExThisOperationIsNotAllowedForParameterContextOperatingWithExpectedValuesOfParameters);
    }

    #endregion


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    public ParameterContext(ParameterContext outerContext = null)
    {
      this.outerContext = outerContext;
    }

    private ParameterContext(bool isExpectedValuesContext)
    {
      this.isExpectedValuesContext = isExpectedValuesContext;
    }
  }
}
