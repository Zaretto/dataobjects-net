// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2007.12.20

using System;
using System.Diagnostics;
using System.Text;
using Xtensive.Core;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Tuple = Xtensive.Core.Tuples.Tuple;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Model;
using Xtensive.Storage.Resources;
using Xtensive.Core.Reflection;
using Xtensive.Core.Helpers;
using System.Linq;

namespace Xtensive.Storage
{
  /// <summary>
  /// Identifies a particular <see cref="Entity"/>.
  /// Stores the set of <see cref="Entity"/>'s <see cref="KeyAttribute">[Key]</see> field values, 
  /// as well as <see cref="HierarchyInfo"/> the entity belongs to.
  /// </summary>
  /// <remarks>
  /// Every entity is uniquely identified by its <see cref="Entity.Key"/>.
  /// </remarks>
  /// <seealso cref="Entity.Key"/>
  public abstract class Key : IEquatable<Key>
  {
    private const char KeyFormatEscape    = '\\';
    private const char KeyFormatDelimiter = ':';
    private int? hashCode;
    private string cachedFormatResult;

    /// <summary>
    /// Protected member caching the tuple with key values.
    /// Can be <see langword="null" />, if the value isn't materialized yet.
    /// </summary>
    protected Tuple value;

    /// <summary>
    /// Gets the key value.
    /// </summary>
    public Tuple Value {
      get {
        if (value == null)
          value = GetValue();
        return value;
      }
    }

    /// <summary>
    /// Gets the <see cref="TypeReference"/> object
    /// describing the type this key belongs to.
    /// </summary>
    public TypeReference TypeReference { get; internal set; }

    /// <summary>
    /// Gets the type of <see cref="Entity"/> this instance identifies.
    /// </summary>
    /// <exception cref="InvalidOperationException">Unable to resolve type for Key.</exception>
    public TypeInfo Type {
      get {
        if (TypeReference.Accuracy == TypeReferenceAccuracy.ExactType)
          return TypeReference.Type;

        var session = Session.Current;
        if (session==null)
          return null;

        var domain = session.Domain;
        var keyCache = domain.KeyCache;
        Key cachedKey;

        lock (keyCache)
          keyCache.TryGetItem(this, true, out cachedKey);
        if (cachedKey!=null) {
          TypeReference = cachedKey.TypeReference;
          return TypeReference.Type;
        }

        var hierarchy = TypeReference.Type.Hierarchy;
        if (hierarchy != null && hierarchy.Types.Count==1) {
          TypeReference = new TypeReference(hierarchy.Types[0], TypeReferenceAccuracy.ExactType);
          return TypeReference.Type;
        }

        if (IsTemporary(domain))
          return TypeReference.Type;

        if (session.IsDebugEventLoggingEnabled)
          Log.Debug(Strings.LogSessionXResolvingKeyYExactTypeIsUnknownFetchIsRequired, session, this);

        var entityState = session.Handler.FetchEntityState(this);
        if (entityState==null || entityState.IsNotAvailableOrMarkedAsRemoved)
          throw new InvalidOperationException(string.Format(Strings.ExUnableToResolveTypeForKeyX, this));
        TypeReference = new TypeReference(entityState.Type, TypeReferenceAccuracy.ExactType);
        return TypeReference.Type;

      }
    }

    /// <summary>
    /// Determines whether this key is a temporary key 
    /// in the <see cref="Domain.Demand">current</see> <see cref="Domain"/>.
    /// </summary>
    public bool IsTemporary()
    {
      return IsTemporary(Domain.Demand());
    }

    /// <summary>
    /// Determines whether this key is a temporary key in the specified <paramref name="domain"/>.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns>Check result.</returns>
    public bool IsTemporary(Domain domain)
    {
      var keyInfo = TypeReference.Type.Key;
      var keyGenerator = domain.KeyGenerators[keyInfo];
      return keyGenerator!=null && keyGenerator.IsTemporaryKey(Value);
    }

