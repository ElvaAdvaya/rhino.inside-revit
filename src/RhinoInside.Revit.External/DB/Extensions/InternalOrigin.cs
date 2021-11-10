using Autodesk.Revit.DB;


namespace RhinoInside.Revit.External.DB.Extensions
{
#if !REVIT_2021
  using InternalOrigin = Autodesk.Revit.DB.BasePoint;
#endif

  public static class InternalOriginExtension
  {
#if REVIT_2021
    /// <summary>
    /// Gets the shared position of the InternalOrigin.
    /// </summary>
    /// <param name="basePoint"></param>
    /// <returns></returns>
    public static XYZ GetSharedPosition(this InternalOrigin basePoint) => basePoint.SharedPosition;

    /// <summary>
    /// Gets the position of the InternalOrigin.
    /// </summary>
    /// <param name="basePoint"></param>
    /// <returns></returns>
    public static XYZ GetPosition(this InternalOrigin basePoint) => basePoint.Position;
#endif

    /// <summary>
    /// Gets the project internal origin for the document.
    /// </summary>
    /// <param name="doc">The document from which to get the internal origin.</param>
    /// <returns>The project internal origin of the document.</returns>
    public static InternalOrigin Get(Document doc)
    {
#if REVIT_2021
      return InternalOrigin.Get(doc);
#else
      using (var collector = new FilteredElementCollector(doc))
      {
        return collector.
          OfClass(typeof(InternalOrigin)).
          OfCategoryId(new ElementId(BuiltInCategory.OST_IOS_GeoSite)).
          FirstElement() as InternalOrigin;
      }
#endif
    }
  }
}
