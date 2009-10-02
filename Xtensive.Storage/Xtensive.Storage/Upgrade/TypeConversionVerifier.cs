// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.05.28

using System;
using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Core.Reflection;
using Xtensive.Storage.Indexing.Model;

namespace Xtensive.Storage.Upgrade
{
  /// <summary>
  /// Verifies whether the conversion between types is allowed or not.
  /// </summary>
  public static class TypeConversionVerifier
  {
    private static readonly Dictionary<Type, List<Type>> supportedConversions;

    /// <summary>
    /// Verifies whether the source type can be converted to the target type. 
    /// Loss of data is allowed.
    /// </summary>
    /// <param name="from">The source type.</param>
    /// <param name="to">The target type.</param>
    /// <returns>
    /// <see langword="true"/> if the source type can be converted to the 
    /// target type; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool CanConvert(TypeInfo from, TypeInfo to)
    {
      ArgumentValidator.EnsureArgumentNotNull(from, "from");
      ArgumentValidator.EnsureArgumentNotNull(to, "to");

      if (from.IsTypeUndefined || to.IsTypeUndefined)
        return false;
      if (from==to)
        return true;
      if (to.Type==typeof(string))
        return !to.Length.HasValue || CanConvertToString(from, to.Length.Value);
      if (from.IsNullable && !to.IsNullable)
        return false;
      var fromType = from.Type.StripNullable();
      var toType = to.Type.StripNullable();
      return supportedConversions.ContainsKey(fromType) && supportedConversions[fromType].Contains(toType);
    }

    private static bool CanConvertToString(TypeInfo from, int length)
    {
      switch (Type.GetTypeCode(from.Type)) {
      case TypeCode.Char:
      case TypeCode.String:
        return true;
      case TypeCode.Decimal:
        return length >= from.Precision + 2;
      case TypeCode.Byte:
        return length >= 3;
      case TypeCode.SByte:
        return length >= 4;
      case TypeCode.Int16:
        return length >= 6;
      case TypeCode.Int32:
        return length >= 11;
      case TypeCode.Int64:
        return length >= 20;
      case TypeCode.UInt16:
        return length >= 5;
      case TypeCode.UInt32:
        return length >= 10;
      case TypeCode.UInt64:
        return length >= 20;
      default:
        return false;
      }
    }

    /// <summary>
    /// Verifies whether the source type can be converted to the target 
    /// type without loss of data.
    /// </summary>
    /// <param name="from">The source type.</param>
    /// <param name="to">The target type.</param>
    /// <returns>
    /// <see langword="true"/> if the source type can be converted to the 
    /// target type without loss of data; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool CanConvertSafely(TypeInfo from, TypeInfo to)
    {
      ArgumentValidator.EnsureArgumentNotNull(from, "from");
      ArgumentValidator.EnsureArgumentNotNull(to, "to");

      if (to.IsTypeUndefined || from.IsTypeUndefined)
        return false;
      if (to.Type==typeof (string) && from.Type == typeof (string))
        return !to.Length.HasValue || to.Length >= from.Length;

      if (to.Type==typeof(string))
        return !to.Length.HasValue || CanConvertToString(from, to.Length.Value);

      if (to.Type==typeof(decimal) && from.Type==typeof(decimal))
        return from.Scale <= to.Scale && to.Precision <= to.Precision;

      if (!CanConvert(from, to))
        return false;

      return !to.Length.HasValue || to.Length >= from.Length;
    }

    // Constructors

    static TypeConversionVerifier()
    {
      supportedConversions = new Dictionary<Type, List<Type>>();
      AddConverter<Boolean>(
        typeof (Int16), typeof (UInt16), typeof (Int32), typeof (UInt32),
        typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<Byte>(
        typeof (Int16), typeof (UInt16), typeof (Char), typeof (Int32), typeof (UInt32),
        typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single),
        typeof (Decimal));
      AddConverter<SByte>(
        typeof (Int16), typeof (UInt16), typeof (Char), typeof (Int32),
        typeof (UInt32), typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single),
        typeof (Decimal));
      AddConverter<Int16>(
        typeof (Int32), typeof (UInt32), typeof (Int64), typeof (UInt64),
        typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<UInt16>(
        typeof (Char), typeof (Int32), typeof (UInt32), typeof (Int64),
        typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<Int32>(
        typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<UInt32>(
        typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<Int64>(
        typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<UInt64>(
        typeof (Int64), typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<Char>(
        typeof (UInt16), typeof (Int32), typeof (UInt32), typeof (Int64),
        typeof (UInt64), typeof (Double), typeof (Single), typeof (Decimal));
      AddConverter<Decimal>(typeof (Double), typeof (Single), typeof(Decimal));
      AddConverter<Single>(typeof (Double));
    }

    private static void AddConverter<T>(params Type[] types)
    {
      ArgumentValidator.EnsureArgumentIsGreaterThan(types.Length, -1, "types.Length");
      supportedConversions.Add(typeof(T), new List<Type>(types));
    }
  }
}