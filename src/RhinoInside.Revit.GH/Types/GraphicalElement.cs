using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Convert.Units;
  using External.DB.Extensions;

  /// <summary>
  /// Interface that represents any <see cref="ARDB.Element"/> that has a Graphical representation in Revit
  /// </summary>
  [Kernel.Attributes.Name("Graphical Element")]
  public interface IGH_GraphicalElement : IGH_Element, IGH_QuickCast
  {
    bool? ViewSpecific { get; }
    View OwnerView { get; }
  }

  [Kernel.Attributes.Name("Graphical Element")]
  public class GraphicalElement :
    Element,
    IGH_GraphicalElement,
    IGH_GeometricGoo,
    IGH_PreviewData
  {
    public GraphicalElement() { }
    public GraphicalElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public GraphicalElement(ARDB.Element element) : base(element) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      if (!element.IsValid())
        return false;

      if (element is ARDB.ElementType)
        return false;

      if (element is ARDB.View)
        return false;

      // Unplaced ARDB.SpatialElement is also a GraphicalElement
      if (element is ARDB.SpatialElement)
        return true;

      using (var location = element.Location)
      {
        if (location is object) return true;
      }

      return element.HasBoundingBoxXYZ();
    }

    protected override void SubInvalidateGraphics()
    {
      clippingBox = default;

      base.SubInvalidateGraphics();
    }

    #region IGH_GraphicalElement
    public bool? ViewSpecific => Value?.ViewSpecific;
    public View OwnerView => View.FromElementId(Document, Value?.OwnerViewId) as View;
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => BoundingBox;
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => false;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsReferencedDataLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public virtual BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.Element element)
        return element.GetBoundingBoxXYZ().ToBoundingBox().GetBoundingBox(xform);

      return NaN.BoundingBox;
    }

    bool IGH_GeometricGoo.LoadGeometry() => IsReferencedDataLoaded || LoadReferencedData();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsReferencedDataLoaded || LoadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_QuickCast
    Point3d IGH_QuickCast.QC_Pt()
    {
      var position = Position;
      if (position.IsValid)
        return position;

      throw new InvalidCastException();
    }
    Vector3d IGH_QuickCast.QC_Vec()
    {
      var direction = Direction;
      if (direction.IsValid)
        return direction;

      throw new InvalidCastException();
    }
    Interval IGH_QuickCast.QC_Interval()
    {
      var bbox = BoundingBox;
      if (bbox.IsValid)
        return new Interval(bbox.Min.Z, bbox.Max.Z);

      throw new InvalidCastException();
    }
    #endregion

    #region IGH_PreviewData
    private BoundingBox? clippingBox;
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (!clippingBox.HasValue)
          clippingBox = ClippingBox;

        return clippingBox.Value;
      }
    }

    /// <summary>
    /// Not necessarily accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for display.
    /// </summary>
    public virtual BoundingBox ClippingBox => BoundingBox;

    public virtual void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = ClippingBox;
      if (!bbox.IsValid)
        return;

      foreach (var edge in bbox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x00003333, args.Thickness);
    }

    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override bool CastTo<Q>(out Q target)
    {
      target = default;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Interval)))
      {
        var domain = Domain;
        if (!domain.IsValid)
          return false;

        target = (Q) (object) new GH_Interval(domain);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      {
        var domain = DomainUV;
        if (!domain.IsValid)
          return false;

        target = (Q) (object) new GH_Interval2D(domain);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        try
        {
          var plane = Location;
          if (!plane.IsValid || !plane.Origin.IsValid)
            return false;

          target = (Q) (object) new GH_Plane(plane);
          return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        var position = Position;
        if (!position.IsValid)
          return false;

        target = (Q) (object) new GH_Point(position);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        var direction = Direction;
        if (!direction.IsValid || direction.IsZero)
          return false;

        target = (Q) (object) new GH_Vector(direction);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
      {
        var curve = Curve;
        if (!curve.IsValid || curve.IsClosed)
          return false;

        target = (Q) (object) new GH_Line(new Line(curve.PointAtStart, curve.PointAtEnd));
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Transform)))
      {
        var plane = Location;
        if (!plane.IsValid || !plane.Origin.IsValid)
          return false;

        target = (Q) (object) new GH_Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        var box = Box;
        if (!box.IsValid)
          return false;

        target = (Q) (object) new GH_Box(box);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Rectangle)))
      {
        var rectangle = Rectangle;
        if (!rectangle.IsValid)
          return false;

        target = (Q) (object) new GH_Rectangle(rectangle);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
      {
        var curve = Curve;
        if (curve?.IsValid != true)
          return false;

        target = (Q) (object) new GH_Line(new Line(curve.PointAtStart, curve.PointAtEnd));
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
      {
        var axis = Curve;
        if (axis is null)
          return false;

        target = (Q) (object) new GH_Curve(axis);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      {
        var surface = TrimmedSurface;
        if (surface is null)
          return false;

        target = (Q) (object) new GH_Surface(surface);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
      {
        var brep = PolySurface;
        if (brep is null)
          return false;

        target = (Q) (object) new GH_Brep(brep);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
      {
        var mesh = Mesh;
        if (mesh is null)
          return false;

        target = (Q) (object) new GH_Mesh(mesh);
        return true;
      }

      return base.CastTo(out target);
    }

    #region Properties
    public virtual Category Subcategory
    {
      get => default;
      set => throw new Exceptions.RuntimeErrorException($"{((IGH_Goo) this).TypeName} '{DisplayName}' does not support assignment of a Subcategory.");
    }

    public virtual ARDB.ElementId LevelId => Value?.LevelId;
    public Level Level => LevelId is ARDB.ElementId levelId ? new Level(Document, levelId) : default;
    #endregion

    #region Location
    /// <summary>
    /// Accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for computation.
    /// </summary>
    public BoundingBox BoundingBox => GetBoundingBox(Transform.Identity);

    /// <summary>
    /// Box aligned to <see cref="Location"/>
    /// </summary>
    public virtual Box Box
    {
      get
      {
        if (Value is ARDB.Element element)
        {
          var plane = Location;
          if (!Location.IsValid)
            return element.GetBoundingBoxXYZ().ToBox();

          var bbox = GetBoundingBox(Transform.ChangeBasis(Plane.WorldXY, plane));
          if (bbox.IsValid)
          {
            return new Box
            (
              plane,
              new Interval(bbox.Min.X, bbox.Max.X),
              new Interval(bbox.Min.Y, bbox.Max.Y),
              new Interval(bbox.Min.Z, bbox.Max.Z)
            );
          }
        }

        return NaN.Box;
      }
    }

    public virtual Rectangle3d Rectangle
    {
      get
      {
        var box = Box;
        if (box.IsValid)
          return new Rectangle3d(box.Plane, box.X, box.Y);

        return Rectangle3d.Unset;
      }
    }

    public virtual Interval Domain
    {
      get
      {
        var box = BoundingBox;
        if (!box.IsValid)
          return NaN.Interval;

        return new Interval(box.Min.Z, box.Max.Z);
      }
    }

    public virtual UVInterval DomainUV
    {
      get
      {
        var box = BoundingBox;
        if (!box.IsValid)
          return new UVInterval(NaN.Interval, NaN.Interval);

        var u = new Interval(box.Min.X, box.Max.X);
        var v = new Interval(box.Min.Y, box.Max.Y);
        return new UVInterval(u, v);
      }
    }

    /// <summary>
    /// <see cref="Rhino.Geometry.Plane"/> where this element is located.
    /// </summary>
    public virtual Plane Location
    {
      get
      {
        var origin = NaN.Point3d;
        var axis = NaN.Vector3d;
        var perp = NaN.Vector3d;

        if (Value is ARDB.Element element)
        {
          switch (element.Location)
          {
            case ARDB.LocationPoint pointLocation:
              origin = pointLocation.Point.ToPoint3d();
              axis = Vector3d.XAxis;
              perp = Vector3d.YAxis;

              try
              {
                axis.Rotate(pointLocation.Rotation, Vector3d.ZAxis);
                perp.Rotate(pointLocation.Rotation, Vector3d.ZAxis);
              }
              catch { }

              break;
            case ARDB.LocationCurve curveLocation:
              if(curveLocation.Curve.TryGetLocation(out var cO, out var cX, out var cY))
                return new Plane(cO.ToPoint3d(), cX.ToVector3d(), cY.ToVector3d());

              break;
            default:
              // Try with the first non empty geometry object.
              using (var options = new ARDB.Options { DetailLevel = ARDB.ViewDetailLevel.Undefined })
              {
                if (element.get_Geometry(options).TryGetLocation(out var gO, out var gX, out var gY))
                  return new Plane(gO.ToPoint3d(), gX.ToVector3d(), gY.ToVector3d());
              }

              var bbox = BoundingBox;
              if (bbox.IsValid)
              {
                // If we have nothing better, the center of the BoundingBox will do the job.
                origin = BoundingBox.Center;
                axis = Vector3d.XAxis;
                perp = Vector3d.YAxis;
              }
              break;
          }
        }

        return new Plane(origin, axis, perp);
      }
      set => SetLocation(value, keepJoins: false);
    }

    public void SetLocation(Plane location, bool keepJoins = false)
    {
      using (var plane = location.ToPlane())
        SetLocation(plane.Origin, plane.XVec, plane.YVec, keepJoins);
    }

    void GetLocation(out ARDB.XYZ origin, out ARDB.XYZ basisX, out ARDB.XYZ basisY)
    {
      var plane = Location.ToPlane();
      origin = plane.Origin;
      basisX = plane.XVec;
      basisY = plane.YVec;
    }

    void SetLocation(ARDB.XYZ newOrigin, ARDB.XYZ newBasisX, ARDB.XYZ newBasisY, bool keepJoins)
    {
      if (Value is ARDB.Element element)
      {
        GetLocation(out var origin, out var basisX, out var basisY);
        var basisZ = basisX.CrossProduct(basisY);
        var newBasisZ = newBasisX.CrossProduct(newBasisY);

        if (element.Location is ARDB.LocationCurve curveLocation)
        {
          var orientation = ARDB.Transform.Identity;
          orientation.SetToAlignCoordSystem
          (
            origin, basisX, basisY, basisZ,
            newOrigin, newBasisX, newBasisY, newBasisZ
          );

          if (!orientation.IsIdentity)
            SetCurve(curveLocation.Curve.CreateTransformed(orientation).ToCurve(), keepJoins);

          return;
        }

        var pinned = element.Pinned;
        var modified = false;

        try
        {
          {
            if (!basisZ.IsParallelTo(newBasisZ))
            {
              var axisDirection = basisZ.CrossProduct(newBasisZ);
              double angle = basisZ.AngleTo(newBasisZ);

              using (var axis = ARDB.Line.CreateUnbound(origin, axisDirection))
              {
                element.Pinned = false;
                ARDB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
                modified = true;
              }

              GetLocation(out origin, out basisX, out basisY);
              basisZ = basisX.CrossProduct(basisY);
            }

            if (!basisX.IsAlmostEqualTo(newBasisX))
            {
              double angle = basisX.AngleOnPlaneTo(newBasisX, newBasisZ);
              using (var axis = ARDB.Line.CreateUnbound(origin, newBasisZ))
              {
                element.Pinned = false;
                ARDB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
                modified = true;
              }
            }

            {
              var trans = newOrigin - origin;
              if (!trans.IsZeroLength())
              {
                element.Pinned = false;
                ARDB.ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
                modified = true;
              }
            }
          }
        }
        finally
        {
          if (modified)
          {
            if (element.Pinned != pinned)
              element.Pinned = pinned;

            InvalidateGraphics();
          }
        }
      }
    }

    protected static Rhino.DocObjects.ConstructionPlane CreateConstructionPlane(string name, Plane location, Rhino.RhinoDoc rhinoDoc)
    {
      bool imperial = rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Feet || rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Inches;

      return new Rhino.DocObjects.ConstructionPlane()
      {
        Plane = location,
        GridSpacing = imperial ?
        UnitScale.Convert(1.0, UnitScale.Yards, UnitScale.GetModelScale(rhinoDoc)) :
        UnitScale.Convert(1.0, UnitScale.Meters, UnitScale.GetModelScale(rhinoDoc)),

        SnapSpacing = imperial ?
        UnitScale.Convert(1.0, UnitScale.Yards, UnitScale.GetModelScale(rhinoDoc)) :
        UnitScale.Convert(1.0, UnitScale.Meters, UnitScale.GetModelScale(rhinoDoc)),

        GridLineCount = 70,
        ThickLineFrequency = imperial ? 6 : 5,
        DepthBuffered = true,
        Name = name
      };
    }

    public virtual Point3d Position => Curve is Curve curve ?
    curve.PointAtStart : Location.Origin;
    public virtual Vector3d Direction => Curve is Curve curve ?
    curve.PointAtEnd - curve.PointAtStart : PlaneOrientation;

    public virtual Vector3d HandOrientation => Location.XAxis;
    public virtual Vector3d FacingOrientation => Location.YAxis;
    public virtual Vector3d PlaneOrientation => Location.ZAxis;

    public virtual Curve Curve
    {
      get => Value?.Location is ARDB.LocationCurve curveLocation ?
          curveLocation.Curve.ToCurve() :
          default;
      set => SetCurve(value, keepJoins: false);
    }

    public virtual void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (Value is ARDB.Element element && curve is object)
      {
        if (element.Location is ARDB.LocationCurve locationCurve)
        {
          var newCurve = curve.ToCurve();
          if (!locationCurve.Curve.AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
          {
            locationCurve.Curve = newCurve;
            InvalidateGraphics();
          }
        }
        else throw new InvalidOperationException("Curve can not be set for this element.");
      }
    }

    public virtual Surface Surface => null;
    public virtual Brep TrimmedSurface => Brep.CreateFromSurface(Surface);
    public virtual Brep PolySurface => TrimmedSurface;

    public virtual Mesh Mesh => default;
    #endregion

    #region Flip
    public virtual bool CanFlipFacing
    {
      get
      {
        return Value?.GetType() is Type type &&
          type.GetMethod("Flip") is MethodInfo &&
          type.GetProperty("Flipped") is PropertyInfo;
      }
    }
    public virtual bool? FacingFlipped
    {
      get
      {
        return Value is ARDB.Element element && element.GetType().GetProperty("Flipped") is PropertyInfo Flipped ?
          (bool?) Flipped.GetValue(element) :
          default;
      }
      set
      {
        if (value.HasValue && Value is ARDB.Element element)
        {
          var Flip = element.GetType().GetMethod("Flip");
          var Flipped = element.GetType().GetProperty("Flipped");

          if (Flip is null || Flipped is null)
            throw new Exceptions.RuntimeException($"Facing can not be flipped for element. {{{element.Id.ToValue()}}}");

          if ((bool) Flipped.GetValue(element) != value)
          {
            InvalidateGraphics();
            Flip.Invoke(element, new object[] { });
          }
        }
      }
    }

    public virtual bool CanFlipHand => false;
    public virtual bool? HandFlipped
    {
      get => default;
      set
      {
        if (value.HasValue && Value is ARDB.Element element)
        {
          if (!CanFlipHand)
            throw new Exceptions.RuntimeException($"Hand can not be flipped for this element. {{{element.Id.ToValue()}}}");

          if (HandFlipped != value)
            throw new MissingMemberException(element.GetType().FullName, nameof(HandFlipped));
        }
      }
    }

    public virtual bool CanFlipWorkPlane => false;
    public virtual bool? WorkPlaneFlipped
    {
      get => default;
      set
      {
        if (value.HasValue && Value is ARDB.Element element)
        {
          if (!CanFlipWorkPlane)
            throw new Exceptions.RuntimeException($"Work Plane can not be flipped for this element. {{{element.Id.ToValue()}}}");

          if (WorkPlaneFlipped != value)
            throw new MissingMemberException(element.GetType().FullName, nameof(WorkPlaneFlipped));
        }
      }
    }
    #endregion
  }

  static class ElementJoins
  {
    static bool? IsJoinAllowedAtEnd(ARDB.Element element, int end)
    {
      switch (element)
      {
        case ARDB.Wall wall:
          return ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, end);

        case ARDB.FamilyInstance instance:
          if (instance.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_StructuralFraming)
            return ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(instance, end);

          break;
      }

      return null;
    }

    static void AllowJoinAtEnd(ARDB.Element element, int end, bool? allow)
    {
      if (allow.HasValue && allow.Value != IsJoinAllowedAtEnd(element, end))
      {
        switch (element)
        {
          case ARDB.Wall wall:
            if (allow.Value) ARDB.WallUtils.AllowWallJoinAtEnd(wall, end);
            else ARDB.WallUtils.DisallowWallJoinAtEnd(wall, end);

            return;

          case ARDB.FamilyInstance instance:
            if (instance.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_StructuralFraming)
            {
              if (allow.Value) ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(instance, end);
              else ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(instance, end);
            }
            return;
        }

        switch (end)
        {
          case 0: throw new InvalidOperationException("Join at start is not valid for this elemenmt.");
          case 1: throw new InvalidOperationException("Join at end is not valid for this elemenmt.");
          default: throw new ArgumentOutOfRangeException($"{nameof(end)} should be 0 for start or 1 for end.");
        }
      }
    }

    struct DisableJoinsDisposable : IDisposable
    {
      private readonly List<(ARDB.Element Element, int End)> AllowedJoinEnds;

      public DisableJoinsDisposable(ARDB.Element element)
      {
        if (element.Location is ARDB.LocationCurve locationCurve)
        {
          AllowedJoinEnds = new List<(ARDB.Element, int)>();

          // Elements at Ends
          for (int end = 0; end < 2; ++end)
          {
            if (IsJoinAllowedAtEnd(element, end) == true)
            {
              var elementsAtEnd = locationCurve.get_ElementsAtJoin(end).Cast<ARDB.Element>();

              foreach (var elementAtEnd in elementsAtEnd)
              {
                if (elementAtEnd.Id == element.Id) continue;
                if (elementAtEnd.Location is ARDB.LocationCurve joinCurve)
                {
                  if (joinCurve.get_ElementsAtJoin(ERDB.CurveEnd.Start).Cast<ARDB.Element>().Contains(element, ElementEqualityComparer.SameDocument))
                  {
                    AllowedJoinEnds.Add((elementAtEnd, ERDB.CurveEnd.Start));
                    AllowJoinAtEnd(elementAtEnd, ERDB.CurveEnd.Start, allow: false);
                  }

                  if (joinCurve.get_ElementsAtJoin(ERDB.CurveEnd.End).Cast<ARDB.Element>().Contains(element, ElementEqualityComparer.SameDocument))
                  {
                    AllowedJoinEnds.Add((elementAtEnd, ERDB.CurveEnd.End));
                    AllowJoinAtEnd(elementAtEnd, ERDB.CurveEnd.End, allow: false);
                  }
                }
              }

              AllowedJoinEnds.Add((element, end));
              AllowJoinAtEnd(element, end, allow: false);
            }
          }

          // Elements at Mid
          if (element.GetOutline() is ARDB.Outline outline)
          {
            using (var collector = new ARDB.FilteredElementCollector(element.Document))
            {
              var elementCollector = collector.
                WhereElementIsNotElementType().
                WhereElementIsKindOf(element.GetType()).
                WhereCategoryIdEqualsTo(element.Category?.Id).
                WherePasses(new ARDB.ElementIsCurveDrivenFilter()).
                WherePasses(new ARDB.BoundingBoxIntersectsFilter(outline)).
                WherePasses(new ARDB.ExclusionFilter(new ARDB.ElementId[] { element.Id }));

              foreach (var elementAtMid in elementCollector)
              {
                if (elementAtMid.Location is ARDB.LocationCurve joinCurve)
                {
                  if (joinCurve.get_ElementsAtJoin(ERDB.CurveEnd.Start).Cast<ARDB.Element>().Contains(element, ElementEqualityComparer.SameDocument))
                  {
                    if (IsJoinAllowedAtEnd(elementAtMid, ERDB.CurveEnd.Start) == true)
                    {
                      AllowedJoinEnds.Add((elementAtMid, ERDB.CurveEnd.Start));
                      AllowJoinAtEnd(elementAtMid, ERDB.CurveEnd.Start, allow: false);
                    }
                  }

                  if (joinCurve.get_ElementsAtJoin(ERDB.CurveEnd.End).Cast<ARDB.Element>().Contains(element, ElementEqualityComparer.SameDocument))
                  {
                    if (IsJoinAllowedAtEnd(elementAtMid, ERDB.CurveEnd.End) == true)
                    {
                      AllowedJoinEnds.Add((elementAtMid, ERDB.CurveEnd.End));
                      AllowJoinAtEnd(elementAtMid, ERDB.CurveEnd.End, allow: false);
                    }
                  }
                }
              }
            }
          }
        }
        else AllowedJoinEnds = default;
      }

      void IDisposable.Dispose()
      {
        if (AllowedJoinEnds is object)
        {
          foreach (var join in AllowedJoinEnds.OrderBy(x => x.Element.Id.ToValue()).ThenBy(x => x.End))
            AllowJoinAtEnd(join.Element, join.End, allow: true);
        }
      }
    }

    /// <summary>
    /// Disables this element joins until returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that should be disposed to restore <paramref name="element"/> joins state.</returns>
    public static IDisposable DisableJoinsScope(ARDB.Element element) => new DisableJoinsDisposable(element);
  }
}
