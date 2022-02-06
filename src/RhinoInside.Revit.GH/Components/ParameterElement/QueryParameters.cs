using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  [ComponentVersion(introduced: "1.0", updated: "1.4")]
  public class QueryParameters : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("D82D9FC3-FC74-4C54-AAE1-CB4D806741DB");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.ParameterElement));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var doc = activeApp.ActiveUIDocument?.Document;
      if (doc is null) return;

      var commandId = doc.IsFamilyDocument ?
        Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.FamilyTypes) :
        Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ProjectParameters);

      var commandName = doc.IsFamilyDocument ?
        "Open Family Parameters…" :
        "Open Project Parameters…";

      Menu_AppendItem
      (
        menu, commandName,
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryParameters() : base
    (
      name: "Query Parameters",
      nickname: "Parameters",
      description: "Get document parameters list",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ParameterScope>>("Scope", "S", "Parameter scope", optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Parameter name", optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ParameterType>>("Type", "T", "Parameter type", optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ParameterGroup>>("Group", "G", "Parameter group", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ParameterKey>("Parameter", "K", "Parameters list", GH_ParamAccess.list)
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Input<IGH_Param>("Binding") is IGH_Param binding)
        binding.Name = "Scope";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Scope", out Types.ParameterScope scope, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => x is object)) return;
      if (!Params.TryGetData(DA, "Type", out Types.ParameterType type, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Group", out Types.ParameterGroup group, x => x.IsValid)) return;

      var parameters = doc.GetParameterDefinitions
      (
        scope is object ? scope.Value :
        ERDB.ParameterScope.Instance | ERDB.ParameterScope.Type | ERDB.ParameterScope.Global
      ).Select(x => x.Definition);

      if (!string.IsNullOrEmpty(name))
        parameters = parameters.Where(x => x.Name.IsSymbolNameLike(name));

      if (type is object)
        parameters = parameters.Where(x => (DBXS.DataType) x.GetDataType() == type.Value);

      if (group is object)
        parameters = parameters.Where(x => x.GetGroupType() == group.Value);

      // As any other Query component this should return elements sorted by Id.
      parameters = parameters.OrderBy(x => x.Id.IntegerValue);

      DA.SetDataList
      (
        "Parameter",
        parameters.
        Select(x => new Types.ParameterKey(doc, x)).
        TakeWhileIsNotEscapeKeyDown(this)
      );
    }
  }
}
