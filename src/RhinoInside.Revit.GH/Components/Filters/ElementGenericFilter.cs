using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  using External.DB;

  [ComponentVersion(introduced: "1.0", updated: "1.3")]
  public class ElementExcludeElementTypeFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F69D485F-B262-4297-A496-93F5653F5D19");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "T";

    public ElementExcludeElementTypeFilter()
    : base("Exclude Types", "NoTypes", "Filter used to exclude element types", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", CompoundElementFilter.ElementIsElementTypeFilter(!inverted));
    }
  }

  public class ElementClassFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("6BD34014-CD73-42D8-94DB-658BE8F42254");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "C";

    public ElementClassFilter()
    : base("Class Filter", "ClassFltr", "Filter used to match elements by their API class", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddTextParameter("Classes", "C", "Classes to match", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var classNames = new List<string>();
      if (!DA.GetDataList("Classes", classNames))
        return;

      try
      {
        var types = classNames.Select(x => typeof(ARDB.Element).Assembly.GetType(x, throwOnError: true)).ToArray();
        DA.SetData("Filter", CompoundElementFilter.ElementClassFilter(types));
      }
      catch (System.TypeLoadException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message.Replace(". ", $".{Environment.NewLine}"));
      }
    }
  }

  public class ElementCategoryFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("D08F7AB1-BE36-45FA-B006-0078022DB140");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "C";

    public ElementCategoryFilter()
    : base("Category Filter", "CatFltr", "Filter used to match elements by their category", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Categories", "C", "Categories to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var categoryIds = new List<ARDB.ElementId>();
      if (!DA.GetDataList("Categories", categoryIds))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var ids = categoryIds.Where(x => x is object).ToList();
      DA.SetData("Filter", CompoundElementFilter.ElementCategoryFilter(ids, inverted));
    }
  }

  [ComponentVersion(introduced: "1.3")]
  public class ElementFamilyFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("B344F1C1-F37D-4A1A-83B3-65A34FE946D2");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "F";

    public ElementFamilyFilter()
    : base("Family Filter", "FamFltr", "Filter used to match elements by their family", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        new Parameters.Param_Enum<Types.ElementKind>()
        {
          Name = "Kind", NickName = "K",
          Description = "Kind to match",
          Optional = true
        }.
        SetDefaultVale(ElementKind.System | ElementKind.Component)
      );

      manager[manager.AddTextParameter("Family Name", "FN", "Family Name to match", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("Type Name", "TN", "Type Name to match", GH_ParamAccess.item)].Optional = true;
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Kind", out ElementKind? kind)) return;
      if (!Params.TryGetData(DA, "Family Name", out string familyName)) return;
      if (!Params.TryGetData(DA, "Type Name", out string typeName)) return;
      if (!Params.GetData(DA, "Inverted", out bool? inverted)) return;

      var filters = new List<ARDB.ElementFilter>(3);
      if (kind.HasValue)
        filters.Add(CompoundElementFilter.ElementKindFilter(kind.Value, elementType: default, inverted.Value));

      if (familyName is object)
        filters.Add(CompoundElementFilter.ElementFamilyNameFilter(familyName, inverted.Value));
      //else
      //  filters.Add(CompoundElementFilter.ElementFamilyNameFilter(string.Empty, inverted: true));

      if (typeName is object)
        filters.Add(CompoundElementFilter.ElementTypeNameFilter(typeName, inverted.Value));
      else if (familyName is null)
        filters.Add(CompoundElementFilter.ElementTypeNameFilter(string.Empty, inverted: true));

      var filter = inverted.Value ? CompoundElementFilter.Union(filters) : CompoundElementFilter.Intersect(filters);
      DA.SetData("Filter", filter);
    }
  }

  public class ElementTypeFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("4434C470-4CAF-4178-929D-284C3B5A24B5");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "T";

    public ElementTypeFilter()
    : base("Type Filter", "TypeFltr", "Filter used to match elements by their type", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "T", "Types to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var types = new List<ARDB.ElementType>();
      if (!DA.GetDataList("Types", types))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var filter = CompoundElementFilter.ElementTypeFilter(types, inverted);
      DA.SetData("Filter", filter);
    }
  }

  public class ElementParameterFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("E6A1F501-BDA4-4B78-8828-084B5EDA926F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "#";

    public ElementParameterFilter()
    : base("Parameter Filter", "ParaFltr", "Filter used to match elements by the value of a parameter", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rules", "R", "Rules to check", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var rules = new List<ARDB.FilterRule>();
      if (!DA.GetDataList("Rules", rules))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      rules = rules.OfType<ARDB.FilterRule>().ToList();
      if (rules.Count > 0)
        DA.SetData("Filter", new ARDB.ElementParameterFilter(rules, inverted));
    }
  }

  [ComponentVersion(introduced: "1.2")]
  public class ElementWorksetFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("3380C493-B1DF-4E93-A2CA-612808291394");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "W";

    public ElementWorksetFilter()
    : base("Workset Filter", "WSetFltr", "Filter used to match elements by their workset", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Workset(), "Workset", "W", "Workset to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var workset = default(Types.Workset);
      if (!DA.GetData("Workset", ref workset))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new ARDB.ElementWorksetFilter(workset.Id, inverted));
    }
  }
}
