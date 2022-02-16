using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Units;
  using Convert.System.Drawing;
  using External.DB.Extensions;
  using System.Linq;

  [Kernel.Attributes.Name("Category")]
  public class Category : Element, Bake.IGH_BakeAwareElement
  {
    #region IGH_Goo
    public override bool IsValid => (Id?.TryGetBuiltInCategory(out var _) == true) || base.IsValid;
    protected override Type ValueType => typeof(ARDB.Category);
    public override object ScriptVariable() => APIObject;

    public sealed override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var categoryId = ARDB.ElementId.InvalidElementId;

      if (source is IGH_Goo goo)
      {
        if (source is CategoryId id)
        {
          source = (ARDB.BuiltInCategory) id.Value;
        }
        else source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int i:                    categoryId = new ARDB.ElementId(i); break;
        case ARDB.BuiltInCategory bic: categoryId = new ARDB.ElementId(bic); break;
        case ARDB.ElementId id:        categoryId = id; break;
        case ARDB.Category c:          SetValue(c.Document(), c.Id); return true;
        case ARDB.GraphicsStyle s:     SetValue(s.Document, s.GraphicsStyleCategory.Id); return true;
        case ARDB.Family f:            SetValue(f.Document, f.FamilyCategoryId); return true;
        case ARDB.Element e:
          if(e.Category is ARDB.Category category)
          {
            SetValue(e.Document, category.Id);
            return true;
          }
          break;
      }

      if (categoryId.TryGetBuiltInCategory(out var _))
      {
        SetValue(document, categoryId);
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Category)))
      {
        target = (Q) (object) APIObject;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(CategoryId)))
      {
        var categoryId = new CategoryId();
        if (APIObject.Id.TryGetBuiltInCategory(out var bic))
        {
          categoryId.Value = bic;
          target = (Q) (object) categoryId;
          return true;
        }
        else
        {
          target = (Q) (object) default(Q);
          return false;
        }
      }

      return base.CastTo(out target);
    }

    new class Proxy : Element.Proxy
    {
      protected new Category owner => base.owner as Category;

      public Proxy(Category c) : base(c) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => !owner.IsReferencedData || owner.Document is object;
      public override string FormatInstance()
      {
        if (owner.IsReferencedData && owner.IsReferencedDataLoaded)
          return owner.DisplayName;

        return base.FormatInstance();
      }
      public override bool FromString(string str)
      {
        var doc = owner.Document ?? Revit.ActiveUIDocument.Document;

        if (Enum.TryParse(str, out ARDB.BuiltInCategory builtInCategory) && builtInCategory.IsValid())
          owner.SetValue(doc, new ARDB.ElementId(builtInCategory));
        else if (str == string.Empty)
          owner.SetValue(default, new ARDB.ElementId(ARDB.BuiltInCategory.INVALID));
        else if (doc is object)
        {
          foreach (var category in doc.GetCategories())
          {
            if (category.FullName() == str)
            {
              owner.SetValue(doc, category.Id);
              break;
            }
          }
        }
        else
          return false;

        owner.UnloadReferencedData();
        return owner.LoadReferencedData();
      }

      #region Misc
      protected override bool IsValidId(ARDB.Document doc, ARDB.ElementId id) => id.IsCategoryId(doc);
      public override Type ObjectType => IsBuiltIn ? typeof(ARDB.BuiltInCategory) : base.ObjectType;

      [System.ComponentModel.Description("BuiltIn category Id.")]
      public ARDB.BuiltInCategory? BuiltInId => owner.Id.TryGetBuiltInCategory(out var bic) ? bic : default;

      [System.ComponentModel.Description("Forge schema Id.")]
      public DBXS.CategoryId ForgeTypeId => owner.Id.TryGetBuiltInCategory(out var bic) ? (DBXS.CategoryId) bic : default;
      #endregion

      #region Category
      const string Category = "Category";

      ARDB.Category category => owner.APIObject;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Parent category of this category.")]
      public string Parent => category?.Parent?.Name;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Category can have project parameters.")]
      public bool? AllowsParameters => category?.AllowsBoundParameters;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Identifies if the category is associated with a type of tag for a different category.")]
      public bool? IsTag => category?.IsTagCategory;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Material of the category.")]
      public string Material => category?.Material?.Name;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Identifies if elements of the category are able to report what materials they contain in what quantities.")]
      public bool? HasMaterialQuantities => category?.HasMaterialQuantities;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Category type of this category.")]
      public ARDB.CategoryType? CategoryType => category?.CategoryType;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Indicates if the category is cuttable or not.")]
      public bool? IsCuttable => category?.IsCuttable;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("The color of lines shown for elements of this category.")]
      public System.Drawing.Color LineColor => category?.LineColor.ToColor() ?? System.Drawing.Color.Empty;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region DocumentObject
    internal ARDB.Category APIObject => IsReferencedDataLoaded ? Document.GetCategory(Id) : default;

    protected override void ResetValue()
    {
      // Some categories are slow to found,
      // Category reference seem to be generated on demand and the reference become invalid "sudently".
      // so we can not catch the reference but the name is used many times in UI and needs to be fast.
      fullName = default;

      base.ResetValue();
    }

    public override string DisplayName
    {
      get
      {
        if (FullName is string full)
          return full;

        return base.DisplayName;
      }
    }
    #endregion

    public Category() : base() { }
    public Category(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Category(ARDB.Category value) : base(value.Document(), value?.Id ?? ARDB.ElementId.InvalidElementId) { }

    protected override bool SetValue(ARDB.Element element)
    {
      if (DocumentExtension.AsCategory(element) is ARDB.Category)
      {
        SetValue(element.Document, element.Id);
        return true;
      }

      return false;
    }

    public static Category FromCategory(ARDB.Category category)
    {
      if (category is null)
        return null;

      return new Category(category);
    }

    public static new Category FromElementId(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id.IsCategoryId(doc))
        return new Category(doc, id);

      return null;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    static readonly double[] PlotWeights = new double[]
    {
      0.0,
      0.003,
      0.003,
      0.003,
      0.004,
      0.006,
      0.009,
      0.013,
      0.018,
      0.025,
      0.035,
      0.050,
      0.065,
      0.085,
      0.110,
      0.150,
      0.200
    };

    static double ToPlotWeight(int? value)
    {
      if (!value.HasValue) return -1.0;

      if (0 < value.Value && value.Value < PlotWeights.Length)
        return UnitScale.Convert(PlotWeights[value.Value], UnitScale.Inches, UnitScale.Millimeters);

      return 0.0;
    }

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      const string RootLayerName = "Revit";
      var PS = Layer.PathSeparator;

      if (APIObject is ARDB.Category category)
      {
        var linetypeIndex = -1;
        if (ProjectionLinePattern is LinePatternElement linePattern)
        {
          if (linePattern.BakeElement(idMap, false, doc, att, out var linetypeGuid))
            linetypeIndex = doc.Linetypes.FindId(linetypeGuid).Index;
        }

        var materialIndex = Rhino.DocObjects.Material.DefaultMaterial.Index;
        if (Material is Material material)
        {
          if (material.BakeElement(idMap, false, doc, att, out var materialGuid))
            materialIndex = doc.Materials.FindId(materialGuid).Index;
        }

        var fullLayerName = category.Parent is null ?
          $"{RootLayerName}{PS}{category.CategoryType}{PS}{category.Name}" :
          $"{RootLayerName}{PS}{category.CategoryType}{PS}{category.Parent.Name}{PS}{category.Name}";

        // 2. Check if already exist
        var index = doc.Layers.FindByFullPath(fullLayerName, -1);
        var layer = index < 0 ?
          Layer.GetDefaultLayerProperties() :
          doc.Layers[index];

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (index < 0)
          {
            // Create Root Layer
            new Category(category.Parent).BakeElement(idMap, false, doc, att, out var parentGuid);

            // Create Category Type Layer
            if (category.Parent is null)
            {
              if (Types.CategoryType.NamedValues.TryGetValue((int) category.CategoryType, out var typeName))
              {
                var type = doc.Layers.FindByFullPath($"{RootLayerName}::{category.CategoryType}", -1);
                if (type < 0)
                {
                  var typeLayer = Layer.GetDefaultLayerProperties();
                  typeLayer.ParentLayerId = parentGuid;
                  typeLayer.Name = typeName;
                  type = doc.Layers.Add(typeLayer);
                }

                parentGuid = doc.Layers[type].Id;
              }
            }

            layer.ParentLayerId = parentGuid;
            layer.Name = category.Name;
            layer.IsExpanded = false;
          }

          layer.Color = category.LineColor.ToColor();
          layer.LinetypeIndex = linetypeIndex;
          layer.PlotWeight = ToPlotWeight(ProjectionLineWeight);

          if (category.CategoryType == ARDB.CategoryType.Annotation)
          {
            if
            (
              Id.TryGetBuiltInCategory(out var builtInCategory) &&
              builtInCategory == ARDB.BuiltInCategory.OST_Grids
            )
            {
              layer.Color = System.Drawing.Color.FromArgb(35, layer.Color);
              layer.IsLocked = true;
            }
          }
          else
          {
            layer.RenderMaterialIndex = materialIndex;

            // Special case for "Light Sources"
            if (category.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_LightingFixtureSource)
              layer.IsVisible = false;
          }

          if (index < 0) { index = doc.Layers.Add(layer); layer = doc.Layers[index]; }
          else if (overwrite) doc.Layers.Modify(layer, index, true);
        }

        idMap.Add(Id, guid = layer.Id);
        return true;
      }
      else
      {
        var layerIndex = doc.Layers.FindByFullPath(RootLayerName, -1);
        if (layerIndex < 0)
        {
          var layer = Layer.GetDefaultLayerProperties();
          {
            layer.Name = RootLayerName;
          }
          layerIndex = doc.Layers.Add(layer);
        }

        guid = doc.Layers[layerIndex].Id;
        return true;
      }
    }
    #endregion

    #region Properties

    public override string NextIncrementalNomen(string prefix)
    {
      if (APIObject is ARDB.Category category)
      {
        DocumentExtension.TryParseNomenId(prefix, out prefix, out var _);
        var nextName = category.Parent?.SubCategories.
          Cast<ARDB.Category>().
          Select(x => x.Name).
          WhereNomenPrefixedWith(prefix).
          NextNomenOrDefault() ?? $"{prefix} 1";

        return nextName;
      }

      return default;
    }

    public override string Nomen
    {
      get
      {
        if (FullName is string full)
        {
          var segments = full.Split('\\');
          return segments[segments.Length - 1];
        }

        return base.Nomen;
      }
      set
      {
        base.Nomen = value;
        fullName = null;
      }
    }

    string fullName;
    public string FullName
    {
      get
      {
        if (fullName is null && APIObject is ARDB.Category category)
          fullName = category.FullName();

        return fullName;
      }
    }

    public ARDB.CategoryType CategoryType => APIObject?.CategoryType ?? ARDB.CategoryType.Invalid;

    public bool? IsTagCategory => APIObject?.IsTagCategory;

    public System.Drawing.Color? LineColor
    {
      get => APIObject?.LineColor.ToColor();
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != category.LineColor)
              category.LineColor = color;
          }
        }
      }
    }

    public Material Material
    {
      get => APIObject is ARDB.Category category ? new Material(category.Material) : default;
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          AssertValidDocument(value, nameof(Material));
          if ((category.Material?.Id ?? ARDB.ElementId.InvalidElementId) != value.Id)
            category.Material = value.Value;
        }
      }
    }

    public int? ProjectionLineWeight
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle _)
            return category.GetLineWeight(ARDB.GraphicsStyleType.Projection);
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle _)
          {
            if (category.GetLineWeight(ARDB.GraphicsStyleType.Projection) != value)
              category.SetLineWeight(value.Value, ARDB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public int? CutLineWeight
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle _)
            return category.GetLineWeight(ARDB.GraphicsStyleType.Cut);
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle _)
          {
            if (category.GetLineWeight(ARDB.GraphicsStyleType.Cut) != value)
              category.SetLineWeight(value.Value, ARDB.GraphicsStyleType.Cut);
          }
        }
      }
    }

    public LinePatternElement ProjectionLinePattern
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(ARDB.GraphicsStyleType.Projection));
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          AssertValidDocument(value, nameof(ProjectionLinePattern));
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle)
          {
            if (category.GetLinePatternId(ARDB.GraphicsStyleType.Projection) != value.Id)
              category.SetLinePatternId(value.Id, ARDB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public LinePatternElement CutLinePattern
    {
      get
      {
        if (APIObject is ARDB.Category category)
        {
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(ARDB.GraphicsStyleType.Cut));
        }

        return default;
      }
      set
      {
        if (value is object && APIObject is ARDB.Category category)
        {
          AssertValidDocument(value, nameof(CutLinePattern));
          if (category.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut) is ARDB.GraphicsStyle)
          {
            if (category.GetLinePatternId(ARDB.GraphicsStyleType.Cut) != value.Id)
              category.SetLinePatternId(value.Id, ARDB.GraphicsStyleType.Cut);
          }
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Graphics Style")]
  public class GraphicsStyle : Element
  {
    protected override Type ValueType => typeof(ARDB.GraphicsStyle);
    public new ARDB.GraphicsStyle Value => base.Value as ARDB.GraphicsStyle;

    public GraphicsStyle() { }
    public GraphicsStyle(ARDB.GraphicsStyle graphicsStyle) : base(graphicsStyle) { }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.GraphicsStyle style)
        {
          var tip = string.Empty;
          if (style.GraphicsStyleCategory.Parent is ARDB.Category parent)
            tip = $"{parent.Name}\\";

          switch (style.GraphicsStyleType)
          {
            case ARDB.GraphicsStyleType.Projection: return $"{tip}{style.Name} [projection]";
            case ARDB.GraphicsStyleType.Cut:        return $"{tip}{style.Name} [cut]";
          }
        }

        return base.DisplayName;
      }
    }

    public override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      if (source is Category category)
      {
        if (category.APIObject.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection) is ARDB.GraphicsStyle style)
        {
          SetValue(style.Document, style.Id);
          return true;
        }
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GraphicsStyle)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo(out target);
    }
  }
}
