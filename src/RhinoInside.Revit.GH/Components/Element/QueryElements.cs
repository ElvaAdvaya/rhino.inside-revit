using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Extensions;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("0F7DA57E-6C05-4DD0-AABF-69E42DF38859");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    public QueryElements() : base
    (
      name: "Query Elements",
      nickname: "Elements",
      description: "Get document model elements list",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Integer>("Limit", "L", $"Max number of Elements to query for.{Environment.NewLine}For an unlimited query remove this parameter.", defaultValue: 100, GH_ParamAccess.item, relevance: ParamVisibility.Default),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Elements", "E", "Elements list", GH_ParamAccess.list, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Integer>("Count", "C", $"Elements count.{Environment.NewLine}For a more performant way of knowing how many elements this query returns remove the Elements output.", GH_ParamAccess.item, relevance: ParamVisibility.Default),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      DB.ElementFilter filter = null;
      if (!DA.GetData("Filter", ref filter))
        return;

      if (!DA.TryGetData(Params.Input, "Limit", out int? limit))
        limit = int.MaxValue;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter).WherePasses(filter);
        var elementCount = elementCollector.GetElementCount();

        var _Elements_ = Params.IndexOfOutputParam("Elements");
        if (_Elements_ >= 0)
        {
          if(elementCount > limit)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{Params.Output[_Elements_].NickName}' contains only first {limit} of {elementCount} total elements.");

          DA.SetDataList
          (
            _Elements_,
            elementCollector.Take(limit.Value).Convert(Types.Element.FromElement)
          );
        }

        DA.TrySetData
        (
          Params.Output,
          "Count",
          () => elementCount
        );
      }
    }
  }

  public class QueryViewElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("79DAEA3A-13A3-49BF-8BEB-AA28E3BE4515");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    public QueryViewElements() : base
    (
      name: "Query View Elements",
      nickname: "ViewEles",
      description: "Get elements visible in a view",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View", GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", "Category", GH_ParamAccess.list, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    static DB.ElementFilter ElementCategoriesFilter(DB.Document doc, DB.ElementId[] ids)
    {
      var list = new List<DB.ElementFilter>();

      if (ids.Length == 1)    list.Add(new DB.ElementCategoryFilter(ids[0]));
      else if(ids.Length > 1) list.Add(new DB.ElementMulticategoryFilter(ids));

      if (doc.IsFamilyDocument)
      {
        foreach (var id in ids)
        {
          using (var provider = new DB.ParameterValueProvider(new DB.ElementId(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY)))
          using (var evaluator = new DB.FilterNumericEquals())
          using (var rule = new DB.FilterElementIdRule(provider, evaluator, id))
            list.Add(new DB.ElementParameterFilter(rule));
        }
      }

      if (list.Count == 0)
      {
        var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
        return new DB.LogicalAndFilter(nothing);
      }
      else if (list.Count == 1)
      {
        return list[0];
      }
      else
      {
        return new DB.LogicalOrFilter(list);
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(Types.View);
      if (!DA.GetData("View", ref view) || view?.IsValid != true)
        return;

      var Categories = new List<Types.Category>();
      var _Categories_ = Params.IndexOfInputParam("Categories");
      var noFilterCategories = Params.Input[_Categories_].DataType == GH_ParamData.@void;
      if (!noFilterCategories)
        DA.GetDataList(_Categories_, Categories);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(view.Document, view.Id))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (!noFilterCategories)
        {
          var ids = Categories.
            Where(x => x.IsValid && x.Document.Equals(view.Document)).
            Select(x => x.Id).ToArray();

          elementCollector = elementCollector.WherePasses(ElementCategoriesFilter(view.Document, ids));
        }

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          Where(x => Types.GraphicalElement.IsValidElement(x)).
          Convert(Types.Element.FromElement)
        );
      }
    }
  }
}
