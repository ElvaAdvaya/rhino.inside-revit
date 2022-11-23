using System;
using System.Drawing;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  /// <summary>
  /// Interface to implement into classes that has a stable <see cref="ARDB.Reference"/>.
  /// For example: <see cref="ARDB.Element"/>, <see cref="ARDB.GeometryObject"/>
  /// </summary>
  public interface IGH_Reference : IGH_ReferenceObject
  {
    ARDB.ElementId Id { get; }

    ARDB.Reference GetReference();
    ARDB.Document ReferenceDocument {get;}
    ARDB.ElementId ReferenceId { get; }
  }

  public abstract class Reference : ReferenceObject,
    IGH_Reference,
    IGH_ItemDescription
  {
    #region System.Object
    public override string ToString()
    {
      var valid = IsValid;
      string Invalid = Id == ARDB.ElementId.InvalidElementId ?
        (string.IsNullOrWhiteSpace(ReferenceUniqueId) ? string.Empty : "Unresolved ") :
        valid ? string.Empty :
        (IsReferencedData ? "❌ Deleted " : "⚠ Invalid ");
      string TypeName = ((IGH_Goo) this).TypeName;
      string InstanceName = DisplayName;

      if (!string.IsNullOrWhiteSpace(InstanceName))
        InstanceName = $" : {InstanceName}";

      if (!IsReferencedData)
        return $"{Invalid}{TypeName}{InstanceName}";

      string InstanceId = Id is null ?
        $" : {ReferenceUniqueId}" :
        IsLinked ?
        $" : id {ReferenceId.ToValue()}:{Id.ToValue()}" :
        $" : id {Id.ToValue()}";

      if (ReferenceDocument is ARDB.Document && Document is ARDB.Document document)
      {
        if (document.IsLinked || document.IsFamilyDocument)
          InstanceId = $"{InstanceId} @ {document.GetTitle()}";
      }
      else InstanceId = $"{InstanceId} @ {ReferenceDocumentId:B}";

      if (IsLinked) TypeName = "Linked " + TypeName;
      return $"{Invalid}{TypeName}{InstanceName}{InstanceId}";
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && ReferenceDocument is object && Id.IsValid();
    public override string IsValidWhyNot
    {
      get
      {
        if (ReferenceDocumentId == Guid.Empty) return $"Reference Document Id '{Guid.Empty}' is invalid";
        if (!External.DB.UniqueId.TryParse(ReferenceUniqueId, out var _, out var _)) return $"Reference Unique Id '{ReferenceUniqueId}' is invalid";

        if (ReferenceDocument is null)
        {
          return $"Referenced Revit document '{ReferenceDocumentId}' was closed.";
        }
        else if (Document is null)
        {
          return "Referenced Revit linked document is not loaded.";
        }
        else
        {
          var id = Id;
          if (id is null) return $"Referenced Revit element '{ReferenceUniqueId}' is not available.";
          if (id == ARDB.ElementId.InvalidElementId) return "Id is equal to InvalidElementId.";
        }

        return default;
      }
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.ElementId)))
      {
        target = (Q) (object) Id;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(Id.IntegerValue);
        return true;
      }

      target = default;
      return false;
    }
    #endregion

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetImage(Size size) => default;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.NickName => $"{{{Id?.ToString()}}}";
    string IGH_ItemDescription.Description => Document?.GetTitle();
    #endregion

    #region IGH_Reference
    public abstract ARDB.ElementId Id { get; }

    public abstract ARDB.Reference GetReference();
    public abstract ARDB.Document ReferenceDocument { get; }
    public abstract ARDB.ElementId ReferenceId { get; }

    public bool IsLinked => ReferenceDocument is object && !ReferenceDocument.IsEquivalent(Document);
    #endregion

    public Reference() { }

    protected Reference(ARDB.Document doc, object value) : base(doc, value) { }

    protected ARDB.Reference GetReference(ARDB.Reference reference)
    {
      if (reference.LinkedElementId == ARDB.ElementId.InvalidElementId)
      {
        if (reference.ElementId != Id)
          throw new ArgumentException("Invalid Reference", nameof(reference));

        if (IsLinked)
          return reference.CreateLinkReference(ReferenceDocument, ReferenceId, Document);
      }
      else
      {
        if (reference.ElementId != ReferenceId || reference.LinkedElementId != Id)
          throw new ArgumentException("Invalid Reference", nameof(reference));
      }

      return reference;
    }

    internal T GetElementFromReference<T>(ARDB.Reference reference) where T : Element
    {
      if (reference.ElementReferenceType != ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
        throw new ArgumentException("Invalid ElementReferenceType", nameof(reference));

      return Element.FromReference(ReferenceDocument, GetReference(reference)) as T;
    }

    internal T GetGeometryObjectFromReference<T>(ARDB.Reference reference) where T : GeometryObject
    {
      return GeometryObject.FromReference(ReferenceDocument, GetReference(reference)) as T;
    }
  }
}