using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.8")]
  public class AddRegion : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("AD88CF11-1946-4429-8F4D-172E3F9B866F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddRegion() : base
    (
      name: "Add Region",
      nickname: "Region",
      description: "Given a profile, it adds a region to the given View",
      category: "Revit",
      subCategory: "Annotate"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific region",
        }
      ),
      new ParamDefinition
      (
        new Param_Surface
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Boundary to create a specific region",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given region",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_DetailComponents
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicsStyle
        {
          Name = "Line Style",
          NickName = "LS",
          Description = "Boundary line style",
          Optional = true
        }, ParamRelevance.Secondary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
        }
      )
    };

    const string _Output_ = "Region";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;

      ReconstructElement<ARDB.FilledRegion>
      (
        view.Document, _Output_, region =>
        {
          // Input
          if (!view.Value.IsAnnotationView()) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Nomen}' does not support detail items creation", view);
          if (!Params.GetDataList(DA, "Boundary", out IList<Brep> boundary) || boundary.Count == 0) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.FilledRegionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.FilledRegionType)) return null;
          if (!Params.TryGetData(DA, "Line Style", out Types.GraphicsStyle linestyle, x => view.AssertValidDocument(x, "Line Style"))) return null;

          var tol = GeometryTolerance.Model;
          var viewPlane = view.Location;
          var loops = boundary.OfType<Brep>().SelectMany(x => x.Loops).Select(x => x.To3dCurve()).ToArray();
          foreach (var loop in loops)
          {
            if (loop is null) return null;
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.IsParallelToPlane(viewPlane, tol.VertexTolerance, tol.AngleTolerance)
            )
              throw new Exceptions.RuntimeArgumentException("Boundary", "Curve should be a valid planar, closed curve and parallel to the input view.", loop);
          }

          loops = loops.Select(x => Curve.ProjectToPlane(x, viewPlane)).ToArray();

          // Compute
          region = Reconstruct
          (
            region,
            view.Value,
            loops,
            type,
            linestyle?.Value
          );

          DA.SetData(_Output_, region);
          return region;
        }
      );
    }

    bool Reuse(ARDB.FilledRegion region, ARDB.View view, IList<Curve> boundaries, ARDB.FilledRegionType type)
    {
      if (region is null) return false;
      if (region.OwnerViewId != view.Id) return false;

      if (!(region.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, view.ViewDirection.ToVector3d())))
        return false;
      
      if (region.GetTypeId() != type.Id) region.ChangeTypeId(type.Id);
      return true;
    }

    ARDB.FilledRegion Reconstruct
    (
      ARDB.FilledRegion region,
      ARDB.View view,
      IList<Curve> boundaries,
      ARDB.FilledRegionType type,
      ARDB.GraphicsStyle linestyle
    )
    {
      if (!Reuse(region, view, boundaries, type))
      {
        var curves = boundaries.Select(GeometryEncoder.ToBoundedCurveLoop).ToArray();
        if (curves.Length == 0)
          return null;

        region = ARDB.FilledRegion.Create(view.Document, type.Id, view.Id, curves);
      }

      if (linestyle is object)
      {
        var validStyles = ARDB.FilledRegion.GetValidLineStyleIdsForFilledRegion(view.Document);
        if (view.Document.IsFamilyDocument)
          validStyles.Add(view.Document.OwnerFamily.FamilyCategory.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection).Id);

        if (!validStyles.Contains(linestyle.Id))
            throw new Exceptions.RuntimeArgumentException("Line Style", $"'{linestyle.Name}' is not a valid Line Style for Filled Regions.");

        using (var sketch = region.GetSketch())
        {
          foreach (var curve in sketch.GetProfileCurveElements().SelectMany(x => x))
          {
            if (linestyle.IsEquivalent(curve.LineStyle)) continue;
            curve.LineStyle = linestyle;
          }
        }
      }

      return region;
    }
  }
}
