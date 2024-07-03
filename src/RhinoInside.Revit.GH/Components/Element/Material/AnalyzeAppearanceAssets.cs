using System;

using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Materials
{
#if REVIT_2018
  public abstract class AnalyzeAppearanceAsset<T>
  : BaseAssetComponent<T> where T : ShaderData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AnalyzeAppearanceAsset() : base()
    {
      Name = $"Analyze {ComponentInfo.Name}";
      NickName = $"A-{ComponentInfo.NickName}";
      Description = $"Analyze given {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };
    protected override ParamDefinition[] Outputs => GetAssetDataAsOutputs();

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var appearanceAsset = default(ARDB.AppearanceAssetElement);
      if (!DA.GetData(ComponentInfo.Name, ref appearanceAsset))
        return;

      using (var asset = appearanceAsset.GetRenderingAsset())
      {
        if (asset != null)
          SetOutputsFromAsset(DA, asset);
      }
    }
  }

  [ComponentVersion(introduced: "1.0", updated: "1.22")]
  public class AnalyzeGenericShader
    : AnalyzeAppearanceAsset<GenericData>
  {
    public override Guid ComponentGuid =>
      new Guid("5b18389b-5e25-4428-b1a6-1a55109a7a3c");
  }
#endif
}
