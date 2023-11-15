using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.2")]
  public class QuerySheets : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("97c8cb27-955f-44cf-948d-dfbde285cd7a");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.ViewSheet));

    public QuerySheets() : base
    (
      name: "Query Sheets",
      nickname: "Sheets",
      description: "Get all document sheets",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Placeholder", "PH", "Sheet is placeholder", false, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Sheet Number", "NUM", "Sheet number", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Sheet Name", "N", "Sheet name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Sheet Issue Date", "ID", "Sheet issue date", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Appears In Sheet List", "AISL", "Sheet appears on sheet lists", true, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.AssemblyInstance>("Assembly", "A", "Assembly the view belongs to", new Types.AssemblyInstance(), GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheets", "S", "Sheets list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      bool IsPlaceholder = false;
      var _IsPlaceholder_ = Params.IndexOfInputParam("Placeholder");
      bool nofilterIsPlaceholder = (!DA.GetData(_IsPlaceholder_, ref IsPlaceholder) && Params.Input[_IsPlaceholder_].DataType == GH_ParamData.@void);

      string number = null;
      DA.GetData("Sheet Number", ref number);

      string name = null;
      DA.GetData("Sheet Name", ref name);

      string date = null;
      DA.GetData("Sheet Issue Date", ref date);

      bool IsScheduled = false;
      var _IsScheduled_ = Params.IndexOfInputParam("Appears In Sheet List");
      bool nofilterIsScheduled = (!DA.GetData(_IsScheduled_, ref IsScheduled) && Params.Input[_IsScheduled_].DataType == GH_ParamData.@void);

      var Assembly = default(Types.AssemblyInstance);
      var _Assembly_ = Params.IndexOfInputParam("Assembly");
      bool noFilterAssembly = (!DA.GetData(_Assembly_, ref Assembly) && Params.Input[_Assembly_].DataType == GH_ParamData.@void);

      ARDB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var sheetsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          sheetsCollector = sheetsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.SHEET_NUMBER, ref number, out var sheetNumberFilter))
          sheetsCollector = sheetsCollector.WherePasses(sheetNumberFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.SHEET_NAME, ref name, out var sheetNameFilter))
          sheetsCollector = sheetsCollector.WherePasses(sheetNameFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.SHEET_ISSUE_DATE, ref date, out var sheetIssueDateFilter))
          sheetsCollector = sheetsCollector.WherePasses(sheetIssueDateFilter);

        if (!nofilterIsScheduled)
          sheetsCollector = sheetsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.SHEET_SCHEDULED, IsScheduled ? 1 : 0);

        if (!noFilterAssembly && TryGetFilterElementIdParam(ARDB.BuiltInParameter.VIEW_ASSOCIATED_ASSEMBLY_INSTANCE_ID, Assembly?.Id ?? ARDB.ElementId.InvalidElementId, out var assemblyFilter))
          sheetsCollector = sheetsCollector.WherePasses(assemblyFilter);

        var sheets = sheetsCollector.Cast<ARDB.ViewSheet>();

        if (!nofilterIsPlaceholder)
          sheets = sheets.Where((x) => x.IsPlaceholder == IsPlaceholder);

        if (!string.IsNullOrEmpty(number))
          sheets = sheets.Where(x => x.SheetNumber.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          sheets = sheets.Where(x => x.Name.IsSymbolNameLike(name));

        if (!string.IsNullOrEmpty(date))
          sheets = sheets.Where(x => x.get_Parameter(ARDB.BuiltInParameter.SHEET_ISSUE_DATE).AsString().IsSymbolNameLike(date));

        DA.SetDataList
        (
          "Sheets",
          sheets.
          Select(x => new Types.ViewSheet(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
