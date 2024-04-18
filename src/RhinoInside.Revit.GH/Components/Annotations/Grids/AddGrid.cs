using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Annotations.Grids
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using GH.Exceptions;

  [ComponentVersion(introduced: "1.0", updated: "1.6")]
  public class AddGrid : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("CEC2B3DF-C6BA-414F-BECE-E3DAEE2A3F2C");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddGrid() : base
    (
      name: "Add Grid",
      nickname: "Grid",
      description: "Given its Axis, it adds a Grid element to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Grid curve",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ProjectElevation
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the grid.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Curve'.",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.ProjectElevation
        {
          Name = "Top",
          NickName = "TO",
          Description = $"Top of the grid.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Curve'",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Grid Name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Grid Type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Grids
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Grid()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template grid",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Grid()
        {
          Name = _Grid_,
          NickName = _Grid_.Substring(0, 1),
          Description = $"Output {_Grid_}",
        }
      ),
    };

    const string _Grid_ = "Grid";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.DATUM_TEXT
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Grid>
      (
        doc.Value, _Grid_, grid =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;
          if (!Params.TryGetData(DA, "Top", out ERDB.ElevationElementReference? topElevation)) return null;
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.GridType type, doc, ARDB.ElementTypeGroup.GridType)) return null;
          Params.TryGetData(DA, "Template", out ARDB.Grid template);


          var extents = new Interval();
          // Validation & Defaults
          {
            var tol = GeometryTolerance.Model;
            if
            (
              !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance)) ||
              !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
              axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
            )
              throw new RuntimeArgumentException("Curve", "Curve must be a horizontal line or arc curve.", curve);

            extents = new Interval
            (
              GeometryEncoder.ToInternalLength(axisPlane.OriginZ),
              GeometryEncoder.ToInternalLength(axisPlane.OriginZ)
            );

            if (!baseElevation.HasValue) extents.T0 -= 12.0;
            else if (baseElevation.Value.IsOffset(out var baseOffset)) extents.T0 += baseOffset;
            else if (baseElevation.Value.IsElevation(out var elevation)) extents.T0 = elevation;
            else if (baseElevation.Value.IsUnlimited()) extents.T0 = double.NegativeInfinity;

            if (!topElevation.HasValue) extents.T1 += 12.0;
            else if (topElevation.Value.IsOffset(out var topOffset)) extents.T1 += topOffset;
            else if (topElevation.Value.IsElevation(out var elevation)) extents.T1 = elevation;
            else if (topElevation.Value.IsUnlimited()) extents.T1 = double.PositiveInfinity;
          }

          // Compute
          if (CanReconstruct(_Grid_, out var untracked, ref grid, doc.Value, name, categoryId: ARDB.BuiltInCategory.OST_Grids))
            grid = Reconstruct(grid, doc.Value, curve, extents, type, name, template);

          DA.SetData(_Grid_, grid);
          return untracked ? null : grid;
        }
      );
    }

    bool Reuse(ARDB.Grid grid, Curve curve, ARDB.GridType type, ARDB.Grid template)
    {
      if (grid is null) return false;

      var tol = GeometryTolerance.Internal;
      var gridCurve = grid.IsCurved ? grid.Curve : grid.Curve.CreateReversed();
      var newCurve = curve.ToCurve();

      if (!gridCurve.AlmostEquals(newCurve, tol.VertexTolerance))
      {
        if (!gridCurve.IsSameKindAs(newCurve)) return false;
        if (gridCurve is ARDB.Arc gridArc && newCurve is ARDB.Arc newArc)
        {
          // I do not found any way to update the radius ??
          if (!tol.DefaultTolerance.Equals(gridArc.Radius, newArc.Radius))
            return false;
        }

        // Makes Grid cross maximum number of views.
        // This increases our chances of obtaining a valid view for this Grid.
        grid.Maximize3DExtents();

        var view = default(ARDB.View);
        var viewsFilter = ERDB.CompoundElementFilter.ElementClassFilter(typeof(ARDB.View3D), typeof(ARDB.ViewPlan));
        using (var collector = new ARDB.FilteredElementCollector(grid.Document).WherePasses(viewsFilter))
        {
          var views = collector.Cast<ARDB.View>().
            Where(x => grid.CanBeVisibleInView(x) && !x.IsTemplate && !x.IsAssemblyView).
            OrderByDescending(x => x.ViewType);

          view = views.FirstOrDefault();
        }

        if (view is null) return false;

        var curves = grid.GetCurvesInView(ARDB.DatumExtentType.Model, view);
        if (curves.Count != 1) return false;

        curves[0] = grid.IsCurved ? curves[0] : curves[0].CreateReversed();
        curves[0].TryGetLocation(out var origin0, out var basisX0, out var basisY0);
        newCurve.TryGetLocation(out var origin, out var _, out var _);

        // Move newCurve to same plane as current curve
        var elevationDelta = origin0.Z - origin.Z;
        newCurve = newCurve.CreateTransformed(ARDB.Transform.CreateTranslation(ERDB.UnitXYZ.BasisZ * elevationDelta));
        newCurve.TryGetLocation(out var origin1, out var basisX1, out var basisY1);

        var pinned = grid.Pinned;
        grid.Pinned = false;

        grid.Location.Move(origin1 - origin0);
        using (var axis = ARDB.Line.CreateUnbound(origin1, ERDB.UnitXYZ.BasisZ))
          grid.Location.Rotate(axis, basisX0.AngleOnPlaneTo(basisX1, ERDB.UnitXYZ.BasisZ));

        grid.SetCurveInView(ARDB.DatumExtentType.Model, view, newCurve);
        grid.Pinned = pinned;
      }

      if (type is object && grid.GetTypeId() != type.Id) grid.ChangeTypeId(type.Id);
      grid.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.Grid Create(ARDB.Document doc, Curve curve, ARDB.GridType type, ARDB.Grid template)
    {
      var grid = default(ARDB.Grid);
      {
        var tol = GeometryTolerance.Model;
        if (curve.TryGetLine(out var line, tol.VertexTolerance))
        {
          grid = ARDB.Grid.Create(doc, line.ToLine().CreateReversed() as ARDB.Line);
        }
        else if (curve.TryGetArc(out var arc, tol.VertexTolerance))
        {
          grid = ARDB.Grid.Create(doc, arc.ToArc().CreateReversed() as ARDB.Arc);
        }
        else
        {
          throw new RuntimeArgumentException("Curve", "Curve must be a horizontal line or arc curve.", curve);
        }

        grid.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      if (type is object) grid.ChangeTypeId(type.Id);

      return grid;
    }

    ARDB.Grid Reconstruct(ARDB.Grid grid, ARDB.Document doc, Curve curve, Interval extents, ARDB.GridType type, string name, ARDB.Grid template)
    {
      if (!Reuse(grid, curve, type, template))
      {
        var previousGrid = grid;
        grid = grid.ReplaceElement
        (
          Create(doc, curve, type, template),
          ExcludeUniqueProperties
        );

        // Avoids conflict in case we are going to assign same name...
        if (previousGrid.IsValid())
        {
          if (name is null) name = previousGrid.Name;
          previousGrid.Document.Delete(previousGrid.Id);
        }
      }

      if (name is object && grid.Name != name)
        grid.Name = name;

      using (var outline = grid.GetExtents())
      {
        var tol = GeometryTolerance.Internal;
        if (!tol.DefaultTolerance.Equals(extents.T0, outline.MinimumPoint.Z) || !tol.DefaultTolerance.Equals(extents.T1, outline.MaximumPoint.Z))
        {
          grid.SetVerticalExtents
          (
            double.IsInfinity(extents.T0) ? outline.MinimumPoint.Z : extents.T0,
            double.IsInfinity(extents.T1) ? outline.MaximumPoint.Z : extents.T1
          );
        }
      }

      return grid;
    }
  }
}
