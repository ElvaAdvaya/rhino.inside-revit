using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class CurtainGridLine : Element
  {
    public override string TypeDescription => "Represents a Revit Curtain Grid Line Element";
    protected override Type ScriptVariableType => typeof(DB.CurtainGridLine);
    public static explicit operator DB.CurtainGridLine(CurtainGridLine self) =>
      self.Document?.GetElement(self) as DB.CurtainGridLine;

    public CurtainGridLine() { }
    public CurtainGridLine(DB.CurtainGridLine gridLine) : base(gridLine) { }

    public override Rhino.Geometry.Curve Axis
    {
      get
      {
        var gridLine = (DB.CurtainGridLine) this;
        Rhino.Geometry.Curve c = gridLine?.FullCurve?.ToRhino();
        return c?.ChangeUnits(Revit.ModelUnits);
      }
    }
  }
}
