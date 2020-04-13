using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  // TODO: improve AnalyseWallProfile to work on curtain walls
  // TODO: improve AnalyseWallProfile to work on curved walls
  // TODO: improve AnalyseWallProfile to return profile curves at WallLocationLine
  public class AnalyseWallProfile : Component
  {
    public override Guid ComponentGuid => new Guid("9D2E9D8D-E794-4202-B725-82E78317892F");

    public AnalyseWallProfile() : base(
      name:"Analyse Wall Profile",
      nickname: "A-WP",
      description: "Get the vertical profile of the given wall",
      category: "Revit",
      subCategory: "Analyse"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Wall",
        nickname: "W",
        description: "Wall element to extract the profile",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter(
        name: "Profile Curves",
        nickname: "PC",
        description: "Profile curves of given wall element",
        access: GH_ParamAccess.list
        );
    }

    private List<Rhino.Geometry.Curve> ExtractDependentCurves(DB.Element element)
    {
      return element.GetDependentElements(new DB.ElementClassFilter(typeof(DB.CurveElement)))
             .Select(x => element.Document.GetElement(x))
             .Cast<DB.CurveElement>()
             .Select(x => x.GeometryCurve.ToRhino())
             .ToList();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Wall wall = null;
      if (!DA.GetData("Wall", ref wall))
        return;

      if (wall.WallType.Kind != DB.WallKind.Curtain)
        DA.SetDataList("Profile Curves", ExtractDependentCurves(wall));
    }
  }
}
