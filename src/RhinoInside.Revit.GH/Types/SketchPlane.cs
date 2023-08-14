using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Work Plane")]
  public class SketchPlane : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.SketchPlane);
    public new ARDB.SketchPlane Value => base.Value as ARDB.SketchPlane;

    public SketchPlane() : base() { }
    public SketchPlane(ARDB.SketchPlane sketchPlane) : base(sketchPlane) { }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is ARDB.View view)
        return SetValue(view.SketchPlane);

      if (value is ARDB.CurveElement curveElement)
        return SetValue(curveElement.SketchPlane);

      if (value is ARDB.DatumPlane datum)
        return SetValue(datum.GetSketchPlane());

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var location = Location;
      if (location.IsValid)
      {
        var radius = Grasshopper.CentralSettings.PreviewPlaneRadius;
        var origin = location.Origin;
        origin.Transform(xform);
        return new BoundingBox
        (
          origin - new Vector3d(radius, radius, radius),
          origin + new Vector3d(radius, radius, radius)
        );
      }

      return NaN.BoundingBox;
    }

    #region IGH_PreviewData
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = GetBoundingBox(Transform.Identity);
      return !clippingBox.IsValid;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (!location.IsValid)
        return;

      GH_Plane.DrawPlane(args.Pipeline, location, Grasshopper.CentralSettings.PreviewPlaneRadius, 4, args.Color, System.Drawing.Color.DarkRed, System.Drawing.Color.DarkGreen);
    }
    #endregion

    #region Location
    public override Plane Location => Value?.GetPlane().ToPlane() ?? base.Location;
    #endregion
  }
}
