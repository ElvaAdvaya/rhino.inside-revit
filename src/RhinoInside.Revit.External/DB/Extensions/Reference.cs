using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal static class ReferenceEqualityComparer
  {
    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Reference"/>
    /// that assumes all references are from the same <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static IEqualityComparer<Reference> SameDocument(Document document) => new EqualityComparer(document);

    struct EqualityComparer : IEqualityComparer<Reference>
    {
      readonly Document Document;
      public EqualityComparer(Document doc) => Document = doc;
      bool IEqualityComparer<Reference>.Equals(Reference x, Reference y) => IsEquivalent(x, y, Document);
      int IEqualityComparer<Reference>.GetHashCode(Reference obj)
      {
        int hashCode = 2022825623;
        hashCode = hashCode * -1521134295 + (int) obj.ElementReferenceType;
        hashCode = hashCode * -1521134295 + obj.LinkedElementId.GetHashCode();
        hashCode = hashCode * -1521134295 + obj.ElementId.GetHashCode();
        return hashCode;
      }
    }

    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.Reference"/> equals
    /// to this <see cref="Autodesk.Revit.DB.Reference"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.Reference"/> instances are considered equivalent
    /// if their stable representation are equal.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this Reference self, Reference other, Document document)
    {
      if (ReferenceEquals(self, other)) return true;
      if (self is null || other is null) return false;

      if (self.ElementReferenceType != other.ElementReferenceType) return false;
      if (self.ElementId != other.ElementId) return false;
      if (self.LinkedElementId != other.LinkedElementId) return false;

// TODO : Test if it validates the document and is faster.
//#if REVIT_2018
//      return self.EqualTo(other);
//#else
      return self.ConvertToStableRepresentation(document) == other.ConvertToStableRepresentation(document);
//#endif
    }
  }

  public static class ReferenceExtension
  {
    public static Reference CreateGeometryLinkReference(this Reference reference, Document document, ElementId linkInstanceId, Document linkedDocument)
    {
      if (reference is null) throw new ArgumentNullException(nameof(reference));

      if (!linkedDocument.IsValid() || !linkInstanceId.IsValid()) return null;
      if (document.IsEquivalent(linkedDocument)) return reference;

      var stable = reference.ConvertToStableRepresentation(linkedDocument);

      var referenceId = ReferenceId.Parse(stable, linkedDocument);
      referenceId = new ReferenceId
      (
        new GeometryObjectId(linkInstanceId.ToValue(), 0, GeometryObjectType.RVTLINK),
        referenceId.Element,
        referenceId.Symbol
      );
      stable = referenceId.ToString(document);

      return Reference.ParseFromStableRepresentation(document, stable);
    }
  }
}
