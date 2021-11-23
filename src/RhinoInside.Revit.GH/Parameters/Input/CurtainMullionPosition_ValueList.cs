using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class CurtainMullionPosition_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("3DF76236-A44B-4BDB-89D0-7C2D6024962D");
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public CurtainMullionPosition_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Curtain Mullion Position";
      NickName = "CMP";
      Description = "Picker for curtain mullion position options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Parallel to Ground", ((int) External.DB.BuiltInMullionPositionId.ParallelToGround).ToString()));
      ListItems.Add(new GH_ValueListItem("Perpendicular to Face", ((int) External.DB.BuiltInMullionPositionId.PerpendicularToFace).ToString()));
    }
  }
}
