using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.GH.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public abstract class AnalyzeAppearanceAsset<T>
  : AssetComponent<T> where T : ShaderData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AnalyzeAppearanceAsset() : base()
    {
      Name = $"Analyze {ComponentInfo.Name}";
      NickName = $"A-{ComponentInfo.NickName}";
      Description = $"Analyze given {ComponentInfo.Description}";
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.AppearanceAsset(),
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
      => SetFieldsAsOutputs(pManager);

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var appearanceAsset = default(DB.AppearanceAssetElement);
      if (DA.GetData(ComponentInfo.Name, ref appearanceAsset))
      {
        var asset = appearanceAsset.GetRenderingAsset();
        if (asset != null)
          SetOutputsFromAsset(DA, asset);
      }
    }
  }

  public class AnalyzeGenericShader
    : AnalyzeAppearanceAsset<GenericData>
  {
    public override Guid ComponentGuid =>
      new Guid("5b18389b-5e25-4428-b1a6-1a55109a7a3c");
  }
}
