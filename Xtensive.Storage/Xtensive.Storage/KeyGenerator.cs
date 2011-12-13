// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.10

using System;
using System.Diagnostics;
using System.Linq;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Tuple = Xtensive.Core.Tuples.Tuple;
using Xtensive.Storage.Model;
using Xtensive.Storage.Providers;
using Xtensive.Storage.Resources;

namespace Xtensive.Storage
{
  ///<summary>
  /// Abstract base class for any key generator.
  ///</summary>
  public abstract class KeyGenerator
  {
    private bool isInitialized;

    /// <summary>
    /// Gets a value indicating whether this instance is initialized.
    /// </summary>
    public bool IsInitialized {
      get { return isInitialized; }
    }

    /// <summary>
    /// Gets a read-only hash set containing all the key field types supported by default.
    /// </summary>
    public static readonly ReadOnlyHashSet<Type> SupportedKeyFieldTypes = 
      new ReadOnlyHashSet<Type>(
        new [] {
          typeof(string),
          typeof(Guid),
          }.Concat(WellKnown.SupportedNumericTypes).ToHashSet()
        );

    /// <summary>
    /// Gets the <see cref="HandlerAccessor"/> providing other available handlers.
    /// </summary>
    protected HandlerAccessor Handlers { get; private set; }

    /// <summary>
    /// Gets or sets the <see cref="KeyInfo"/> instance that describes <see cref="KeyGenerator"/> object.
    /// </summary>
    public KeyInfo KeyInfo { get; private set; }

    /// <summary>
    /// Gets the sequence increment value for the underlying sequence,
    /// if this key generator requires it. 
    /// Otherwise, returns <see langword="null" />.
    /// </summary>
    /// <returns>Sequence increment value.</returns>
    public abstract long? SequenceIncrement { get; }

    /// <summary>
    /// Create the <see cref="Tuple"/> with the unique values in key sequence.
    /// </summary>
    /// <param name="temporaryKey">If set to <see langword="true"/>, a temporary key must be created.</param>
    /// <returns>Generated key;
    /// <see langword="null" />, if required key can not be generated.</returns>
    public abstract Tuple TryGenerateKey(bool temporaryKey);

    /// <summary>
    /// Create the <see cref="Tuple"/> with the unique values in key sequence.
    /// </summary>
    /// <param name="temporaryKey">If set to <see langword="true"/>, a temporary key must be created.</param>
    /// <returns>Generated key.</returns>
    /// <exception cref="NotSupportedException">Key of specified type cannot be generated by this
    /// key generator.</exception>
    public Tuple GenerateKey(bool temporaryKey)
    {
      var key = TryGenerateKey(temporaryKey);
      if (key==null)
        throw new NotSupportedException(Strings.ExKeyOfSpecifiedTypeCannotBeGeneratedByThisKeyGenerator);
      return key;
    }

    /// <summary>
    /// Determines whether the specified key is temporary.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>
    /// <see langword="true"/> if the specified key is temporary; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool IsTemporaryKey(Tuple key);

    /// <summary>
    /// Initializer.
    /// </summary>
    /// <param name="handlers">Handler accessor.</param>
    /// <param name="keyInfo">The <see cref="KeyInfo"/> instance that describes generator.</param>
    /// <exception cref="NotSupportedException">Instance is already initialized.</exception>
    protected internal virtual void Initialize(HandlerAccessor handlers, KeyInfo keyInfo)
    {
      if (isInitialized)
        throw Exceptions.AlreadyInitialized(null);
      isInitialized = true;
      ArgumentValidator.EnsureArgumentNotNull(handlers, "handlers");
      ArgumentValidator.EnsureArgumentNotNull(keyInfo, "keyInfo");
      Handlers = handlers;
      KeyInfo = keyInfo;
    }

    /// <summary>
    /// Called on background thread to fully prepare the key generator.
    /// Since this method is called optionally, it can't do any essential
    /// job. 
    /// But it can e.g. invoke some properties that needs delayed 
    /// evaluation, and so on.
    /// </summary>
    protected internal virtual void Prepare()
    {
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    protected KeyGenerator()
    {
    }
  }
}