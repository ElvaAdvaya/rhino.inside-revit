using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class DocumentExtension
  {
    public static bool IsValid(this Document doc) => doc?.IsValidObject == true;

    public static bool Release(this Document doc)
    {
      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
      {
        if (uiDocument.GetOpenUIViews().Count == 0)
          return doc.Close(false);
      }

      return true;
    }

    public static string GetFilePath(this Document doc)
    {
      if (doc is null)
        return string.Empty;

      if (string.IsNullOrEmpty(doc.PathName))
        return (doc.Title + (doc.IsFamilyDocument ? ".rfa" : ".rvt"));

      return doc.PathName;
    }

    public static bool HasModelPath(this Document doc, ModelPath modelPath)
    {
#if REVIT_2020
      if (modelPath.CloudPath)
        return doc.IsModelInCloud && modelPath.Compare(doc.GetCloudModelPath()) == 0;
#endif

      if (modelPath.ServerPath)
        return doc.IsWorkshared && modelPath.Compare(doc.GetWorksharingCentralModelPath()) == 0;

      return modelPath.Compare(new FilePath(doc.PathName)) == 0;
    }

    public static Guid GetFingerprintGUID(this Document doc)
    {
      if (doc?.IsValidObject != true)
        return Guid.Empty;

      return ExportUtils.GetGBXMLDocumentId(doc);
    }

    private static int seed = 0;
    private static readonly Dictionary<Guid, int> DocumentsSessionDictionary = new Dictionary<Guid, int>();

    public static int DocumentSessionId(Guid key)
    {
      if (key == Guid.Empty)
        throw new ArgumentException("Invalid argument value", nameof(key));

      if (DocumentsSessionDictionary.TryGetValue(key, out var value))
        return value;

      DocumentsSessionDictionary.Add(key, ++seed);
      return seed;
    }

    internal static bool TryGetDocument(this IEnumerable<Document> set, Guid guid, out Document document, Document activeDBDocument)
    {
      if (guid != Guid.Empty)
      {
        // For performance reasons and also in case of conflict the ActiveDBDocument will have priority
        if (activeDBDocument is object && ExportUtils.GetGBXMLDocumentId(activeDBDocument) == guid)
        {
          document = activeDBDocument;
          return true;
        }

        foreach (var doc in set.Where(x => ExportUtils.GetGBXMLDocumentId(x) == guid))
        {
          document = doc;
          return true;
        }
      }

      document = default;
      return false;
    }

    public static bool TryGetCategoryId(this Document doc, string uniqueId, out ElementId categoryId)
    {
      categoryId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        if (EpisodeId == Guid.Empty)
        {
          if (((BuiltInCategory) id).IsValid())
            categoryId = new ElementId((BuiltInCategory) id);
        }
        else
        {
          if (doc.GetElement(uniqueId) is Element category)
          {
            try { categoryId = Category.GetCategory(doc, category.Id)?.Id; }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

            if (!categoryId.IsValid())
            {
              var dependents = category.GetDependentElements(new ElementClassFilter(typeof(GraphicsStyle)));
              if (dependents.FirstOrDefault() is ElementId styleId)
              {
                categoryId = category.Document.GetElement<GraphicsStyle>(styleId)?.
                  GraphicsStyleCategory.Id;

                if (category.Id != categoryId)
                  categoryId = default;
              }
            }
          }
        }
      }

      return categoryId is object;
    }

    public static bool TryGetParameterId(this Document doc, string uniqueId, out ElementId parameterId)
    {
      parameterId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        if (EpisodeId == Guid.Empty)
        {
          if (((BuiltInParameter) id).IsValid())
            parameterId = new ElementId((BuiltInParameter) id);
        }
        else
        {
          if (doc.GetElement(uniqueId) is ParameterElement parameter)
            parameterId = parameter.Id;
        }
      }

      return parameterId is object;
    }

    public static bool TryGetLinePatternId(this Document doc, string uniqueId, out ElementId patternId)
    {
      patternId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        if (EpisodeId == Guid.Empty)
        {
          if (id == -3000010)
            patternId = new ElementId(id);
        }
        else
        {
          if (doc.GetElement(uniqueId) is LinePatternElement pattern)
            patternId = pattern.Id;
        }
      }

      return patternId is object;
    }

    public static bool TryGetElementId(this Document doc, string uniqueId, out ElementId elementId)
    {
      elementId = default;

      try
      {
        if (Reference.ParseFromStableRepresentation(doc, uniqueId) is Reference reference)
          elementId = reference.ElementId;
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException) { }

      return elementId is object;
    }

    /// <summary>
    /// Compare two <see cref="Autodesk.Revit.DB.Reference"/> objects to know it are referencing same <see cref="Autodesk.Revit.DB.Element"/>
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>true if both references are equivalent</returns>
    public static bool AreEquivalentReferences(this Document doc, Reference x, Reference y)
    {
      var UniqueIdX = x?.ConvertToStableRepresentation(doc);
      var UniqueIdY = y?.ConvertToStableRepresentation(doc);

      return UniqueIdX == UniqueIdY;
    }

    public static T GetElement<T>(this Document doc, ElementId elementId) where T : Element
    {
      return doc.GetElement(elementId) as T;
    }

    public static T GetElement<T>(this Document doc, Reference reference) where T : Element
    {
      return doc.GetElement(reference) as T;
    }

    internal static Category AsCategory(Element element)
    {
      if (element?.GetType() == typeof(Element))
      {
        // 1. We try with the regular way calling Category.GetCategory
        try
        {
          if (Category.GetCategory(element.Document, element.Id) is Category category)
            return category;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        // 2. Try looking for any GraphicsStyle that points to the Category we are looking for.
        if (element.GetFirstDependent<GraphicsStyle>() is GraphicsStyle style)
        {
          if (style.GraphicsStyleCategory.Id == element.Id)
            return style.GraphicsStyleCategory;
        }
      }

      return null;
    }

    public static Category GetCategory(this Document doc, string uniqueId)
    {
      if (doc is null || string.IsNullOrEmpty(uniqueId))
        return null;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        return (EpisodeId == Guid.Empty) ?
          GetCategory(doc, (BuiltInCategory) id) :
          AsCategory(doc.GetElement(uniqueId));
      }

      return null;
    }

    public static Category GetCategory(this Document doc, BuiltInCategory categoryId)
    {
      if (doc is null || categoryId == BuiltInCategory.INVALID)
        return null;

      // 1. We try with the regular way calling Category.GetCategory
      try
      {
        if (Category.GetCategory(doc, categoryId) is Category category)
          return category;
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

      // 2. Try if there is any Element in doc that belongs to that Category.
      {
        using (var collector = new FilteredElementCollector(doc).OfCategory(categoryId))
        {
          if(collector.FirstElement() is Element element)
            return element.Category;
        }
      }

      // 3. Try looking for any GraphicsStyle that points to the Category we are looking for.
      {
        using (var collector = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)))
        {
          foreach (var style in collector.Cast<GraphicsStyle>())
          {
            var category = style.GraphicsStyleCategory;
            if (category.Id.IntegerValue == (int) categoryId)
              return style.GraphicsStyleCategory;
          }
        }
      }

      return default;
    }

    public static Category GetCategory(this Document doc, ElementId id)
    {
      if (doc is null || !id.IsValid())
        return null;

      return id.TryGetBuiltInCategory(out var categoryId) ?
        GetCategory(doc, categoryId):
        AsCategory(doc.GetElement(id));
    }

    static BuiltInCategory[] BuiltInCategoriesWithParameters;
    static Document BuiltInCategoriesWithParametersDocument;
    internal static IReadOnlyCollection<BuiltInCategory> GetBuiltInCategoriesWithParameters(this Document doc)
    {
      if (BuiltInCategoriesWithParameters is null || !doc.Equals(BuiltInCategoriesWithParametersDocument))
      {
        BuiltInCategoriesWithParametersDocument = doc;
        BuiltInCategoriesWithParameters = BuiltInCategoryExtension.BuiltInCategories.
          Where
          (
            bic =>
            {
              try { return Category.GetCategory(doc, bic)?.AllowsBoundParameters == true; }
              catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
            }
          ).
          ToArray();
      }

      return BuiltInCategoriesWithParameters;
    }

    public static Level FindLevelByElevation(this Document doc, double elevation)
    {
      Level level = null;

      if (!double.IsNaN(elevation))
      {
        var min = double.PositiveInfinity;
        using (var collector = new FilteredElementCollector(doc))
        {
          foreach (var levelN in collector.OfClass(typeof(Level)).Cast<Level>().OrderBy(c => c.Elevation))
          {
            var distance = Math.Abs(levelN.Elevation - elevation);
            if (distance < min)
            {
              level = levelN;
              min = distance;
            }
          }
        }
      }

      return level;
    }

    public static Level FindBaseLevelByElevation(this Document doc, double elevation, out Level topLevel)
    {
      elevation += Revit.ShortCurveTolerance;

      topLevel = null;
      Level level = null;
      using (var collector = new FilteredElementCollector(doc))
      {
        foreach (var levelN in collector.OfClass(typeof(Level)).Cast<Level>().OrderBy(c => c.Elevation))
        {
          if (levelN.Elevation <= elevation) level = levelN;
          else
          {
            topLevel = levelN;
            break;
          }
        }
      }
      return level;
    }

    public static Level FindTopLevelByElevation(this Document doc, double elevation, out Level baseLevel)
    {
      elevation -= Revit.ShortCurveTolerance;

      baseLevel = null;
      Level level = null;
      using (var collector = new FilteredElementCollector(doc))
      {
        foreach (var levelN in collector.OfClass(typeof(Level)).Cast<Level>().OrderByDescending(c => c.Elevation))
        {
          if (levelN.Elevation >= elevation) level = levelN;
          else
          {
            baseLevel = levelN;
            break;
          }
        }
      }
      return level;
    }

    /// <summary>
    /// Gets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active graphical <see cref="Autodesk.Revit.DB.View"/></returns>
    public static View GetActiveGraphicalView(this Document doc) => Rhinoceros.InvokeInHostContext(() =>
    {
      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
      {
        var activeView = uiDocument.ActiveGraphicalView;

        if (activeView is null)
        {
          var openViews = uiDocument.GetOpenUIViews().
              Select(x => doc.GetElement(x.ViewId) as View).
              Where(x => x.ViewType.IsGraphicalViewType());

          activeView = openViews.FirstOrDefault();
        }

        return activeView;
      }
    });

    /// <summary>
    /// Gets the active <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active <see cref="Autodesk.Revit.DB.View"/></returns>
    public static View GetActiveView(this Document doc) => Rhinoceros.InvokeInHostContext(() =>
    {
      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
      {
        var activeView = uiDocument.ActiveView;

        if (activeView is null)
        {
          var openViews = uiDocument.GetOpenUIViews().
              Select(x => doc.GetElement(x.ViewId) as View);

          activeView = openViews.FirstOrDefault();
        }

        return activeView;
      }
    });

    static readonly Guid PurgePerformanceAdviserRuleId = new Guid("E8C63650-70B7-435A-9010-EC97660C1BDA");
    public static bool GetPurgableElementTypes(this Document doc, out ICollection<ElementId> purgableTypeIds)
    {
      try
      {
        using (var adviser = PerformanceAdviser.GetPerformanceAdviser())
        {
          var rules = adviser.GetAllRuleIds().Where(x => x.Guid == PurgePerformanceAdviserRuleId).ToList();
          if (rules.Count > 0)
          {
            var results = adviser.ExecuteRules(doc, rules);
            if (results.Count > 0)
            {
              purgableTypeIds = new HashSet<ElementId>(results[0].GetFailingElements());
              return true;
            }
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.InternalException) { }

      purgableTypeIds = default;
      return false;
    }
  }
}
