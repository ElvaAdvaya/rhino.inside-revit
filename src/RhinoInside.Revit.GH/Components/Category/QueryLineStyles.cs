using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ObjectStyles
{
  public class QueryLineStyles : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("54082395-7160-4563-B289-215AFDD33A7F");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.GraphicsStyle));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.LineStyles);
      Menu_AppendItem
      (
        menu, $"Open Line Styles…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryLineStyles() : base
    (
      name: "Query Line Styles",
      nickname: "Line Styles",
      description: "Get document line styles list",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition (new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Line style name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicsStyle>("Styles", "S", "Line styles list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      string name = null;
      DA.GetData("Name", ref name);

      Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter);

      using (var categories = doc.Settings.Categories)
      {
        var styles = categories.
          get_Item(ARDB.BuiltInCategory.OST_Lines).SubCategories.Cast<ARDB.Category>().
          Select(x => x.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection));

        if (filter is object)
          styles = styles.Where(x => filter.PassesFilter(x));

        if (name is object)
          styles = styles.Where(x => x.Name == name);

        DA.SetDataList
        (
          "Styles",
          styles.
          Select(x => new Types.GraphicsStyle(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
