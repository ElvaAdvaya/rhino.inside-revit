using System;
using System.Linq;
using System.Reflection;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using System.Diagnostics;
  using Convert.Display;
  using Convert.Geometry;
  using External.DB.Extensions;
  using GH.Kernel.Attributes;
  using Grasshopper.Special;

  [Name("Geometry")]
  public interface IGH_GeometryObject : IGH_ElementId { }

  [Name("Geometry")]
  public abstract class GeometryObject : ElementId,
    IGH_GeometryObject,
    IGH_GeometricGoo,
    IGH_PreviewData,
    IGH_PreviewMeshData
  {
    #region System.Object
#if DEBUG
    public override string ToString()
    {
      try   { return Reference.ConvertToStableRepresentation(ReferenceDocument); }
      catch { return base.ToString(); }
    }
#endif
    #endregion

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GeometryObject)))
      {
        target = (Q) (object) Value;
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(ARDB.Reference)))
      {
        target = (Q) (object) Reference;
        return true;
      }
      else if (Reference is object && typeof(IGH_Element).IsAssignableFrom(typeof(Q)))
      {
        target = (Q) (object) Element.FromReference(ReferenceDocument, Reference) is Q goo ? goo : default;
        return true;
      }

      return base.CastTo(out target);
    }
    #endregion

    #region IGH_ElementId
    public override ARDB.ElementId Id => reference is null ? null :
      reference.LinkedElementId != ARDB.ElementId.InvalidElementId ?
      reference.LinkedElementId :
      reference.ElementId;

    private ARDB.Document referenceDocument;
    public override ARDB.Document ReferenceDocument => referenceDocument?.IsValidObject is true ? referenceDocument : null;

    private ARDB.Reference reference;
    public override ARDB.Reference Reference => reference;

    public override ARDB.ElementId ReferenceId => reference?.ElementId;

    public override bool IsReferencedData => DocumentGUID != Guid.Empty;
    public override bool IsReferencedDataLoaded => ReferenceDocument is object && Reference is object;
    public override bool LoadReferencedData()
    {
      if (IsReferencedData && !IsReferencedDataLoaded)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(DocumentGUID, out referenceDocument))
        {
          try
          {
            reference = ARDB.Reference.ParseFromStableRepresentation(referenceDocument, UniqueID);

            if (reference.LinkedElementId == ARDB.ElementId.InvalidElementId)
            {
              Document = referenceDocument;
              return true;
            }

            if (referenceDocument.GetElement(reference.ElementId) is ARDB.RevitLinkInstance link && link.GetLinkDocument() is ARDB.Document linkDocument)
            {
              Document = linkDocument;
              return true;
            }
          }
          catch { }

          referenceDocument = null;
          reference = null;
          Document = null;
        }
      }

      return IsReferencedDataLoaded;
    }

    public override void UnloadReferencedData()
    {
      if (IsReferencedData)
      {
        reference = default;
        referenceDocument = default;
      }

      base.UnloadReferencedData();
    }

    protected override object FetchValue()
    {
      transform = default;

      if (ReferenceDocument is object && reference is object)
      {
        try
        {
          if (ReferenceDocument.GetElement(reference) is ARDB.Element element)
          {
            if (element is ARDB.RevitLinkInstance link)
            {
              transform = link.GetTransform().ToTransform();
              element = link.GetLinkDocument()?.GetElement(reference.LinkedElementId);
              reference = reference.CreateReferenceInLink();
            }

            if (element is ARDB.Instance instance)
              transform = transform.HasValue ? transform.Value * instance.GetTransform().ToTransform() : instance.GetTransform().ToTransform();

            Document = element?.Document;
            var geometry = element?.GetGeometryObjectFromReference(reference);
            return geometry;
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }

      return default;
    }
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => GetBoundingBox(Transform.Identity);
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedData;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsReferencedDataLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public abstract BoundingBox GetBoundingBox(Transform xform);
    bool IGH_GeometricGoo.LoadGeometry() => IsReferencedDataLoaded || LoadReferencedData();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsReferencedDataLoaded || LoadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_PreviewData
    private BoundingBox? clippingBox;
    BoundingBox IGH_PreviewData.ClippingBox => clippingBox ?? (clippingBox = ClippingBox).Value;

    public virtual void DrawViewportWires(GH_PreviewWireArgs args) { }
    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    #region IGH_PreviewMeshData
    protected Point point = null;
    protected Curve[] wires = null;
    protected Mesh[] meshes = null;
    protected double levelOfDetail = double.NaN;

    Transform? transform = default;
    protected Transform Transform => transform ?? Transform.Identity;
    protected bool HasTransform => transform.HasValue;

    void IGH_PreviewMeshData.DestroyPreviewMeshes()
    {
      clippingBox = null;
      transform = null;
      levelOfDetail = double.NaN;

      point = null;
      wires = null;
      meshes = null;
    }

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => meshes;
    #endregion

    protected GeometryObject() { }
    protected GeometryObject(ARDB.Document document, ARDB.GeometryObject geometryObject) : base(document, geometryObject) { }
    protected GeometryObject(ARDB.Document document, ARDB.Reference reference)
    {
      DocumentGUID = document.GetFingerprintGUID();
      UniqueID = reference.ConvertToStableRepresentation(document);

      referenceDocument = document;
      this.reference = reference;

      Document = reference.LinkedElementId == ARDB.ElementId.InvalidElementId ? referenceDocument :
        referenceDocument.GetElement<ARDB.RevitLinkInstance>(reference.ElementId)?.GetLinkDocument();
    }

    public static GeometryObject FromReference(ARDB.Document document, ARDB.Reference reference, ARDB.ElementId linkInstanceId, ARDB.Document linkedDocument)
    {
      return FromReference(document, reference.CreateGeometryLinkReference(document, linkInstanceId, linkedDocument));
    }

    public static GeometryObject FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      switch (reference.ElementReferenceType)
      {
        case ARDB.ElementReferenceType.REFERENCE_TYPE_NONE:
          return new GeometryElement(document, reference);

        case ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR:
        {
          var stable = reference.ConvertToStableRepresentation(document);
          return (stable.EndsWith("/0") || stable.EndsWith("/1")) ?
            (GeometryObject) GeometryPoint.FromReference(document, reference) :
            (GeometryObject) new GeometryCurve(document, reference);
        }

        case ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE:
          return new GeometryFace(document, reference);
      }
      return null;
    }

    public new ARDB.GeometryObject Value
    {
      get
      {
        var value = base.Value as ARDB.GeometryObject;

        if (!value.IsValid())
        {
          if (value is object)
          {
            Debug.WriteLine("GeometryObject.IsValid() == false");
            ResetValue();
          }

          value = base.Value as ARDB.GeometryObject;
        }

        return value;
      }
    }

    protected override void ResetValue()
    {
      (this as IGH_PreviewMeshData).DestroyPreviewMeshes();
      base.ResetValue();
    }

    protected void SetValue(ARDB.Document doc, ARDB.Reference reference)
    {
      ResetValue();

      if (doc is object && reference is object)
      {
        try
        {
          DocumentGUID = doc.GetFingerprintGUID();
          UniqueID = reference.ConvertToStableRepresentation(doc);

          referenceDocument = doc;
          this.reference = reference;

          Document = reference.LinkedElementId == ARDB.ElementId.InvalidElementId ? referenceDocument :
            referenceDocument.GetElement<ARDB.RevitLinkInstance>(reference.ElementId)?.GetLinkDocument();

          return;
        }
        catch (Autodesk.Revit.Exceptions.InvalidObjectException) { }
      }

      Document = default;
      this.reference = default;
      referenceDocument = null;
      UniqueID = string.Empty;
      DocumentGUID = Guid.Empty;
    }

    /// <summary>
    /// Accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for computation.
    /// </summary>
    public virtual BoundingBox BoundingBox => GetBoundingBox(Transform.Identity);

    /// <summary>
    /// Not necessarily accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for display.
    /// </summary>
    public virtual BoundingBox ClippingBox => BoundingBox;

    public virtual ARDB.Reference GetDefaultReference() => reference;

    #region DocumentObject
    public override string DisplayName => GetType().GetCustomAttribute<NameAttribute>().Name;
    #endregion

    #region ReferenceObject
    public override bool? IsEditable => Value?.IsReadOnly;
    #endregion
  }

  [Name("Element")]
  public class GeometryElement : GeometryObject, IGH_PreviewData
  {
    public new ARDB.GeometryElement Value => base.Value as ARDB.GeometryElement;
    public override ARDB.Reference GetDefaultReference()
    {
      return Document?.GetElement(Id)?.
        GetDefaultReference()?.
        CreateGeometryLinkReference(ReferenceDocument, ReferenceId, Document);
    }

    public GeometryElement() { }
    public GeometryElement(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GeometryElement)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }

      return base.CastTo(out target);
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case ARDB.Element element:
          if (element.GetDefaultReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
          {
            SetValue(element.Document, reference);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return Value?.GetBoundingBox()?.ToBox().GetBoundingBox(xform) ?? NaN.BoundingBox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid) return;

      var bbox = ClippingBox;
      if (!bbox.IsValid)
        return;

      args.Pipeline.DrawBoxCorners(bbox, args.Color);
    }
    #endregion
  }

  [Name("Point")]
  public class GeometryPoint : GeometryObject, IGH_PreviewData
  {
    public new ARDB.Point Value
    {
      get
      {
        if (Reference is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR)
        {
          var uniqueId = reference.ConvertToStableRepresentation(Document);
          int end = -1;
          if (uniqueId.EndsWith("/0")) end = 0;
          else if (uniqueId.EndsWith("/1")) end = 1;

          if (end == 0 || end == 1)
          {
            var curve = default(ARDB.Curve);
            switch (base.Value)
            {
              case ARDB.Edge e: curve = e.AsCurve(); break;
              case ARDB.Curve c: curve = c; break;
            }

            if (curve is object && curve.IsBound)
              return ARDB.Point.Create(curve.GetEndPoint(end));
          }
        }

        return base.Value as ARDB.Point;
      }
    }
    public override object ScriptVariable() => Value;

    public GeometryPoint() { }
    public GeometryPoint(ARDB.Document document, ARDB.XYZ xyz) : base(document, ARDB.Point.Create(xyz)) { }
    public GeometryPoint(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public sealed override string ToString()
    {
      return IsReferencedData ? base.ToString() :
        Point is Point point ? GH_Format.FormatPoint(point.Location) :
        "Invalid Point";
    }

    public static new GeometryPoint FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      if (document.GetElement(reference) is ARDB.Element element && element.GetGeometryObjectFromReference(reference, out var transform) is ARDB.GeometryObject geometry)
      {
        using (geometry)
        {
          if (reference.GlobalPoint is object)
          {
            switch (geometry)
            {
              case ARDB.Edge edge:
              {
                using (var worldCurve = edge.AsCurve().CreateTransformed(transform))
                {
                  var result = worldCurve.Project(reference.GlobalPoint);
                  var points = new ARDB.XYZ[] { worldCurve.GetEndPoint(0), worldCurve.GetEndPoint(1) };
                  int end = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

                  var stable = reference.ConvertToStableRepresentation(document);
                  reference = ARDB.Reference.ParseFromStableRepresentation(document, $"{stable}/{end}");
                  return new GeometryPoint(document, reference);
                }
              }

              case ARDB.Curve curve:
              {
                using (var worldCurve = curve.CreateTransformed(transform))
                {
                  var result = worldCurve.Project(reference.GlobalPoint);
                  var points = new ARDB.XYZ[] { worldCurve.GetEndPoint(0), worldCurve.GetEndPoint(1) };
                  int end = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

                  var stable = reference.ConvertToStableRepresentation(document);
                  reference = ARDB.Reference.ParseFromStableRepresentation(document, $"{stable}/{end}");
                  return new GeometryPoint(document, reference);
                }
              }
            }

            return new GeometryPoint(document, reference.GlobalPoint);
          }
          else if (geometry.TryGetLocation(out var location, out var _, out var _))
          {
            return new GeometryPoint(document, location);
          }
        }
      }

      return null;
    }

    Point Point
    {
      get
      {
        if (base.point is null && Value is ARDB.Point point)
        {
          base.point = new Point(point.Coord.ToPoint3d());

          if (HasTransform)
            base.point.Transform(Transform);
        }

        return base.point;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return Point is Point point ?
      (
        xform == Transform.Identity ?
        point.GetBoundingBox(true) :
        point.GetBoundingBox(xform)
      ) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Point is Point point)
        args.Pipeline.DrawPoint(point.Location, CentralSettings.PreviewPointStyle, CentralSettings.PreviewPointRadius, args.Color);
    }
    #endregion

    #region Casting
    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Point)))
      {
        target = (Q) (object) (base.IsValid ? base.Value : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        if (Point is Point point)
        {
          target = (Q) (object) new GH_Point(point.Location);
          return true;
        }

        target = default;
        return false;
      }

      return base.CastTo(out target);
    }
    #endregion
  }

  [Name("Curve")]
  public class GeometryCurve : GeometryObject, IGH_PreviewData, IGH_Goo
  {
    public new ARDB.Curve Value
    {
      get
      {
        switch (base.Value)
        {
          case ARDB.Curve c: return c;
          case ARDB.Edge e: return e.AsCurve();
        }
        return default;
      }
    }

    public GeometryCurve() { }
    public GeometryCurve(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public static new GeometryCurve FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      return reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR ?
        new GeometryCurve(document, reference) : null;
    }

    public Curve Curve
    {
      get
      {
        if (Value is ARDB.Curve curve && wires is null)
        {
          wires = new Curve[] { curve.ToCurve() };

          if (HasTransform)
            wires[0].Transform(Transform);
        }

        return wires?.FirstOrDefault();
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return Curve is Curve curve ?
      (
        xform == Transform.Identity ?
        curve.GetBoundingBox(true) :
        curve.GetBoundingBox(xform)
      ) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }
    #endregion

    #region Properties
    public override string DisplayName
    {
      get
      {
        var value = base.Value;
        string visibility;
        switch (value?.Visibility)
        {
          case null:                    visibility = string.Empty; break;
          case ARDB.Visibility.Visible: visibility = string.Empty; break;
          default:                      visibility = $"{value.Visibility} "; break;
        }

        var typeName = base.DisplayName;
        if (value is ARDB.Edge edge)
        {
          typeName = "Edge";
          value = edge.AsCurve();
        }

        switch (value)
        {
          case null:                    return $"Null {typeName}";
          case ARDB.Arc _:              return $"{visibility}Arc {typeName}";
          case ARDB.CylindricalHelix _: return $"{visibility}Helix {typeName}";
          case ARDB.Ellipse _:          return $"{visibility}Ellipse {typeName}";
          case ARDB.HermiteSpline _:    return $"{visibility}Hermite {typeName}";
          case ARDB.Line _:             return $"{visibility}Line {typeName}";
          case ARDB.NurbSpline _:       return $"{visibility}NURBS {typeName}";
          case ARDB.Curve _:            return $"{visibility}Unknown {typeName}";
          default:                      return "Curve";
        }
      }
    }
    #endregion

    #region Casting
    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Curve)))
      {
        target = (Q) (object) (base.Value as ARDB.Curve);
        return true;
      }
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Edge)))
      {
        target = (Q) (object) (base.Value as ARDB.Edge);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
      {
        if (Curve is LineCurve curve)
        {
          target = (Q) (object) new GH_Line(curve.Line);
          return true;
        }

        target = default;
        return false;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Arc)))
      {
        if (Curve is ArcCurve curve)
        {
          target = (Q) (object) new GH_Arc(curve.Arc);
          return true;
        }

        target = default;
        return false;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Circle)))
      {
        if (Curve is ArcCurve curve && curve.IsCompleteCircle)
        {
          target = (Q) (object) new GH_Circle(new Circle(curve.Arc.Plane, curve.Arc.Radius));
          return true;
        }

        target = default;
        return false;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Curve))) 
      {
        if (Curve is Curve curve)
        {
          target = (Q) (object) new GH_Curve(curve);
          return true;
        }

        target = default;
        return false;
      }

      return base.CastTo(out target);
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case ARDB.Element element:
          if (element.GetDefaultReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR)
          {
            SetValue(element.Document, reference);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }
    #endregion
  }

  [Name("Face")]
  public class GeometryFace : GeometryObject, IGH_PreviewData
  {
    public new ARDB.Face Value => base.Value as ARDB.Face;

    public GeometryFace() { }
    public GeometryFace(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return Value?.ToBrep() is Brep brep ?
      (
        xform == Transform.Identity ?
        brep.GetBoundingBox(true) :
        brep.GetBoundingBox(xform)
      ) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    protected Curve[] Edges
    {
      get
      {
        if (ClippingBox.IsValid && Value is ARDB.Face face && wires is null)
        {
          wires = face.GetEdgesAsCurveLoops().SelectMany(x => x.GetPreviewWires()).ToArray();

          if (HasTransform)
          {
            foreach (var wire in wires)
              wire.Transform(Transform);
          }
        }

        return wires;
      }
    }

    protected Mesh[] GetPreviewMeshes(MeshingParameters meshingParameters)
    {
      if (meshingParameters.LevelOfDetail() != levelOfDetail)
      {
        levelOfDetail = meshingParameters.LevelOfDetail();
        if (ClippingBox.IsValid && Value is ARDB.Face face)
        {
          meshes = Enumerable.Repeat(face, 1).GetPreviewMeshes(Document, meshingParameters).ToArray();

          var transform = Transform;
          foreach (var mesh in meshes)
          {
            if (HasTransform) mesh.Transform(transform);
            mesh.Normals.ComputeNormals();
          }
        }
      }

      return meshes;
    }

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Edges is Curve[] curves)
      {
        foreach (var curve in curves)
          args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
      }
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (GetPreviewMeshes(args.MeshingParameters) is Mesh[] meshes)
      {
        foreach (var mesh in meshes)
          args.Pipeline.DrawMeshShaded(mesh, args.Material);
      }
    }
    #endregion

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(ARDB.Face)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is ARDB.Face face)
      {
        var element = Reference is object ? Document?.GetElement(Reference) : null;

        if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
        {
          if (face.ToBrep() is Brep brep)
          {
            if (HasTransform) brep.Transform(Transform);
            target = (Q) (object) new GH_Surface(brep.Surfaces.FirstOrDefault());
          }
          else target = default;
          return true;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
        {
          if (face.ToBrep() is Brep brep)
          {
            if (HasTransform) brep.Transform(Transform);
            target = (Q) (object) new GH_Brep(brep);
          }
          else target = default;
          return true;
        }
        if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
        {
          if (meshes is object)
          {
            var m = new Mesh(); m.Append(meshes);
            target = (Q) (object) new GH_Mesh(m);
          }
          else target = default;
          return true;
        }
        else if (element is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElement(element);
          return true;
        }
      }

      return base.CastTo(out target);
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case ARDB.Element element:
          if (element.GetDefaultReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE)
          {
            SetValue(element.Document, reference);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }

    #region Properties
    public override string DisplayName
    {
      get
      {
        var value = base.Value;
        string visibility;
        switch (value?.Visibility)
        {
          case null:                    visibility = string.Empty; break;
          case ARDB.Visibility.Visible: visibility = string.Empty; break;
          default:                      visibility = $"{value.Visibility} "; break;
        }

        switch (value)
        {
          case null:                    return "Null Face";
          case ARDB.ConicalFace _:      return $"{visibility}Conical Face";
          case ARDB.CylindricalFace _:  return $"{visibility}Cylindrical Face";
          case ARDB.HermiteFace _:      return $"{visibility}Hermite Face";
          case ARDB.PlanarFace _:       return $"{visibility}Planar Face";
          case ARDB.RevolvedFace _:     return $"{visibility}Revolved Face";
          case ARDB.RuledFace _:        return $"{visibility}Ruled Face";
          case ARDB.Face face:

#if REVIT_2021
          using(var surface = face.GetSurface())
          if (surface is ARDB.OffsetSurface) return $"{visibility}Offset Face";
#endif
                                        return $"{visibility}Unknown Face";
          default:                      return "Face";
        }
      }
    }
    #endregion
  }
}
