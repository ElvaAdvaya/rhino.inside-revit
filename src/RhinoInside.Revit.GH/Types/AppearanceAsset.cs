using System;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class AppearanceAsset : Element
  {
    public override string TypeName => "Revit Appearance Asset";
    public override string TypeDescription => "Represents a Revit Appearance Asset";
    protected override Type ScriptVariableType => typeof(DB.AppearanceAssetElement);
    public static explicit operator DB.AppearanceAssetElement(AppearanceAsset value) =>
      value?.IsValid == true ? value.Value as DB.AppearanceAssetElement : default;

    public AppearanceAsset() { }
    public AppearanceAsset(DB.AppearanceAssetElement asset) : base(asset) { }
  }
}
