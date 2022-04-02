using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElements
{
  [ComponentVersion(introduced: "1.7")]
  public class AddRoomSeparatorLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("34186815-AAF1-44C5-B400-8EE426B14AC8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddRoomSeparatorLine() : base
    (
      name: "Add Room Separation",
      nickname: "RoomSeparation",
      description: "Given the curve, it adds a Room separation line to the given Revit view",
      category: "Revit",
      subCategory: "Spatial"
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
          Description = "View to add a specific a room separator line",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curves to create a specific room separation line",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = _RoomSeparation_,
          NickName = _RoomSeparation_.Substring(0, 1),
          Description = $"Output {_RoomSeparation_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _RoomSeparation_ = "Room Separation";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        view.Document, _RoomSeparation_, (roomSeparatorLine) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

          var tol = GeometryObjectTolerance.Model;
          if
          (
            curve.IsShort(tol.ShortCurveTolerance) ||
            curve.IsClosed ||
            !curve.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid horizontal, coplanar and open curve.", curve);

          // Compute
          roomSeparatorLine = Reconstruct(roomSeparatorLine, view, curve);

          DA.SetData(_RoomSeparation_, roomSeparatorLine);
          return roomSeparatorLine;
        }
      );
    }

    bool Reuse(ARDB.ModelCurve roomSeparator, ARDB.View view, Curve curve)
    {
      if (roomSeparator is null) return false;
      if (roomSeparator.OwnerViewId != view.Id) return false;

      using (var projectedCurve = Curve.ProjectToPlane(curve, view.SketchPlane.GetPlane().ToPlane()).ToCurve())
      {
        if (!projectedCurve.IsAlmostEqualTo(roomSeparator.GeometryCurve))
          roomSeparator.SetGeometryCurve(projectedCurve, true);
      }

      return true;
    }

    ARDB.ModelCurve Create(ARDB.View view, Curve curve)
    {
      using (var projectedCurve = Curve.ProjectToPlane(curve, view.SketchPlane.GetPlane().ToPlane()))
      using (var curveArray = new ARDB.CurveArray())
      {
        curveArray.Append(projectedCurve.ToCurve());
        return view.Document.Create.NewRoomBoundaryLines(view.SketchPlane, curveArray, view).get_Item(0);
      }
    }

    ARDB.ModelCurve Reconstruct(ARDB.ModelCurve roomSeparator, ARDB.View view, Curve curve)
    {
      if (!Reuse(roomSeparator, view, curve))
        roomSeparator = Create(view, curve);

      return roomSeparator;
    }
  }
}
