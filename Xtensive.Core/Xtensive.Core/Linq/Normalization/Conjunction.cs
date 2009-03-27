// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.03.26

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Xtensive.Core.Linq.Normalization
{
  /// <summary>
  /// A conjunction of many operands.
  /// </summary>
  /// <typeparam name="T">The type of operands.</typeparam>
  [Serializable]
  public class Conjunction<T> : MultiOperandOperation<T>
  {
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">All operands must be Expressions with type Boolean.</exception>
    public override Expression ToExpression()
    {
      var operands = new Stack<Expression>();
      foreach (var operand in Operands) {
        var expression = operand as Expression;
        if (expression==null || expression.Type!=typeof (bool))
          throw new InvalidOperationException("All operands must be Expressions with type Boolean.");
        operands.Push(expression);
      }

      if (operands.Count==0) {
        return null;
      }

      if (operands.Count==1) {
        return operands.Pop();
      }

      var result = Expression.And(operands.Pop(), operands.Pop());

      while (operands.Count > 0) {
        result = Expression.And(operands.Pop(), result);
      }

      return result;
    }


    // Constructors

    /// <inheritdoc/>
    public Conjunction()
    {
    }

    /// <inheritdoc/>
    public Conjunction(IEnumerable<T> operands)
      : base(operands)
    {
    }

    /// <inheritdoc/>
    public Conjunction(IEnumerable<T> operands, params IEnumerable<T>[] operandSets)
      : base(operands, operandSets)
    {
    }

    /// <inheritdoc/>
    public Conjunction(T operand, params T[] operands)
      :base(operand, operands)
    {
    }
    
  }
}