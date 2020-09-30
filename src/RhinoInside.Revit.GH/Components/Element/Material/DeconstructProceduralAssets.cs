using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public abstract class DeconstructTextureAsset<T>
    : AssetComponent<T> where T : TextureData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public DeconstructTextureAsset() : base()
    {
      this.Name = $"Deconstruct {ComponentInfo.Name}";
      this.NickName = $"D-{ComponentInfo.NickName}";
      this.Description = $"Deconstruct {ComponentInfo.Description}";
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.TextureData<T>(),
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
      Types.TextureData<T> textureAsset = default;
      if(DA.GetData(ComponentInfo.Name, ref textureAsset)
             && textureAsset.Value is T textureData)
      {
        SetOutputsFromAssetData(DA, textureData);
      }
    }
  }


  public class DeconstructBitmapTexture
    : DeconstructTextureAsset<UnifiedBitmapData>
  {
    public override Guid ComponentGuid
      => new Guid("77b391db-014a-4964-8810-5927df0dd7bb");
  }

  public class DeconstructCheckerTexture
    : DeconstructTextureAsset<CheckerData>
  {
    public override Guid ComponentGuid
      => new Guid("d44a1497-ae52-4e4d-ab8e-962195177fbb");
  }
}