    /// <summary>
    /// Gets the value in form of <see cref="Tuple"/>.
    /// </summary>
    protected abstract Tuple GetValue();

    /// <summary>
    /// Calculates hash code.
    /// </summary>
    /// <returns>Calculated hash code.</returns>
    protected abstract int CalculateHashCode();

    /// <summary>
    /// Determines whether <see cref="Type"/> property has exact type value or not.
    /// </summary>
    internal bool HasExactType
    {
      get { return TypeReference.Accuracy == TypeReferenceAccuracy.ExactType; }
    }

    #region Equals, GetHashCode, ==, != 

    /// <inheritdoc/>
    public bool Equals(Key other)
    {
      if (ReferenceEquals(other, null))
        return false;
      if (ReferenceEquals(this, other))
        return true;
      if (GetHashCode()!=other.GetHashCode())
        return false;
      if (HasExactType && other.HasExactType && TypeReference.Type != other.TypeReference.Type)
        return false;
      if (TypeReference.Type.Key.EqualityIdentifier!=other.TypeReference.Type.Key.EqualityIdentifier)
        return false;
      if (other.GetType().IsGenericType)
        return other.ValueEquals(this);
      return ValueEquals(other);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
      if (obj==null)
        return false;
      var other = obj as Key;
      if (other==null)
        return false;
      return Equals(other);
    }

    /// <see cref="ClassDocTemplate.OperatorEq" copy="true" />
    [DebuggerStepThrough]
    public static bool operator ==(Key left, Key right)
    {
      return Equals(left, right);
    }

    /// <see cref="ClassDocTemplate.OperatorNeq" copy="true" />
    [DebuggerStepThrough]
    public static bool operator !=(Key left, Key right)
    {
      return !Equals(left, right);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      if (hashCode.HasValue)
        return hashCode.Value;

      hashCode = CalculateHashCode();
      return hashCode.Value;
    }

    /// <summary>
    /// Compares key value for equality.
    /// </summary>
    /// <param name="other">The other key to compare.</param>
    /// <returns>Equality comparison result.</returns>
    protected abstract bool ValueEquals(Key other);

    #endregion

    #region ToString(bool), Format, Parse methods

