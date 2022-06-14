using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class ElementPropertyName : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("01934AD1-F31B-43E5-ADD9-C196F4A2467E");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "N";

    public ElementPropertyName()
    : base
    (
      "Element Name",
      "ElemName",
      "Element Name Property. Get-Set accessor to Element Name property.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Name",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Name",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
        },
        ParamRelevance.Primary
      ),
    };

    Dictionary<Types.Element, string> renames;
    protected void ElementSetNomen(Types.Element element, string value)
    {
      if (string.IsNullOrEmpty(value))
        return;

      if (renames is null)
        renames = new Dictionary<Types.Element, string>();

      if (renames.TryGetValue(element, out var nomen))
      {
        if (nomen == value)
          return;

        renames.Remove(element);
      }
      else element.Nomen = Guid.NewGuid().ToString();

      renames.Add(element, value);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      if (Params.GetData(DA, "Name", out string name))
        UpdateElement(element.Value, () => ElementSetNomen(element, name));

      Params.TrySetData(DA, "Name", () => element.Nomen);
    }

    Dictionary<string, string> namesMap;
    public override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      if (renames is object)
      {
        // Create a names map to remap the final output at 'Name'
        namesMap = Params.IndexOfOutputParam("Name") < 0 ? default : new Dictionary<string, string>();

        foreach (var rename in renames)
        {
          if (namesMap is object)
          {
            var nomen = rename.Key.Nomen;
            if (!namesMap.ContainsKey(nomen))
              namesMap.Add(rename.Key.Nomen, rename.Value);
          }

          // Update elements to the final names
          rename.Key.Nomen = rename.Value;
        }
      }
    }

    public override void OnDone(ARDB.TransactionStatus status)
    {
      if (status == ARDB.TransactionStatus.Committed && namesMap is object)
      {
        // Reconstruct output 'Name' with final values from `namesMap`.
        var _Name_ = Params.IndexOfOutputParam("Name");
        if (_Name_ >= 0)
        {
          var nameParam = Params.Output[_Name_];
          foreach (var item in nameParam.VolatileData.AllData(true))
          {
            if (item is GH_String text)
            {
              if (namesMap.TryGetValue(text.Value, out var nomen))
                text.Value = nomen;
              else
                text.Value = null;
            }
          }
        }
      }

      namesMap = default;
      renames = default;
    }
  }

  public class ElementPropertyCategory : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("5AC48DE6-F706-4E88-A4AD-7A4439F1DAB5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "C";

    public ElementPropertyCategory()
    : base
    (
      "Element Category",
      "ElemCat",
      "Element Category Property. Get-Set accessor to Element Category property.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Category",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Category",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Element Category",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      DA.SetData("Category", element.Category);
    }
  }

  public class ElementPropertyType : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("FE427D04-1D8F-48BE-BFBA-EB28AD23FC03");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "T";

    public ElementPropertyType()
    : base
    (
      "Element Type",
      "ElemType",
      "Element Type Property. Get-Set accessor to Element Type property.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type",
          Optional = true,
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Element", out IList<Types.Element> elements)) return;
      if (Params.GetDataList(DA, "Type", out IList<Types.ElementType> types))
      {
        var outputTypes = Params.IndexOfOutputParam("Type") < 0 ? default : new List<Types.ElementType>();
        var typesSets = new Dictionary<Types.ElementType, List<ARDB.ElementId>>();

        int index = 0;
        foreach (var element in elements)
        {
          if (element is object && types.ElementAtOrLast(index) is Types.ElementType type)
          {
            outputTypes?.Add(element is object ? type : default);

            if (!typesSets.TryGetValue(type, out var entry))
              typesSets.Add(type, new List<ARDB.ElementId> { element.Id });
            else
              entry.Add(element.Id);
          }
          else outputTypes?.Add(default);

          index++;
        }

        var map = new Dictionary<ARDB.ElementId, ARDB.ElementId>();
        foreach (var type in typesSets)
        {
          UpdateDocument
          (
            type.Key.Document, () =>
            {
              foreach (var entry in ARDB.Element.ChangeTypeId(type.Key.Document, type.Value, type.Key.Id))
              {
                if (map.ContainsKey(entry.Key)) map.Remove(entry.Key);
                map.Add(entry.Key, entry.Value);
              }
            }
          );
        }

        DA.SetDataList
        (
          "Element",
          elements.Select
          (
            x =>
            x is null ? null :
            map.TryGetValue(x.Id, out var newId) ?
            Types.Element.FromElementId(x.Document, newId) : x
          )
        );

        Params.TrySetDataList(DA, "Type", () => outputTypes);
      }
      else
      {
        DA.SetDataList("Element", elements);
        Params.TrySetDataList(DA, "Type", () => elements.Select(x => x?.Type));
      }
    }
  }
}
