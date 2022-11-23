using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  [ComponentVersion(introduced: "1.2")]
  public class QueryWorksets : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("311316BA-81C7-495C-8A20-B7974091D6B1");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "Q";

    public override bool NeedsToBeExpired
    (
      ARDB.Document document,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
    ) => false;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Worksets);
      Menu_AppendItem
      (
        menu, $"Open Worksets…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryWorksets() : base
    (
      name: "Query Worksets",
      nickname: "Worksets",
      description: "Get document construction worksets list",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.WorksetKind>>
        ("Kind", "K", "Workset kind", defaultValue: ARDB.WorksetKind.UserWorkset, optional: true),
      ParamDefinition.Create<Param_String>
        ("Name", "N", "Workset name", optional: true),
    };
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Workset>("Worksets", "W", "Worksets list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Kind", out Types.WorksetKind kind)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;

      using (var collector = new ARDB.FilteredWorksetCollector(doc))
      {
        var worksetCollector = collector;

        if (kind is object)
          worksetCollector = worksetCollector.OfKind(kind.Value);

        var worksets = worksetCollector.Cast<ARDB.Workset>();

        if (name is object)
          worksets = worksets.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Worksets",
          worksets.Select(x => new Types.Workset(doc, x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
