using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using External.DB;
  using External.DB.Extensions;

  public abstract class ElementCollectorComponent : ZuiComponent
  {
    protected ElementCollectorComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected virtual ARDB.ElementFilter ElementFilter { get; } = default;
    public override bool NeedsToBeExpired
    (
      ARDB.Document document,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
    )
    {
      // Check if the change is on a document this component is querying.
      if (!MayNeedToBeExpired(document))
        return false;

      // Check inputs with persistent data
      if (base.NeedsToBeExpired(document, added, deleted, modified))
        return true;

      // Check if any element may pass the filter
      return MayNeedToBeExpired(document, added, deleted, modified);
    }

    protected bool MayNeedToBeExpired(ARDB.Document document)
    {
      if (Params.Input<Parameters.Document>("Document") is Parameters.Document Document)
        return Document.VolatileData.AllData(true).Cast<Types.Document>().Select(x => x.Value).Contains(document);

      if (Parameters.Document.TryGetCurrentDocument(this, out var currentDocument))
        return document.Equals(currentDocument.Value);

      return false;
    }

    protected virtual bool MayNeedToBeExpired
    (
      ARDB.Document document,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
    )
    {
      var elementFilter = ElementFilter;
      var _Filter_ = Params.IndexOfInputParam("Filter");
      var filters = _Filter_ < 0 ?
                    Enumerable.Empty<ARDB.ElementFilter>() :
                    Params.Input[_Filter_].VolatileData.AllData(true).
                    OfType<Types.ElementFilter>().
                    Select(x => CompoundElementFilter.Intersect(elementFilter, x.Value));

      foreach (var filter in filters.Any() ? filters : new ARDB.ElementFilter[] { elementFilter })
      {
        if (added.Any(x => filter.PassesFilter(document, x)))
          return true;

        if (modified.Any(x => filter.PassesFilter(document, x)))
          return true;

        if (deleted.Count > 0)
        {
          foreach (var param in Params.Output.OfType<Kernel.IGH_ReferenceParam>())
          {
            if (param.NeedsToBeExpired(document, ElementIdExtension.EmptySet, deleted, ElementIdExtension.EmptySet))
              return true;
          }
        }
      }

      return false;
    }

    protected static bool TryGetFilterIntegerParam(ARDB.BuiltInParameter paramId, int pattern, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterIntegerRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterDoubleParam(ARDB.BuiltInParameter paramId, double pattern, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterDoubleRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern,
        1e-6
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterDoubleParam(ARDB.BuiltInParameter paramId, double pattern, double tolerance, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterDoubleRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern,
        tolerance
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }

    protected internal static bool TryGetFilterStringParam(ARDB.BuiltInParameter paramId, ref string pattern, out ARDB.ElementFilter filter)
    {
      if (pattern is string subPattern)
      {
        var inverted = false;
        var method = Operator.CompareMethodFromPattern(ref subPattern, ref inverted);
        if (Operator.CompareMethod.Nothing < method && method < Operator.CompareMethod.Wildcard)
        {
          var evaluator = default(ARDB.FilterStringRuleEvaluator);
          switch (method)
          {
            case Operator.CompareMethod.Equals: evaluator = new ARDB.FilterStringEquals(); break;
            case Operator.CompareMethod.StartsWith: evaluator = new ARDB.FilterStringBeginsWith(); break;
            case Operator.CompareMethod.EndsWith: evaluator = new ARDB.FilterStringEndsWith(); break;
            case Operator.CompareMethod.Contains: evaluator = new ARDB.FilterStringContains(); break;
          }

          var rule = CompoundElementFilter.FilterStringRule
          (
            new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
            evaluator,
            subPattern
          );

          filter = new ARDB.ElementParameterFilter(rule, inverted);
          return true;
        }
      }

      filter = default;
      return false;
    }

    protected static bool TryGetFilterElementIdParam(ARDB.BuiltInParameter paramId, ARDB.ElementId pattern, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterElementIdRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }
  }
}
