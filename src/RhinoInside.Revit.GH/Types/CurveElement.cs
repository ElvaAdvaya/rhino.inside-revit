using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Curve Element")]
  public class CurveElement : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.CurveElement);
    public static explicit operator DB.CurveElement(CurveElement value) => value?.Value;
    public new DB.CurveElement Value => base.Value as DB.CurveElement;

    public CurveElement() { }
    public CurveElement(DB.CurveElement value) : base(value) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Curve is Curve curve)
        return curve.GetBoundingBox(xform);

      return base.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }
    #endregion

    #region Properties
    public override Curve Curve => Value?.GeometryCurve.ToCurve();
    #endregion
  }
}
