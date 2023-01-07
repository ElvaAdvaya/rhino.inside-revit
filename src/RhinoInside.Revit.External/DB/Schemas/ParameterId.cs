using System;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.BuiltInParameter
  /// </summary>
  public partial class ParameterId : DataType
  {
    static readonly ParameterId empty = new ParameterId();
    public static new ParameterId Empty => empty;

    public string LocalizedLabel =>
#if REVIT_2022
      Autodesk.Revit.DB.LabelUtils.GetLabelForBuiltInParameter(this);
#else
      Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.BuiltInParameter) this);
#endif


    public ParameterId() { }
    public ParameterId(string id) : base(id)
    {
      if (!IsParameterId(id))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsParameterId(string id)
    {
      return id.StartsWith("autodesk.parameter.aec") || id.StartsWith("autodesk.revit.parameter");
    }

    public static bool IsParameterId(DataType value, out ParameterId parameterId)
    {
      var typeId = value.TypeId;
      if (IsParameterId(typeId))
      {
        parameterId = new ParameterId(typeId);
        return true;
      }

      parameterId = default;
      return false;
    }

#if REVIT_2021
    public static implicit operator ParameterId(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new ParameterId(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(ParameterId value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

    public static implicit operator ParameterId(Autodesk.Revit.DB.BuiltInParameter value)
    {
      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.BuiltInParameter(ParameterId value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.BuiltInParameter) ut;

      return Autodesk.Revit.DB.BuiltInParameter.INVALID;
    }
  }
}
