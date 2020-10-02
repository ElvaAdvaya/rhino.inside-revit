using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class CreateStructuralAsset : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("af2678c8-2a53-4056-9399-5a06dd9ac14d");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
    };
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
    };

    public CreateStructuralAsset() : base(
      name: "Add Structural Asset",
      nickname: "C-STAST",
      description: "Create a new instance of structural asset inside document",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {

    }
  }
}
