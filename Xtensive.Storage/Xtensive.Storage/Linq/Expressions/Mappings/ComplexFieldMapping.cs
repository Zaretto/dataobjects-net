// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.04.06

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Linq.Expressions.Mappings
{
  internal sealed class ComplexFieldMapping : FieldMapping
  {
    internal readonly Dictionary<string, Pair<ComplexFieldMapping, Expression>> AnonymousFields;
    internal readonly Dictionary<string, Segment<int>> Fields;
    internal readonly Dictionary<string, ComplexFieldMapping> JoinedFields;
    private  readonly List<int> columns = new List<int>();

    #region Accessor methods

    public Segment<int> GetFieldSegment(string fieldName)
    {
      Segment<int> result;
      if (!Fields.TryGetValue(fieldName, out result))
        throw new InvalidOperationException(string.Format("Could not find field segment for field '{0}'.", fieldName));
      return result;
    }

    public bool TryGetJoined(string fieldName, out ComplexFieldMapping value)
    {
      return JoinedFields.TryGetValue(fieldName, out value);
    }

    public ComplexFieldMapping GetJoinedFieldMapping(string fieldName)
    {
      ComplexFieldMapping result;
      if (!JoinedFields.TryGetValue(fieldName, out result))
        throw new InvalidOperationException(string.Format("Could not find joined field mapping for field '{0}'.", fieldName));
      return result;
    }

    public Pair<ComplexFieldMapping, Expression> GetAnonymousMapping(string fieldName)
    {
      Pair<ComplexFieldMapping, Expression> result;
      if (!AnonymousFields.TryGetValue(fieldName, out result))
        throw new InvalidOperationException(string.Format("Could not find anonymous projection for field '{0}'.", fieldName));
      return result;
    }

    #endregion

    public override IList<int> GetColumns()
    {
      return columns.Distinct().ToList();
    }

    public override FieldMapping ShiftOffset(int offset)
    {
      var shiftedFields = Fields.ToDictionary(fm => fm.Key, fm => new Segment<int>(offset + fm.Value.Offset, fm.Value.Length));
      var shiftedRelations = JoinedFields.ToDictionary(jr => jr.Key, jr => (ComplexFieldMapping)jr.Value.ShiftOffset(offset));
      var shiftedAnonymous = new Dictionary<string, Pair<ComplexFieldMapping, Expression>>();
      foreach (var pair in AnonymousFields) {
        var mapping = pair.Value.First.ShiftOffset(offset);
        // TODO: rewrite tuple access
        var expression = pair.Value.Second;
        shiftedAnonymous.Add(pair.Key, new Pair<ComplexFieldMapping, Expression>((ComplexFieldMapping)mapping, expression));
      }
      return new ComplexFieldMapping(shiftedFields, shiftedRelations, shiftedAnonymous);
    }

    public override Segment<int> GetMemberSegment(MemberPath fieldPath)
    {
      if (fieldPath.Count == 0) {
        if (fieldPath.PathType == MemberType.Structure || fieldPath.PathType == MemberType.Entity)
          return CalculateMemberSegment();
        throw new InvalidOperationException();
      }
      List<MemberPathItem> pathList = fieldPath.ToList();
      ComplexFieldMapping mapping = this;
      for (int i = 0; i < pathList.Count - 1; i++) {
        MemberPathItem pathItem = pathList[i];
        if (pathItem.Type == MemberType.Entity)
          mapping = mapping.GetJoinedFieldMapping(pathItem.Name);
        else if (pathItem.Type == MemberType.Anonymous)
          mapping = mapping.GetAnonymousMapping(pathItem.Name).First;
      }
      MemberPathItem lastItem = pathList.Last();
      if (lastItem.Type == MemberType.Anonymous)
        throw new InvalidOperationException();

      if (lastItem.Type == MemberType.Entity) {
        mapping = mapping.GetJoinedFieldMapping(lastItem.Name);
        return mapping.CalculateMemberSegment();
      }
      return mapping.GetFieldSegment(lastItem.Name);
    }

    public override FieldMapping GetMemberMapping(MemberPath fieldPath)
    {
      List<MemberPathItem> pathList = fieldPath.ToList();
      if (pathList.Count == 0)
        return this;
      ComplexFieldMapping mapping = this;
      foreach (MemberPathItem pathItem in pathList) {
        if (pathItem.Type == MemberType.Entity)
          mapping = mapping.GetJoinedFieldMapping(pathItem.Name);
        else if (pathItem.Type == MemberType.Anonymous)
          mapping = mapping.GetAnonymousMapping(pathItem.Name).First;
      }
      return mapping;
    }

    public override void Fill(FieldMapping fieldMapping)
    {
      if (fieldMapping is PrimitiveFieldMapping) {
        var pfm = (PrimitiveFieldMapping)fieldMapping;
        RegisterFieldMapping(string.Empty, pfm.Segment);
      }
      else {
        var cfm = (ComplexFieldMapping)fieldMapping;
        foreach (var pair in cfm.Fields)
          RegisterFieldMapping(pair.Key, pair.Value);
        foreach (var pair in cfm.JoinedFields)
          RegisterJoin(pair.Key, pair.Value);
        foreach (var pair in cfm.AnonymousFields)
          RegisterAnonymous(pair.Key, pair.Value.First, pair.Value.Second);
      }
    }

    public override string ToString()
    {
      return string.Format("Complex: Fields({0}), JoinedFields({1}), AnonymousFields({2})",
        Fields.Count, JoinedFields.Count, AnonymousFields.Count);
    }

    private Segment<int> CalculateMemberSegment()
    {
      int offset = Fields.Min(pair => pair.Value.Offset);
      int endOffset = Fields.Max(pair => pair.Value.Offset);
      int length = endOffset - offset + 1;
      return new Segment<int>(offset, length);
    }

    #region Register methods

    public void RegisterFieldMapping(string key, Segment<int> value)
    {
      if (!Fields.ContainsKey(key)) {
        Fields.Add(key, value);
        columns.AddRange(value.GetItems());
      }
    }

    public void RegisterJoin(string key, ComplexFieldMapping value)
    {
      if (!JoinedFields.ContainsKey(key))
        JoinedFields.Add(key, value);
    }

    public void RegisterAnonymous(string key, ComplexFieldMapping anonymousMapping, Expression projection)
    {
      if (!AnonymousFields.ContainsKey(key))
        AnonymousFields.Add(key, new Pair<ComplexFieldMapping, Expression>(anonymousMapping, projection));
    }

    #endregion


    // Constructors

    public ComplexFieldMapping()
      : this(new Dictionary<string, Segment<int>>(), new Dictionary<string, ComplexFieldMapping>(), new Dictionary<string, Pair<ComplexFieldMapping, Expression>>())
    {}

    public ComplexFieldMapping(TypeInfo type, int offset)
      : this (null, new Dictionary<string, ComplexFieldMapping>(), new Dictionary<string, Pair<ComplexFieldMapping, Expression>>())
    {
      Fields = new Dictionary<string, Segment<int>>();
      foreach (var field in type.Fields) {
        Fields.Add(field.Name, new Segment<int>(offset + field.MappingInfo.Offset, field.MappingInfo.Length));
        if (field.IsEntity)
          Fields.Add(field.Name + ".Key", new Segment<int>(offset + field.MappingInfo.Offset, field.MappingInfo.Length));
      }
      var keySegment = new Segment<int>(offset, type.Hierarchy.KeyInfo.Fields.Sum(pair => pair.Key.MappingInfo.Length));
      Fields.Add("Key", keySegment);

      columns.AddRange(Enumerable.Range(offset, type.Columns.Count));
    }

    private ComplexFieldMapping(Dictionary<string, Segment<int>> fields, Dictionary<string, ComplexFieldMapping> joinedFields, Dictionary<string, Pair<ComplexFieldMapping, Expression>> anonymousFields)
    {
      Fields = fields;
      JoinedFields = joinedFields;
      AnonymousFields = anonymousFields;
    }

  }
}