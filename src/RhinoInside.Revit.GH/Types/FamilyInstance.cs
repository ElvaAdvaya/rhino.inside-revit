using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Component")]
  public interface IGH_FamilyInstance : IGH_InstanceElement { }

  [Kernel.Attributes.Name("Component")]
  public class FamilyInstance : InstanceElement, IGH_FamilyInstance, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.FamilyInstance);
    public static explicit operator DB.FamilyInstance(FamilyInstance value) => value?.Value;
    public new DB.FamilyInstance Value => base.Value as DB.FamilyInstance;

    public FamilyInstance() { }
    public FamilyInstance(DB.FamilyInstance value) : base(value) { }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public new bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is DB.FamilyInstance element)
      {
        using (var options = new DB.Options() { DetailLevel = DB.ViewDetailLevel.Fine })
        {
          using (var context = GeometryDecoder.Context.Push())
          {
            context.Element = element;
            context.Category = element.Category;
            context.Material = element.Category?.Material;

            using (var geometry = element.GetGeometry(options))
            {
              if (geometry is DB.GeometryElement geometryElement)
              {
                var transform = element.GetTransform();
                var location = new Plane(transform.Origin.ToPoint3d(), transform.BasisX.ToVector3d(), transform.BasisY.ToVector3d());
                var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);

                if (BakeGeometryElement(idMap, overwrite, doc, att, worldToElement, element, geometry, out var idefIndex))
                {
                  att = att.Duplicate();
                  att.Name = element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                  att.Url = element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;

                  var category = Category;
                  if (category is object && category.BakeElement(idMap, false, doc, att, out var layerGuid))
                    att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

                  guid = doc.Objects.AddInstanceObject(idefIndex, Transform.PlaneToPlane(Plane.WorldXY, location), att);
                }
              }

              if (guid != Guid.Empty)
              {
                idMap.Add(Id, guid);
                return true;
              }
            }
          }
        }
      }

      return false;
    }
    #endregion

    #region Location
    public override Level Level
    {
      get
      {
        if (Value is DB.FamilyInstance instance)
        {
          var levelId = instance.LevelId;
          if (levelId == DB.ElementId.InvalidElementId)
          {
            var levelParam = instance.get_Parameter(DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            if (levelParam is null)
              levelParam = instance.get_Parameter(DB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
            if (levelParam is object)
              levelId = levelParam.AsElementId();
          }

          return new Level(instance.Document, levelId);
        }

        return default;
      }
    }

    public override Plane Location
    {
      get
      {
        if (Value is DB.FamilyInstance instance)
        {
          instance.GetLocation(out var origin, out var basisX, out var basisY);
          var baseLocation = new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d());

          if (Value?.Mirrored == true)
          {
            baseLocation.XAxis = -baseLocation.XAxis;
            baseLocation.YAxis = -baseLocation.YAxis;
          }

          return baseLocation;
        }

        return base.Location;
      }
    }

    public override Vector3d FacingOrientation
    {
      get
      {
        if (Value?.CanFlipFacing == true)
          return Value.FacingOrientation.ToVector3d();

        return base.FacingOrientation;
      }
    }

    public override Vector3d HandOrientation
    {
      get
      {
        if (Value?.CanFlipHand == true)
          return Value.HandOrientation.ToVector3d();

        return base.HandOrientation;
      }
    }

    public override Curve Curve
    {
      get
      {
        if(Value is DB.FamilyInstance instance && instance.Location is DB.LocationPoint location)
        {
          if (instance.Symbol.Family.FamilyPlacementType == DB.FamilyPlacementType.TwoLevelsBased)
          {
            var baseLevel = instance.GetParameterValue<DB.Level>(DB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            var topLevel = instance.GetParameterValue<DB.Level>(DB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

            var baseLevelOffset = instance.GetParameterValue<double>(DB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
            var topLevelOffset = instance.GetParameterValue<double>(DB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

            var baseElevation = (baseLevel.GetHeight() + baseLevelOffset) * Revit.ModelUnits;
            var topElevation = (topLevel.GetHeight() + topLevelOffset) * Revit.ModelUnits;

            var origin = location.Point.ToPoint3d();
            return new LineCurve
            (
              new Line
              (
                origin + Vector3d.ZAxis * baseElevation,
                origin + Vector3d.ZAxis * topElevation
              )
              ,
              baseElevation,
              topElevation
            );
          }
        }

        return base.Curve;
      }
    }
    #endregion

    #region Flip
    public override bool CanFlipFacing => Value?.CanFlipFacing ?? false;
    public override bool? FacingFlipped
    {
      get
      {
        return Value is DB.FamilyInstance instance && instance.CanFlipFacing ?
          (bool?) instance.FacingFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.FamilyInstance instance)
        {
          if (!instance.CanFlipFacing)
            throw new InvalidOperationException("Facing can not be flipped for this element.");

          if (instance.FacingFlipped != value)
          {
            InvalidateGraphics();
            instance.flipFacing();
          }
        }
      }
    }

    public override bool CanFlipHand => Value?.CanFlipHand ?? false;
    public override bool? HandFlipped
    {
      get
      {
        return Value is DB.FamilyInstance instance && instance.CanFlipHand ?
          (bool?) instance.HandFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.FamilyInstance instance)
        {
          if (!instance.CanFlipHand)
            throw new InvalidOperationException("Hand can not be flipped for this element.");

          if (instance.HandFlipped != value)
          {
            InvalidateGraphics();
            instance.flipHand();
          }
        }
      }
    }

    public override bool CanFlipWorkPlane => Value?.CanFlipWorkPlane ?? false;
    public override bool? WorkPlaneFlipped
    {
      get
      {
        return Value is DB.FamilyInstance instance && instance.CanFlipWorkPlane ?
          (bool?) instance.IsWorkPlaneFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.FamilyInstance instance)
        {
          if (!instance.CanFlipWorkPlane)
            throw new InvalidOperationException("Work Plane can not be flipped for this element.");

          if (instance.IsWorkPlaneFlipped != value)
          {
            InvalidateGraphics();
            instance.IsWorkPlaneFlipped = value.Value;
          }
        }
      }
    }
    #endregion

    #region Joins
    public override bool? IsJoinAllowedAtStart
    {
      get => Value is DB.FamilyInstance frame && frame.StructuralType != DB.Structure.StructuralType.NonStructural ?
        (bool?) DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(frame, 0) :
        default;

      set
      {
        if (value is object &&  Value is DB.FamilyInstance frame)
        {
          if (frame.StructuralType != DB.Structure.StructuralType.NonStructural)
            throw new InvalidOperationException("Join at start can not be set for this element.");

          InvalidateGraphics();

          if (value == true)
            DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(frame, 0);
          else
            DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(frame, 0);
        }
      }
    }

    public override bool? IsJoinAllowedAtEnd
    {
      get => Value is DB.FamilyInstance frame && frame.StructuralType != DB.Structure.StructuralType.NonStructural ?
        (bool?) DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(frame, 1) :
        default;

      set
      {
        if (value is object && Value is DB.FamilyInstance frame)
        {
          if (frame.StructuralType != DB.Structure.StructuralType.NonStructural)
            throw new InvalidOperationException("Join at end can not be set for this element.");

          InvalidateGraphics();

          if (value == true)
            DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(frame, 1);
          else
            DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(frame, 1);
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Component Type")]
  public interface IGH_FamilySymbol : IGH_ElementType
  {
    Family Family { get; }
  }

  [Kernel.Attributes.Name("Component Type")]
  public class FamilySymbol : ElementType, IGH_FamilySymbol, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.FamilySymbol);
    public static explicit operator DB.FamilySymbol(FamilySymbol value) => value?.Value;
    public new DB.FamilySymbol Value => base.Value as DB.FamilySymbol;

    public FamilySymbol() { }
    protected FamilySymbol(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public FamilySymbol(DB.FamilySymbol elementType) : base(elementType) { }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is DB.FamilySymbol element)
      {
        using (var options = new DB.Options() { DetailLevel = DB.ViewDetailLevel.Fine })
        {
          using (var context = GeometryDecoder.Context.Push())
          {
            context.Element = element;
            context.Category = element.Category;
            context.Material = element.Category?.Material;

            using (var geometry = element.GetGeometry(options))
            {
              if (geometry is DB.GeometryElement geometryElement)
              {
                if (GeometricElement.BakeGeometryElement(idMap, overwrite, doc, att, Transform.Identity, element, geometry, out var idefIndex))
                  guid = doc.InstanceDefinitions[idefIndex].Id;
              }

              if (guid != Guid.Empty)
              {
                idMap.Add(Id, guid);
                return true;
              }
            }
          }
        }
      }

      return false;
    }
    #endregion

    public Family Family => Value is DB.FamilySymbol symbol ? new Family(symbol.Family) : default;
  }
}