    /// <summary>
    /// Converts the <see cref="Key"/> to its string representation.
    /// </summary>
    /// <param name="format">Indicates whether to use <see cref="Format"/>,
    /// or <see cref="ToString()"/> method.</param>
    /// <returns>String representation of the <see cref="Key"/>.</returns>
    public string ToString(bool format)
    {
      return format ? Format() : ToString();
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    public string Format()
    {
      if (cachedFormatResult==null) {
        return new[] {
          Value.Format(),
          TypeReference.Type.UnderlyingType.FullName
        }.RevertibleJoin(KeyFormatEscape, KeyFormatDelimiter);
      }
      return cachedFormatResult;
    }

    /// <summary>
    /// Parses the specified <paramref name="source"/> string 
    /// produced by <see cref="Format"/> back to the <see cref="Key"/>
    /// instance.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns><see cref="Key"/> instance corresponding to the specified
    /// <paramref name="source"/> string.</returns>
    /// <remarks>This method requires open <see cref="Session"/>.</remarks>
    public static Key Parse(string source)
    {
      return Parse(Domain.Demand(), source);
    }

    /// <summary>
    /// Parses the specified <paramref name="source"/> string
    /// produced by <see cref="Format"/> back to the <see cref="Key"/>
    /// instance.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <param name="domain">The domain.</param>
    /// <returns>
    /// <see cref="Key"/> instance corresponding to the specified
    /// <paramref name="source"/> string.
    /// </returns>
    /// <exception cref="InvalidOperationException">Invalid key format.</exception>
    public static Key Parse(Domain domain, string source)
    {
      if (source==null)
        return null;
      var parts = source.RevertibleSplit(KeyFormatEscape, KeyFormatDelimiter).ToList();
      if (parts.Count != 2 || parts.Contains(null))
        throw new InvalidOperationException(Strings.ExInvalidKeyString);
      var valueString = parts[0];
      var typeName = parts[1];

      var typeInfo = domain.Model.Types.Find(typeName);
      if (typeInfo==null)
        throw new InvalidOperationException(
          Strings.ExTypeWithNameXIsNotRegistered.FormatWith(typeName));
      var value = typeInfo.Key.TupleDescriptor.Parse(valueString);
      return Create(domain, typeInfo, TypeReferenceAccuracy.Hierarchy, value);
    }

    #endregion

    #region ToString() method

    /// <inheritdoc/>
    public override string ToString()
    {
      if (HasExactType)
        return string.Format(Strings.KeyFormat,
          Type.UnderlyingType.GetShortName(),
          Value.ToRegular());
      else
        return string.Format(Strings.KeyFormatUnknownKeyType,
          TypeReference.Type.UnderlyingType.GetShortName(),
          Value.ToRegular());
    }

    #endregion

    #region Create next key methods

    /// <summary>
    /// Creates <see cref="Key"/> instance 
    /// for the specified <see cref="Entity"/> type <typeparamref name="T"/>
    /// with newly generated value.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Entity"/> descendant to get <see cref="Key"/> for.</typeparam>
    /// <returns>A newly created <see cref="Key"/> instance .</returns>
    /// <remarks>This method requires open <see cref="Session"/> instance.</remarks>
    public static Key Create<T>()
      where T : Entity
    {
      return Create(typeof (T));
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance 
    /// for the specified <see cref="Entity"/> <paramref name="type"/>
    /// with newly generated value.
    /// </summary>
    /// <returns>A newly created <see cref="Key"/> instance .</returns>
    /// <remarks>This method requires open <see cref="Session"/> instance.</remarks>
    public static Key Create(Type type)
    {
      return Create(Domain.Demand(), type);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance
    /// for the specified <see cref="Entity"/> type <typeparamref name="T"/>
    /// with newly generated value.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Entity"/> descendant to get <see cref="Key"/> for.</typeparam>
    /// <param name="domain">The domain.</param>
    /// <returns>
    /// A newly created <see cref="Key"/> instance .
    /// </returns>
    public static Key Create<T>(Domain domain)
      where T : Entity
    {
      return Create(domain, typeof (T));
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance
    /// for the specified <see cref="Entity"/> <paramref name="type"/>
    /// with newly generated value.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <param name="type">The type.</param>
    /// <returns>
    /// A newly created <see cref="Key"/> instance .
    /// </returns>
    public static Key Create(Domain domain, Type type)
    {
      ArgumentValidator.EnsureArgumentNotNull(domain, "domain");
      ArgumentValidator.EnsureArgumentNotNull(type, "type");

      return KeyFactory.Generate(domain, domain.Model.Types[type]);
    }

    #endregion

    #region Create key by tuple methods

    /// <summary>
    /// Creates <see cref="Key"/> instance
    /// for the specified <see cref="Entity"/> type <typeparamref name="T"/>
    /// and with specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Entity"/> descendant to get <see cref="Key"/> for.</typeparam>
    /// <param name="value">Key value.</param>
    /// <returns>
    /// A newly created or existing <see cref="Key"/> instance.
    /// </returns>
    /// <remarks>This method requires open <see cref="Session"/> instance.</remarks>
    public static Key Create<T>(Tuple value)
      where T : IEntity  
    {
      return Create(typeof(T), value);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance 
    /// for the specified <see cref="Entity"/> <paramref name="type"/>
    /// and with specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">Key value.</param>
    /// <returns>A newly created or existing <see cref="Key"/> instance .</returns>
    /// <remarks>This method requires open <see cref="Session"/> instance.</remarks>
    public static Key Create(Type type, Tuple value)
    {
      return Create(Domain.Demand(), type, value);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance
    /// for the specified <see cref="Entity"/> type <typeparamref name="T"/>
    /// and with specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Entity"/> descendant to get <see cref="Key"/> for.</typeparam>
    /// <param name="value">Key value.</param>
    /// <returns>
    /// A newly created or existing <see cref="Key"/> instance.
    /// </returns>
    public static Key Create<T>(Domain domain, Tuple value)
      where T : IEntity  
    {
      return Create(domain, typeof(T), value);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance 
    /// for the specified <see cref="Entity"/> <paramref name="type"/>
    /// and with specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">Key value.</param>
    /// <returns>A newly created or existing <see cref="Key"/> instance .</returns>
    public static Key Create(Domain domain, Type type, Tuple value)
    {
      ArgumentValidator.EnsureArgumentNotNull(domain, "domain");
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      ArgumentValidator.EnsureArgumentNotNull(value, "value");

      return Create(domain, domain.Model.Types[type], TypeReferenceAccuracy.BaseType, value);
    }

    internal static Key Create(Domain domain, TypeInfo typeInfo, TypeReferenceAccuracy accuracy, Tuple value)
    {
      return KeyFactory.Materialize(domain, typeInfo, value, accuracy, false, null);
    }

    #endregion

    #region Create key by params object[] methods

    /// <summary>
    /// Creates <see cref="Key"/> instance
    /// for the specified <see cref="Entity"/> type <typeparamref name="T"/>
    /// and with specified <paramref name="values"/>.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Entity"/> descendant to get <see cref="Key"/> for.</typeparam>
    /// <param name="values">Key values.</param>
    /// <returns>
    /// A newly created or existing <see cref="Key"/> instance.
    /// </returns>
    /// <remarks>This method requires open <see cref="Session"/> instance.</remarks>
    public static Key Create<T>(params object[] values)
      where T : IEntity
    {
      return Create(typeof(T), values);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance 
    /// for the specified <see cref="Entity"/> <paramref name="type"/>
    /// and with specified <paramref name="values"/>.
    /// </summary>
    /// <param name="values">Key values.</param>
    /// <returns>A newly created or existing <see cref="Key"/> instance .</returns>
    /// <remarks>This method requires open <see cref="Session"/> instance.</remarks>
    public static Key Create(Type type, params object[] values)
    {
      return Create(Domain.Demand(), type, values);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance
    /// for the specified <see cref="Entity"/> type <typeparamref name="T"/>
    /// and with specified <paramref name="values"/>.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Entity"/> descendant to get <see cref="Key"/> for.</typeparam>
    /// <param name="values">Key values.</param>
    /// <returns>
    /// A newly created or existing <see cref="Key"/> instance.
    /// </returns>
    public static Key Create<T>(Domain domain, params object[] values)
      where T : IEntity
    {
      return Create(domain, typeof(T), values);
    }

    /// <summary>
    /// Creates <see cref="Key"/> instance 
    /// for the specified <see cref="Entity"/> <paramref name="type"/>
    /// and with specified <paramref name="values"/>.
    /// </summary>
    /// <param name="values">Key values.</param>
    /// <returns>A newly created or existing <see cref="Key"/> instance .</returns>
    public static Key Create(Domain domain, Type type, params object[] values)
    {
      ArgumentValidator.EnsureArgumentNotNull(domain, "domain");
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      ArgumentValidator.EnsureArgumentNotNull(values, "values");

      return Create(domain, domain.Model.Types[type], TypeReferenceAccuracy.BaseType, values);
    }

    internal static Key Create(Domain domain, TypeInfo typeInfo, TypeReferenceAccuracy accuracy, params object[] values)
    {
      return KeyFactory.Materialize(domain, typeInfo, accuracy, values);
    }

    #endregion

    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="accuracy">The typre reference accuracy.</param>
    /// <param name="value">The value.</param>
    protected Key(TypeInfo type, TypeReferenceAccuracy accuracy, Tuple value)
    {
      TypeReference = new TypeReference(type, accuracy);
      this.value = value;
    }
  }
}