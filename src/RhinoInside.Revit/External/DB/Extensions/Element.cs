using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementExtension
  {
    public static GeometryElement GetGeometry(this Element element, ViewDetailLevel viewDetailLevel, out Options options)
    {
      options = new Options { ComputeReferences = true, DetailLevel = viewDetailLevel };
      var geometry = element.get_Geometry(options);

      if (!(geometry?.Any() ?? false) && element is GenericForm form && !form.Combinations.IsEmpty)
      {
        geometry.Dispose();

        options.IncludeNonVisibleObjects = true;
        return element.get_Geometry(options);
      }

      return geometry;
    }

#if !REVIT_2019
    public static IList<ElementId> GetDependentElements(this Element element, ElementFilter filter)
    {
      try
      {
        using (var transaction = new Transaction(element.Document, nameof(GetDependentElements)))
        {
          transaction.Start();

          var collection = element.Document.Delete(element.Id);
          if (filter is null)
            return collection?.ToList();

          return collection?.Where(x => filter.PassesFilter(element.Document, x)).ToList();
        }
      }
      catch { }

      return default;
    }
#endif

    #region Parameter
    public static IEnumerable<Parameter> GetParameters(this Element element, ParameterClass set)
    {
      switch (set)
      {
        case ParameterClass.Any:
          return Enum.GetValues(typeof(BuiltInParameter)).
            Cast<BuiltInParameter>().
            Select
            (
              x =>
              {
                try { return element.get_Parameter(x); }
                catch (Autodesk.Revit.Exceptions.InternalException) { return null; }
              }
            ).
            Where(x => x?.Definition is object).
            Union(element.Parameters.Cast<Parameter>().OrderBy(x => x.Id.IntegerValue)).
            GroupBy(x => x.Id).
            Select(x => x.First());
        case ParameterClass.BuiltIn:
          return Enum.GetValues(typeof(BuiltInParameter)).
            Cast<BuiltInParameter>().
            GroupBy(x => x).
            Select(x => x.First()).
            Select
            (
              x =>
              {
                try { return element.get_Parameter(x); }
                catch (Autodesk.Revit.Exceptions.InternalException) { return null; }
              }
            ).
            Where(x => x?.Definition is object);
        case ParameterClass.Project:
          return element.Parameters.Cast<Parameter>().
            Where(p => !p.IsShared && p.Id.IntegerValue > 0).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 1).
            OrderBy(x => x.Id.IntegerValue);
        case ParameterClass.Family:
          return element.Parameters.Cast<Parameter>().
            Where(p => !p.IsShared && p.Id.IntegerValue > 0).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 0).
            OrderBy(x => x.Id.IntegerValue);
        case ParameterClass.Shared:
          return element.Parameters.Cast<Parameter>().
            Where(p => p.IsShared).
            OrderBy(x => x.Id.IntegerValue);
      }

      return Enumerable.Empty<Parameter>();
    }

    public static Parameter GetParameter(this Element element, string name, ParameterClass set)
    {
      var parameters = element.
        GetParameters(set).
        Where(x => x.Definition.Name == name);

      return parameters.FirstOrDefault(x => !x.IsReadOnly) ?? parameters.FirstOrDefault();
    }

    public static Parameter GetParameter(this Element element, string name, ParameterType type, ParameterClass set)
    {
      var parameters = element.
        GetParameters(set).
        Where(x => x.Definition.ParameterType == type && x.Definition.Name == name);

      return parameters.FirstOrDefault(x => !x.IsReadOnly) ?? parameters.FirstOrDefault();
    }

    public static Parameter GetParameter(this Element element, string name, ParameterType type, ParameterBinding parameterBinding, ParameterClass set)
    {
      if (element is ElementType ? parameterBinding != ParameterBinding.Type : parameterBinding != ParameterBinding.Instance)
        return null;

      return GetParameter(element, name, type, set);
    }

    public static void CopyParametersFrom(this Element to, Element from, ICollection<BuiltInParameter> parametersMask = null)
    {
      if (ReferenceEquals(to, from) || from is null || to is null)
        return;

      if (!from.Document.Equals(to.Document))
        throw new System.InvalidOperationException();

      foreach (var previousParameter in from.GetParameters(ParameterClass.Any))
        using (previousParameter)
        using (var param = to.get_Parameter(previousParameter.Definition))
        {
          if (param is null || param.IsReadOnly)
            continue;

          if
          (
            parametersMask is object &&
            param.Definition is InternalDefinition internalDefinition &&
            parametersMask.Contains(internalDefinition.BuiltInParameter)
          )
            continue;

          switch (previousParameter.StorageType)
          {
            case StorageType.Integer: param.Set(previousParameter.AsInteger()); break;
            case StorageType.Double: param.Set(previousParameter.AsDouble()); break;
            case StorageType.String: param.Set(previousParameter.AsString()); break;
            case StorageType.ElementId: param.Set(previousParameter.AsElementId()); break;
          }
        }
    }

    #endregion
  }
}
