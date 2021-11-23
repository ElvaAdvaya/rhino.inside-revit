using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Curtain Grid")]
  public class CurtainGrid : DocumentObject,
    IGH_GeometricGoo,
    IGH_PreviewData
  {
    public CurtainGrid() : base() { }
    public CurtainGrid(ARDB.HostObject host, ARDB.CurtainGrid value) : base(host.Document, value)
    { }

    #region DocumentObject
    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.CurtainGrid grid)
          return $"Curtain Grid [{grid.NumULines} x {grid.NumVLines}]";

        return "Curtain Grid";
      }
    }

    protected override void ResetValue()
    {
      clippingBox = default;
      curves = default;

      base.ResetValue();
    }
    #endregion

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
      {
        var mesh = new Mesh();
        foreach (var curve in Curves)
        {
          if (curve.SegmentCount == 4)
          {
            var face = new MeshFace
            (
              mesh.Vertices.Add(curve.SegmentCurve(0).PointAtStart),
              mesh.Vertices.Add(curve.SegmentCurve(1).PointAtStart),
              mesh.Vertices.Add(curve.SegmentCurve(2).PointAtStart),
              mesh.Vertices.Add(curve.SegmentCurve(3).PointAtStart)
            );
            mesh.Faces.AddFace(face);
          }
        }

        target = (Q) (object) new GH_Mesh(mesh);
        return true;
      }

      target = default;
      return false;
    }
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => GetBoundingBox(Transform.Identity);
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }

    bool IGH_GeometricGoo.IsReferencedGeometry => false;
    bool IGH_GeometricGoo.IsGeometryLoaded => Value is object;
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => default;
    public BoundingBox GetBoundingBox(Transform xform)
    {
      var bbox = BoundingBox.Empty;
      foreach (var curve in Curves)
        bbox.Union(curve.GetBoundingBox(xform));

      return bbox;
    }

    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => default;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => default;
    bool IGH_GeometricGoo.LoadGeometry() => false;
    bool IGH_GeometricGoo.LoadGeometry(RhinoDoc doc) => false;
    void IGH_GeometricGoo.ClearCaches() => ResetValue();
    #endregion

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach (var curve in Curves)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }

    private BoundingBox? clippingBox;
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (!clippingBox.HasValue)
        {
          clippingBox = BoundingBox.Empty;
          foreach (var curve in Curves)
            clippingBox.Value.Union(curve.GetBoundingBox(false));
        }

        return clippingBox.Value;
      }
    }
    #endregion

    #region Implementation
    static IEnumerable<ARDB.CurtainGrid> HostCurtainGrids(ARDB.HostObject host)
    {
      var grids = default(IEnumerable<ARDB.CurtainGrid>);
      switch (host)
      {
        case ARDB.CurtainSystem curtainSystem: grids = curtainSystem.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
        case ARDB.ExtrusionRoof extrusionRoof: grids = extrusionRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
        case ARDB.FootPrintRoof footPrintRoof: grids = footPrintRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
        case ARDB.Wall wall: grids = wall.CurtainGrid is null ? null : Enumerable.Repeat(wall.CurtainGrid, 1); break;
      }

      return grids;
    }

    static IList<ARDB.Reference> GetFaceReferences(ARDB.HostObject host)
    {
      var references = new List<ARDB.Reference>();

      try { references.AddRange(ARDB.HostObjectUtils.GetBottomFaces(host)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(ARDB.HostObjectUtils.GetTopFaces(host)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(ARDB.HostObjectUtils.GetSideFaces(host, ARDB.ShellLayerType.Interior)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(ARDB.HostObjectUtils.GetSideFaces(host, ARDB.ShellLayerType.Exterior)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      return references;
    }

    static bool IsCurtainGridOnFace(ICollection<ARDB.CurtainCell> cells, ARDB.Face face)
    {
      var result = cells.Count > 0;

      foreach (var cell in cells)
      {
        foreach (var loop in cell.CurveLoops.Cast<ARDB.CurveArray>())
        {
          foreach (var curve in loop.Cast<ARDB.Curve>())
          {
            var center = curve.Evaluate(0.5, true);
            var distance = face.Project(center).Distance;
            if (distance > Revit.VertexTolerance)
              return false;
          }
        }
      }

      return result;
    }

    static ARDB.Reference FindReference(ARDB.HostObject host, ARDB.CurtainGrid value)
    {
      if (host is ARDB.Wall wall)
        return new ARDB.Reference(wall);

      var cells = value.GetCurtainCells();
      foreach (var reference in GetFaceReferences(host))
      {
        if (host.GetGeometryObjectFromReference(reference) is ARDB.Face face && IsCurtainGridOnFace(cells, face))
          return reference;
      }

      return default;
    }

    ARDB.CurtainGrid FindCurtainGrid(ARDB.HostObject host, ARDB.Reference reference)
    {
      if (host is ARDB.Wall wall)
      {
        return wall.CurtainGrid;
      }
      else
      {
        if
        (
          reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE &&
          host.GetGeometryObjectFromReference(reference) is ARDB.Face face &&
          HostCurtainGrids(host) is IEnumerable<ARDB.CurtainGrid> grids
        )
        {
          foreach (var grid in grids)
          {
            if (IsCurtainGridOnFace(grid.GetCurtainCells(), face))
              return grid;
          }
        }
      }

      return default;
    }
    #endregion

    #region Properties
    PolyCurve[] curves;
    static readonly PolyCurve[] EmptyCurves = new PolyCurve[0];
    public PolyCurve[] Curves
    {
      get
      {
        if (curves is null)
        {
          if (Value is ARDB.CurtainGrid grid)
            curves = grid.GetCurtainCells().Cast<ARDB.CurtainCell>().SelectMany
            (
              x =>
              {
                try { return x.CurveLoops.ToPolyCurves(); }
                catch { return EmptyCurves; }
              }
            ).ToArray();
          else curves = EmptyCurves;
        }

        return curves;
      }
    }
    #endregion
  }
}
