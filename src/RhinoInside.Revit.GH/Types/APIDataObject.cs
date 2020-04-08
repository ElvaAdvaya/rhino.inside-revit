using System;

using Grasshopper.Kernel.Types;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class APIDataObject : GH_Goo<object>
  {
    public override bool IsValid => Value != null;
    public override string TypeName => "Revit API Data Object";
    public override string TypeDescription => "Wraps Revit API Data Objects";

    public DB.Document Document { get; private set; } = default;

    public APIDataObject(object apiObject, DB.Document srcDocument): base(apiObject) {
      Document = srcDocument;
    }

    public override IGH_Goo Duplicate()
    {
      throw new NotImplementedException();
    }

    public override string ToString() => $"Revit API Data Object: {Value.GetType().Name}";
  }
}
