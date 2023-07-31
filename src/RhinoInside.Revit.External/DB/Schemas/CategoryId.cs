using System;
using System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.BuiltInCategory
  /// </summary>
  public partial class CategoryId : DataType
  {
    static readonly CategoryId empty = new CategoryId();
    public static new CategoryId Empty => empty;

    public string LocalizedLabel
    {
      get
      {
        if (IsNullOrEmpty(this)) return string.Empty;

#if REVIT_2020
        return Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.BuiltInCategory) this);
#else
        return Label;
#endif
      }
    }

    public CategoryId() { }
    public CategoryId(string id) : base(id)
    {
      if (!IsCategoryId(id))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsCategoryId(string id)
    {
      return id == string.Empty || // '<None>'
             id.StartsWith("autodesk.revit.category");
    }

    public static bool IsCategoryId(DataType value, out CategoryId categoryId)
    {
      var typeId = value.TypeId;
      if (IsCategoryId(typeId))
      {
        categoryId = new CategoryId(typeId);
        return true;
      }

      categoryId = default;
      return false;
    }

#if REVIT_2021
    public static implicit operator CategoryId(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new CategoryId(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(CategoryId value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

    public static implicit operator CategoryId(Autodesk.Revit.DB.BuiltInCategory value)
    {
      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.BuiltInCategory(CategoryId value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.BuiltInCategory) ut;

      return Autodesk.Revit.DB.BuiltInCategory.INVALID;
    }
  }
}
