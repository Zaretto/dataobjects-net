// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.03.17

using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Helpers;
using System.Linq;
using Xtensive.Core.Threading;

namespace Xtensive.Core.Security
{
  /// <summary>
  /// An abstract base class for hashing signature providers.
  /// </summary>
  [Serializable]
  public class HashingSignatureProvider : ISignatureProvider,
    IDeserializationCallback
  {
    [NonSerialized]
    private ThreadSafeCached<HashAlgorithm> cachedHasher;
    //[NonSerialized]
    private Encoding encoding;
    [NonSerialized]
    private object _lock = new object();

    #region Properties

    /// <summary>
    /// Gets or sets the hasher constructor delegate.
    /// </summary>
    protected Func<HashAlgorithm> HasherConstructor { get; set; }
    
    /// <summary>
    /// Gets the hasher.
    /// </summary>
    protected HashAlgorithm Hasher {
      get {
        return cachedHasher.GetValue(HasherConstructor);
      }
    }

    /// <summary>
    /// Gets or sets the encoding.
    /// </summary>
    protected Encoding Encoding
    {
      get { return encoding; }
      set { encoding = value; }
    }

    /// <summary>
    /// Gets or sets the escape character.
    /// </summary>
    public char Escape { get; set; }

    /// <summary>
    /// Gets or sets the delimiter character.
    /// </summary>
    public char Delimiter { get; set; }

    #endregion

    /// <inheritdoc/>
    public string AddSignature(string token)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(token, "token");
      byte[] byteToken = encoding.GetBytes(token);
      byte[] byteSignature;
      lock (_lock) {
        byteSignature = Hasher.ComputeHash(byteToken);
      }
      return
        new[]
          {
            encoding.GetString(byteToken),
            encoding.GetString(byteSignature)
          }.RevertibleJoin(Escape, Delimiter);
    }

    /// <inheritdoc/>
    public string RemoveSignature(string signedToken)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(signedToken, "signedToken");
      string[] parts = signedToken.RevertibleSplit(Escape, Delimiter).ToArray();
      if (parts.Length!=2)
        return null;
      string token = parts[0];
      string signature = parts[1];
      byte[] byteToken = encoding.GetBytes(token);
      byte[] byteSignature;
      lock (_lock) {
        byteSignature = Hasher.ComputeHash(byteToken);
      }
      if (encoding.GetString(byteSignature)!=signature)
        return null;
      return token;
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="hasherConstructor">The <see cref="Hasher"/> constructor delegate.</param>
    /// <param name="encoding">The encoding.</param>
    public HashingSignatureProvider(Func<HashAlgorithm> hasherConstructor, Encoding encoding)
    {
      HasherConstructor = hasherConstructor;
      Encoding = encoding;
      Escape = '\\';
      Delimiter = ',';
      cachedHasher = ThreadSafeCached<HashAlgorithm>.Create(_lock);
    }

    // Deserialization

    /// <inheritdoc/>
    public void OnDeserialization(object sender)
    {
      _lock = new object();
      cachedHasher = ThreadSafeCached<HashAlgorithm>.Create(_lock);
    }
  }
}