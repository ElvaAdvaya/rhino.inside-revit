using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;

namespace RhinoInside.Revit.GH.Components
{
  public class AdaptiveComponentByPoints : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E8DDC0E4-97E9-4659-9945-E8C77114273D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AdaptiveComponentByPoints() : base
    (
      "Add Component (Adaptive)", "CompAdap",
      "Given a collection of Points, it adds an AdaptiveComponent element to the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FamilyInstance(), "Component", "C", "New Adaptive Component element", GH_ParamAccess.item);
    }

    void ReconstructAdaptiveComponentByPoints
    (
      Document doc,
      ref Autodesk.Revit.DB.Element element,

      IList<Rhino.Geometry.Point3d> points,
      Autodesk.Revit.DB.FamilySymbol type
    )
    {
      var adaptivePoints = points.ConvertAll(GeometryEncoder.ToXYZ);

      if (!type.IsActive)
        type.Activate();

      // Type
      ChangeElementTypeId(ref element, type.Id);

      if (element is FamilyInstance instance && AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(instance))
      {
        var adaptivePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);
        if (adaptivePointIds.Count == adaptivePoints.Count)
        {
          int index = 0;
          foreach (var vertex in adaptivePointIds.Select(id => doc.GetElement(id)).Cast<ReferencePoint>())
            vertex.Position = adaptivePoints[index++];

          return;
        }
      }

      {
        var creationData = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>
        {
          Revit.ActiveUIApplication.Application.Create.NewFamilyInstanceCreationData(type, adaptivePoints)
        };

        var newElementIds = doc.IsFamilyDocument ?
                            doc.FamilyCreate.NewFamilyInstances2( creationData ) :
                            doc.Create.NewFamilyInstances2( creationData );

        if (newElementIds.Count != 1)
        {
          doc.Delete(newElementIds);
          throw new InvalidOperationException();
        }

        var parametersMask = new BuiltInParameter[]
        {
          BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          BuiltInParameter.ELEM_FAMILY_PARAM,
          BuiltInParameter.ELEM_TYPE_PARAM
        };

        ReplaceElement(ref element, doc.GetElement(newElementIds.First()), parametersMask);
      }
    }
  }
}
