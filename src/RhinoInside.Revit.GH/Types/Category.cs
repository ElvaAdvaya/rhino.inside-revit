using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Category")]
  public class Category : Element, Bake.IGH_BakeAwareElement
  {
    #region IGH_Goo
    public override bool IsValid => (Id.TryGetBuiltInCategory(out var _) && true) || base.IsValid;
    protected override Type ScriptVariableType => typeof(DB.Category);
    override public object ScriptVariable() => Value;

    public override sealed bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var categoryId = DB.ElementId.InvalidElementId;

      if (source is ValueTuple<DB.Document, DB.ElementId> tuple)
      {
        (document, categoryId) = tuple;
      }
      else if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          document = element.Document;
          categoryId = element.Id;
        }
        else source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int i:               categoryId = new DB.ElementId(i); break;
        case DB.ElementId id:     categoryId = id; break;
        case DB.Category c:       SetValue(c.Document(), c.Id); return true;
        case DB.GraphicsStyle s:  SetValue(s.Document, s.GraphicsStyleCategory.Id); return true;
        case DB.Family f:         SetValue(f.Document, f.FamilyCategoryId); return true;
        case DB.Element e:        if(e.Category is DB.Category category)
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
      if (typeof(Q).IsAssignableFrom(typeof(DB.Category)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(out target);
    }

    new class Proxy : Element.Proxy
    {
      protected new Category owner => base.owner as Category;

      public Proxy(Category c) : base(c) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => !owner.IsReferencedElement || owner.Document is object;
      public override string FormatInstance()
      {
        if (owner.IsReferencedElement && owner.IsElementLoaded)
          return owner.DisplayName;

        return base.FormatInstance();
      }
      public override bool FromString(string str)
      {
        var doc = owner.Document ?? Revit.ActiveUIDocument.Document;

        if (Enum.TryParse(str, out DB.BuiltInCategory builtInCategory))
          owner.SetValue(doc, new DB.ElementId(builtInCategory));
        else if (str == string.Empty)
          owner.SetValue(default, new DB.ElementId(DB.BuiltInCategory.INVALID));
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

        owner.UnloadElement();
        return owner.LoadElement();
      }

      #region Misc
      protected override bool IsValidId(DB.Document doc, DB.ElementId id) => id.IsCategoryId(doc);
      public override Type ObjectType => IsBuiltIn ? typeof(DB.BuiltInCategory) : base.ObjectType;

      [System.ComponentModel.Description("BuiltIn category Id.")]
      public DB.BuiltInCategory? BuiltInId => owner.Id.TryGetBuiltInCategory(out var bic) ? bic : default;
      #endregion

      #region Category
      const string Category = "Category";

      DB.Category category => owner.Value;

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
      public DB.CategoryType? CategoryType => category?.CategoryType;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("Indicates if the category is cuttable or not.")]
      public bool? IsCuttable => category?.IsCuttable;

      [System.ComponentModel.Category(Category), System.ComponentModel.Description("The color of lines shown for elements of this category.")]
      public System.Drawing.Color LineColor => category?.LineColor.ToColor() ?? System.Drawing.Color.Empty;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region DocumentObject
    public new DB.Category Value => IsElementLoaded ? Document.GetCategory(Id) : default;

    public override string DisplayName
    {
      get
      {
        if (Value is DB.Category category)
          return category.FullName();

        return base.DisplayName;
      }
    }
    #endregion

    public Category() : base() { }
    public Category(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Category(DB.Category value) : base(value.Document(), value?.Id ?? DB.ElementId.InvalidElementId) { }

    protected override bool SetValue(DB.Element element)
    {
      if (DocumentExtension.AsCategory(element) is DB.Category cat)
      {
        SetValue(element.Document, element.Id);
        return true;
      }

      return false;
    }

    public static Category FromCategory(DB.Category category)
    {
      if (category is null)
        return null;

      return new Category(category);
    }

    new public static Category FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsCategoryId(doc))
        return new Category(doc, id);

      return null;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    static double ToPlotWeight(int? value)
    {
      if (!value.HasValue) return -1.0;

      var size = new double[]
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

      if (0 < value.Value && value.Value < 17)
        return size[value.Value] * RhinoMath.UnitScale(Rhino.UnitSystem.Inches, Rhino.UnitSystem.Millimeters);

      return 0.0;
    }

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
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

      if (Value is DB.Category category)
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
              if (CategoryType.NamedValues.TryGetValue((int) category.CategoryType, out var typeName))
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

          if (category.CategoryType == DB.CategoryType.Annotation)
          {
            if
            (
              Id.TryGetBuiltInCategory(out var builtInCategory) &&
              builtInCategory == DB.BuiltInCategory.OST_Grids
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
            if (category.Id.IntegerValue == (int) DB.BuiltInCategory.OST_LightingFixtureSource)
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
    public override string Name
    {
      get => base.Value?.Name ?? Value?.Name;
    }

    public System.Drawing.Color? LineColor
    {
      get => Value?.LineColor.ToColor();
      set
      {
        if (value is object && Value is DB.Category category)
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
      get => Value is DB.Category category ? new Material(category.Material) : default;
      set
      {
        if (value is object && Value is DB.Category category)
        {
          AssertValidDocument(value.Document, nameof(Material));
          if ((category.Material?.Id ?? DB.ElementId.InvalidElementId) != value.Id)
            category.Material = value.Value;
        }
      }
    }

    public int? ProjectionLineWeight
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle _)
            return category.GetLineWeight(DB.GraphicsStyleType.Projection);
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle _)
          {
            if (category.GetLineWeight(DB.GraphicsStyleType.Projection) != value)
              category.SetLineWeight(value.Value, DB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public int? CutLineWeight
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle _)
            return category.GetLineWeight(DB.GraphicsStyleType.Cut);
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle _)
          {
            if (category.GetLineWeight(DB.GraphicsStyleType.Cut) != value)
              category.SetLineWeight(value.Value, DB.GraphicsStyleType.Cut);
          }
        }
      }
    }

    public LinePatternElement ProjectionLinePattern
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(DB.GraphicsStyleType.Projection));
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          AssertValidDocument(value.Document, nameof(ProjectionLinePattern));
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Projection) is DB.GraphicsStyle style)
          {
            if (category.GetLinePatternId(DB.GraphicsStyleType.Projection) != value.Id)
              category.SetLinePatternId(value.Id, DB.GraphicsStyleType.Projection);
          }
        }
      }
    }

    public LinePatternElement CutLinePattern
    {
      get
      {
        if (Value is DB.Category category)
        {
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle style)
            return new LinePatternElement(style.Document, category.GetLinePatternId(DB.GraphicsStyleType.Cut));
        }

        return default;
      }
      set
      {
        if (value is object && Value is DB.Category category)
        {
          AssertValidDocument(value.Document, nameof(CutLinePattern));
          if (category.GetGraphicsStyle(DB.GraphicsStyleType.Cut) is DB.GraphicsStyle style)
          {
            if (category.GetLinePatternId(DB.GraphicsStyleType.Cut) != value.Id)
              category.SetLinePatternId(value.Id, DB.GraphicsStyleType.Cut);
          }
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Graphics Style")]
  public class GraphicsStyle : Element
  {
    protected override Type ScriptVariableType => typeof(DB.GraphicsStyle);
    public new DB.GraphicsStyle Value => base.Value as DB.GraphicsStyle;
    public static explicit operator DB.GraphicsStyle(GraphicsStyle value) => value?.Value;

    public GraphicsStyle() { }
    public GraphicsStyle(DB.GraphicsStyle graphicsStyle) : base(graphicsStyle) { }

    public override string DisplayName
    {
      get
      {
        var graphicsStyle = (DB.GraphicsStyle) this;
        if (graphicsStyle is object)
        {
          var tip = string.Empty;
          if (graphicsStyle.GraphicsStyleCategory.Parent is DB.Category parent)
            tip = $"{parent.Name} : ";

          switch (graphicsStyle.GraphicsStyleType)
          {
            case DB.GraphicsStyleType.Projection: return $"{tip}{graphicsStyle.Name} [projection]";
            case DB.GraphicsStyleType.Cut:        return $"{tip}{graphicsStyle.Name} [cut]";
          }
        }

        return base.DisplayName;
      }
    }
  }
}
