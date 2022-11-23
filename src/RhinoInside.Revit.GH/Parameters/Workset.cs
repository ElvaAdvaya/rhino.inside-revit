using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.2")]
  public class Workset : PersistentParam<Types.Workset>
  {
    public override Guid ComponentGuid => new Guid("5C073F7D-6D31-4063-A943-4152E1A799D1");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    public Workset() : base("Workset", "Workset", "Contains a collection of Revit workset elements", "Params", "Revit") { }

    protected override Types.Workset PreferredCast(object data)
    {
      return data is ValueTuple<ARDB.Document, ARDB.Workset> workset ?
             new Types.Workset(workset.Item1, workset.Item2) :
             null;
    }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Worksets);
      Menu_AppendItem
      (
        menu, $"Open Worksets…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }

    protected override GH_GetterResult Prompt_Singular(ref Types.Workset value)
    {
      return GH_GetterResult.cancel;
    }

    protected override GH_GetterResult Prompt_Plural(ref List<Types.Workset> values)
    {
      return GH_GetterResult.cancel;
    }
    #endregion
  }
}
