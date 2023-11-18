using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if REVIT_2018
using Autodesk.Revit.DB.Visual;
#else
using Autodesk.Revit.Utility;
#endif
using Rhino;
using Rhino.Display;
using Rhino.Render;
using ARDB = Autodesk.Revit.DB;
using SD = System.Drawing;

namespace RhinoInside.Revit.Convert.Render
{
  using Numerical;
  using Convert.Geometry;
  using Convert.System.Drawing;
  using Convert.Units;
  using External.DB.Extensions;
  using DBXS = External.DB.Schemas;

  /// <summary>
  /// Represents a converter for converting <see cref="RenderMaterial"/> values
  /// back and forth Revit and Rhino.
  /// </summary>
  public static class RenderMaterialConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.AppearanceAssetElement" /> to an equivalent <see cref="Rhino.Render.RenderMaterial" /> in given <see cref="Rhino.RhinoDoc" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToRenderMaterial(ARDB.AppearanceAssetElement, RhinoDoc)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using Rhino;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Render;
    ///
    /// RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
    /// RenderMaterial rhinoMaterial = appearanceAssetElement.ToRenderMaterial(doc);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Rhino.Render import RenderMaterial
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Render
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Render)
    /// 
    /// rhino_material = revit_appearance_asset_element.ToRenderMaterial()	# type: RenderMaterial
    /// </code>
    /// 
    /// Using <see cref="ToRenderMaterial(ARDB.AppearanceAssetElement, RhinoDoc)" /> as static method:
    ///
    /// <code language="csharp">
    /// using Rhino;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Render;
    ///
    /// RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
    /// RenderMaterial rhinoMaterial = RenderMaterialConverter.ToRenderMaterial(appearanceAssetElement, doc);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Rhino import RhinoDoc
    /// from Rhino.Render import RenderMaterial
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Render.RenderMaterialConverter as RMC
    ///
    /// doc = RhinoDoc.ActiveDoc	# type: RhinoDoc
    /// rhino_material = RMC.ToRenderMaterial(revit_appearance_asset_element, doc)	# type: RenderMaterial
    /// </code>
    ///
    /// </example>
    /// <param name="appearanceAssetElement">Revit appearance element to convert.</param>
    /// <param name="rhinoDoc">Rhino document to associate the resulting material with.</param>
    /// <returns>Rhino render material that is equivalent to the provided Revit appearance asset.</returns>
    /// <since>1.11</since>
    internal static RenderMaterial ToRenderMaterial(this ARDB.AppearanceAssetElement appearanceAssetElement, RhinoDoc rhinoDoc)
    {
      var renderMaterial = RenderMaterial.CreateBasicMaterial(Rhino.DocObjects.Material.DefaultMaterial, rhinoDoc);
      renderMaterial.Name = appearanceAssetElement.Name;

#if REVIT_2018
      using (var asset = appearanceAssetElement.GetRenderingAsset())
        renderMaterial.SimulateRenderingAsset(asset, rhinoDoc);
#endif

      return renderMaterial;
    }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Material" /> to an equivalent <see cref="Rhino.Render.RenderMaterial" /> in given <see cref="Rhino.RhinoDoc" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToRenderMaterial(ARDB.Material, RhinoDoc)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using Rhino;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Render;
    ///
    /// RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
    /// RenderMaterial rhinoMaterial = revitMaterial.ToRenderMaterial(doc);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Rhino.Render import RenderMaterial
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Render
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Render)
    /// 
    /// rhino_material = revit_material.ToRenderMaterial()	# type: RenderMaterial
    /// </code>
    /// 
    /// Using <see cref="ToRenderMaterial(ARDB.Material, RhinoDoc)" /> as static method:
    ///
    /// <code language="csharp">
    /// using Rhino;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Render;
    ///
    /// RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
    /// RenderMaterial rhinoMaterial = RenderMaterialConverter.ToRenderMaterial(revitMaterial, doc);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Rhino import RhinoDoc
    /// from Rhino.Render import RenderMaterial
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Render.RenderMaterialConverter as RMC
    ///
    /// doc = RhinoDoc.ActiveDoc	# type: RhinoDoc
    /// rhino_material = RMC.ToRenderMaterial(revit_material, doc)	# type: RenderMaterial
    /// </code>
    ///
    /// </example>
    /// <param name="material">Revit material to convert.</param>
    /// <param name="rhinoDoc">Rhino document to associate the resulting material with.</param>
    /// <returns>Rhino render material that is equivalent to the provided Revit material.</returns>
    /// <since>1.3</since>
    public static RenderMaterial ToRenderMaterial(this ARDB.Material material, RhinoDoc rhinoDoc)
    {
      var renderMaterial = RenderMaterial.CreateBasicMaterial(Rhino.DocObjects.Material.DefaultMaterial, rhinoDoc);
      renderMaterial.Name = material.Name;

#if REVIT_2018
      if (material.Document.GetElement(material.AppearanceAssetId) is ARDB.AppearanceAssetElement appearance)
      {
        using (var asset = appearance.GetRenderingAsset())
          renderMaterial.SimulateRenderingAsset(asset, rhinoDoc);
      }
      else
#endif
      {
        renderMaterial.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Diffuse, material.Color.ToColor());
        renderMaterial.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Shine, material.Shininess / 128.0 * Rhino.DocObjects.Material.MaxShine);
        renderMaterial.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Reflectivity, 1.0 / Math.Exp((1.0 - (material.Smoothness / 100.0)) * 10));
        renderMaterial.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Transparency, material.Transparency / 100.0);
      }

      return renderMaterial;
    }

#if REVIT_2018
    class BasicMaterialParameters
    {
      public RenderMaterial.PreviewGeometryType PreviewGeometryType = RenderMaterial.PreviewGeometryType.Scene;

      public Color4f Ambient = Color4f.Black;
      public Color4f Diffuse = Color4f.White;

      public double Shine = 0.0;
      public Color4f Specular = Color4f.White;
      public double Reflectivity = 0.0;
      public Color4f ReflectivityColor = Color4f.White;
      public double Transparency = 0.0;
      public Color4f TransparencyColor = Color4f.White;

      public double Ior = 1.0;
      public bool DisableLighting = false;
      public bool FresnelEnabled = false;
      public double PolishAmount = 0.0;
      public double ClarityAmount = 0.0;
      public Color4f Emission = Color4f.Black;

      public SimulatedTexture DiffuseTexture = default;
      public double DiffuseTextureAmount = 1.0;
      public SimulatedTexture BumpTexture = default;
      public double BumpTextureAmount = 1.0;
      public SimulatedTexture OpacityTexture = default;
      public double OpacityTextureAmount = 1.0;
      public SimulatedTexture EnvironmentTexture = default;
      public double EnvironmentTextureAmount = 1.0;
    }

    class SimulatedProceduralTexture : SimulatedTexture
    {
      public SimulatedProceduralTexture(Guid contentType) : base(RhinoDoc.ActiveDoc) { ContentType = contentType; }
      public readonly Guid ContentType;
      public readonly Dictionary<string, object> Fields = new Dictionary<string, object>();
    }

    static Color4f ToColor4f(AssetPropertyDoubleArray4d value, double f = 1.0)
    {
      var channles = value.GetValueAsDoubles();
      return new Color4f((float) (channles[0] * f), (float) (channles[1] * f), (float) (channles[2] * f), (float) (channles[3] * f));
    }

    /// <summary>
    /// Extracts Parameters from a <see cref="UnifiedBitmap"/> Asset to a <see cref="SimulatedTexture"/>
    /// </summary>
    /// <param name="asset"></param>
    /// <returns>A <see cref="SimulatedTexture"/> with the input <paramref name="asset"/> parameters.</returns>
    static SimulatedTexture ToSimulatedTexture(Asset asset)
    {
      if (asset is null)
        return default;

      if (asset.Name == "UnifiedBitmapSchema") return GetUnifiedBitmapSchemaTexture(asset);
      if (asset.Name == "BumpMapSchema") return GetBumpMapSchemaTexture(asset);
      else Debug.WriteLine($"Unimplemented Texture Schema: {asset.Name}");

      return default;
    }

    static SimulatedProceduralTexture GetUnifiedBitmapSchemaTexture(Asset asset)
    {
      var activeModelScale = UnitScale.GetModelScale(RhinoDoc.ActiveDoc);

      var offset = Rhino.Geometry.Vector2d.Zero;
      if (asset.FindByName(UnifiedBitmap.TextureRealWorldOffsetX) is AssetPropertyDistance offsetX)
        offset.X = ARDB.UnitUtils.Convert(offsetX.Value, offsetX.GetUnitTypeId(), DBXS.UnitType.Meters) /
                   activeModelScale;
      if (asset.FindByName(UnifiedBitmap.TextureRealWorldOffsetY) is AssetPropertyDistance offsetY)
        offset.Y = ARDB.UnitUtils.Convert(offsetY.Value, offsetY.GetUnitTypeId(), DBXS.UnitType.Meters) /
                   activeModelScale;

      var repeat = new Rhino.Geometry.Vector2d(1.0, 1.0);
      if (asset.FindByName(UnifiedBitmap.TextureRealWorldScaleX) is AssetPropertyDistance scaleX)
        repeat.X = activeModelScale /
                   ARDB.UnitUtils.Convert(scaleX.Value, scaleX.GetUnitTypeId(), DBXS.UnitType.Meters);
      if (asset.FindByName(UnifiedBitmap.TextureRealWorldScaleY) is AssetPropertyDistance scaleY)
        repeat.Y = activeModelScale /
                   ARDB.UnitUtils.Convert(scaleY.Value, scaleY.GetUnitTypeId(), DBXS.UnitType.Meters);

      bool inverted = false;
      if (asset.FindByName(UnifiedBitmap.UnifiedbitmapInvert) is AssetPropertyBoolean bitmapInverted)
        inverted = bitmapInverted.Value;

      double multiplier = 1.0;
      if (asset.FindByName(UnifiedBitmap.UnifiedbitmapRGBAmount) is AssetPropertyDouble RGBAAmount)
        multiplier = RGBAAmount.Value;

      var texture = new SimulatedProceduralTexture(ContentUuids.BitmapTextureType)
      {
        ProjectionMode = SimulatedTexture.ProjectionModes.WcsBox,
        Fields =
        {
          { "rdk-texture-adjust-invert", inverted },
          { "rdk-texture-adjust-multiplier", multiplier }
        }
      };

      if (asset.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) is AssetPropertyString source)
      {
        var entries = source.Value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        // Add the first entry without validation to have something in case we don't found a valid one.
        texture.Filename = entries.FirstOrDefault() ?? string.Empty;

        foreach (var entry in entries)
        {
          try
          {
            if (entry.IsFullyQualifiedPath() ?
                File.Exists(entry) :
                Rhino.ApplicationSettings.FileSettings.FindFile(entry) is string)
            {
              texture.Filename = entry;
              break;
            }
          }
          catch { }
        }
      }

      if (asset.FindByName(UnifiedBitmap.TextureURepeat) is AssetPropertyBoolean uRepeat &&
        asset.FindByName(UnifiedBitmap.TextureVRepeat) is AssetPropertyBoolean vRepeat)
        texture.Repeating = uRepeat.Value | vRepeat.Value;

      if (asset.FindByName(UnifiedBitmap.TextureWAngle) is AssetPropertyDouble angle)
        texture.Rotation = RhinoMath.ToRadians(angle.Value);

      texture.Offset = offset;
      texture.Repeat = repeat;

      return texture;
    }

    static SimulatedProceduralTexture GetBumpMapSchemaTexture(Asset asset)
    {
      if (asset.FindByName(BumpMap.BumpmapType) is AssetPropertyInteger bumpmapType)
      {
        switch ((BumpmapType) bumpmapType.Value)
        {
          case BumpmapType.HeightMap:

            var activeModelScale = UnitScale.GetModelScale(RhinoDoc.ActiveDoc);

            var offset = Rhino.Geometry.Vector2d.Zero;
            if (asset.FindByName(BumpMap.TextureRealWorldOffsetX) is AssetPropertyDistance offsetX)
              offset.X = ARDB.UnitUtils.Convert(offsetX.Value, offsetX.GetUnitTypeId(), DBXS.UnitType.Meters) / activeModelScale;
            if (asset.FindByName(BumpMap.TextureRealWorldOffsetY) is AssetPropertyDistance offsetY)
              offset.Y = ARDB.UnitUtils.Convert(offsetY.Value, offsetY.GetUnitTypeId(), DBXS.UnitType.Meters) / activeModelScale;

            var repeat = new Rhino.Geometry.Vector2d(1.0, 1.0);
            if (asset.FindByName(BumpMap.TextureRealWorldScaleX) is AssetPropertyDistance scaleX)
              repeat.X = activeModelScale / ARDB.UnitUtils.Convert(scaleX.Value, scaleX.GetUnitTypeId(), DBXS.UnitType.Meters);
            if (asset.FindByName(BumpMap.TextureRealWorldScaleY) is AssetPropertyDistance scaleY)
              repeat.Y = activeModelScale / ARDB.UnitUtils.Convert(scaleY.Value, scaleY.GetUnitTypeId(), DBXS.UnitType.Meters);

            bool inverted = false;
            double multiplier = 1.0;
            if (asset.FindByName(BumpMap.BumpmapDepth) is AssetPropertyDistance bumpmapDepth)
            {
              var depth = ARDB.UnitUtils.Convert(bumpmapDepth.Value, bumpmapDepth.GetUnitTypeId(), DBXS.UnitType.Meters) / activeModelScale;
              multiplier = depth / ((repeat.X + repeat.Y) * 0.5);
            }

            var texture = new SimulatedProceduralTexture(ContentUuids.BitmapTextureType)
            {
              ProjectionMode = SimulatedTexture.ProjectionModes.WcsBox,
              Fields =
              {
                { "rdk-texture-adjust-invert", inverted },
                { "rdk-texture-adjust-multiplier", multiplier }
              }
            };

            if (asset.FindByName(BumpMap.BumpmapBitmap) is AssetPropertyString source)
            {
              var entries = source.Value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

              // Add the first entry without validation to have something in case we don't found a valid one.
              texture.Filename = entries.FirstOrDefault() ?? string.Empty;

              foreach (var entry in entries)
              {
                try
                {
                  if (entry.IsFullyQualifiedPath() ?
                      File.Exists(entry) :
                      Rhino.ApplicationSettings.FileSettings.FindFile(entry) is string)
                  {
                    texture.Filename = entry;
                    break;
                  }
                }
                catch { }
              }
            }

            if (asset.FindByName(BumpMap.TextureURepeat) is AssetPropertyBoolean uRepeat &&
              asset.FindByName(BumpMap.TextureVRepeat) is AssetPropertyBoolean vRepeat)
              texture.Repeating = uRepeat.Value | vRepeat.Value;

            if (asset.FindByName(BumpMap.TextureWAngle) is AssetPropertyDouble angle)
              texture.Rotation = RhinoMath.ToRadians(angle.Value);

            texture.Offset = offset;
            texture.Repeat = repeat;

            return texture;

          default:
            Debug.WriteLine($"Unimplemented {nameof(BumpmapType)}: {(BumpmapType) bumpmapType.Value}");
            break;
        }
      }

      return null;
    }

    static SimulatedTexture ToSimulatedTexture(string path)
    {
      return new SimulatedTexture(RhinoDoc.ActiveDoc)
      {
        Filename = path,
        ProjectionMode = SimulatedTexture.ProjectionModes.WcsBox,
        Repeat = new Rhino.Geometry.Vector2d(1.0, 1.0)
      };
    }

    internal static void SimulateRenderingAsset(this RenderMaterial material, Asset asset, RhinoDoc doc)
    {
      if (asset.FindByName(SchemaCommon.Description) is AssetPropertyString description)
        material.Notes = description.Value;

      if (asset.FindByName(SchemaCommon.Keyword) is AssetPropertyString keyword)
      {
        string tags = string.Empty;
        foreach (var tag in (keyword.Value ?? string.Empty).Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
          tags += $";{tag.Replace(';', ':')}";

        material.Tags = tags;
      }

      if (TryGetBasicMaterialParameters(asset, out var materialParams))
      {
        material.DefaultPreviewGeometryType = materialParams.PreviewGeometryType;

        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Ambient, materialParams.Ambient);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Diffuse, materialParams.Diffuse);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Shine, materialParams.Shine);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Specular, materialParams.Specular);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Reflectivity, materialParams.Reflectivity);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.ReflectivityColor, materialParams.ReflectivityColor);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Ior, materialParams.Ior);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Transparency, materialParams.Transparency);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.TransparencyColor, materialParams.TransparencyColor);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.DisableLighting, materialParams.DisableLighting);
        material.Fields.Set("fresnel-enabled", materialParams.FresnelEnabled);
        material.Fields.Set("polish-amount", materialParams.PolishAmount);
        material.Fields.Set("clarity-amount", materialParams.ClarityAmount);
        material.Fields.Set(RenderMaterial.BasicMaterialParameterNames.Emission, materialParams.Emission);

        SetChildSlot(material, RenderMaterial.StandardChildSlots.Diffuse, materialParams.DiffuseTexture, materialParams.DiffuseTextureAmount, doc);
        SetChildSlot(material, RenderMaterial.StandardChildSlots.Bump, materialParams.BumpTexture, materialParams.BumpTextureAmount, doc);
        SetChildSlot(material, RenderMaterial.StandardChildSlots.Transparency, materialParams.OpacityTexture, materialParams.OpacityTextureAmount, doc);
        SetChildSlot(material, RenderMaterial.StandardChildSlots.Environment, materialParams.EnvironmentTexture, materialParams.EnvironmentTextureAmount, doc);
      }
      else Debug.WriteLine($"Unimplemented Material Schema: {asset.Name}");
    }

    static void SetFieldValues(Rhino.Render.Fields.FieldDictionary fields, Dictionary<string, object> values)
    {
      foreach (var field in values)
      {
        switch (field.Value)
        {
          case byte[] bb: fields.Set(field.Key, bb); break;
          case string s: fields.Set(field.Key, s); break;
          case bool b: fields.Set(field.Key, b); break;
          case int i: fields.Set(field.Key, i); break;
          case float f: fields.Set(field.Key, f); break;
          case double d: fields.Set(field.Key, d); break;
          case Color4f c: fields.Set(field.Key, c); break;
          case DateTime dt: fields.Set(field.Key, dt); break;
          case Guid g: fields.Set(field.Key, g); break;
          case Rhino.Geometry.Point2d p2: fields.Set(field.Key, p2); break;
          case Rhino.Geometry.Point3d p3: fields.Set(field.Key, p3); break;
          case Rhino.Geometry.Point4d p4: fields.Set(field.Key, p4); break;
          case Rhino.Geometry.Vector2d v2: fields.Set(field.Key, v2); break;
          case Rhino.Geometry.Vector3d v3: fields.Set(field.Key, v3); break;
          case Rhino.Geometry.Transform t: fields.Set(field.Key, t); break;
          default: throw new NotImplementedException();
        }
      }
    }

    static void SetChildSlot
    (
      RenderMaterial material,
      RenderMaterial.StandardChildSlots slot,
      SimulatedTexture simulated,
      double amount,
      RhinoDoc doc
    )
    {
      var slotName = material.TextureChildSlotName(slot);

      if (simulated is null)
      {
        material.SetChildSlotOn(slotName, false, RenderContent.ChangeContexts.Program);
      }
      else
      {
        var texture = default(RenderTexture);
        if (simulated is SimulatedProceduralTexture procedural && procedural.ContentType != ContentUuids.SimpleBitmapTextureType)
        {
          if (procedural.ContentType == ContentUuids.BitmapTextureType)
            texture = RenderTexture.NewBitmapTexture(simulated, doc);
          else
            texture = RenderContentType.NewContentFromTypeId(procedural.ContentType, doc) as RenderTexture;

          texture.SetProjectionMode((TextureProjectionMode) (int) simulated.ProjectionMode, RenderContent.ChangeContexts.Program);
          texture.SetMappingChannel(simulated.MappingChannel, RenderContent.ChangeContexts.Program);
          texture.SetOffset(new Rhino.Geometry.Vector3d(simulated.Offset.X, simulated.Offset.Y, 0.0), RenderContent.ChangeContexts.Program);
          texture.SetRepeat(new Rhino.Geometry.Vector3d(simulated.Repeat.X, simulated.Repeat.Y, 1.0), RenderContent.ChangeContexts.Program);
          texture.SetRotation(new Rhino.Geometry.Vector3d(0.0, 0.0, simulated.Rotation), RenderContent.ChangeContexts.Program);

          SetFieldValues(texture.Fields, procedural.Fields);
        }
        else texture = RenderTexture.NewBitmapTexture(simulated, doc);

        if (material.SetChild(texture, slotName))
        {
          material.SetChildSlotOn(slotName, true, RenderContent.ChangeContexts.Program);
          material.SetChildSlotAmount(slotName, Arithmetic.Clamp(amount, 0.0, 1.0) * 100.0, RenderContent.ChangeContexts.Program);
        }
      }
    }

    static bool TryGetBasicMaterialParameters(Asset asset, out BasicMaterialParameters materialParams)
    {
      materialParams = new BasicMaterialParameters();
      if (asset.Name == "Generic") GetGenericSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "GenericSchema") GetGenericSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "GlazingSchema") GetGlazingSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "SolidGlassSchema") GetSolidGlassSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "ConcreteSchema") GetConcreteSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "StoneSchema") GetStoneSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "MetalSchema") GetMetalSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "MetallicPaintSchema") GetMetallicPaintSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "WallPaintSchema") GetWallPaintSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "HardwoodSchema") GetHardwoodSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "CeramicSchema") GetCeramicSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "PlasticVinylSchema") GetPlasticVinylSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "WaterSchema") GetWaterSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "MirrorSchema") GetMirrorSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "MasonryCMUSchema") GetMasonryCMUSchemaParameters(asset, ref materialParams);

      else if (asset.Name == "PrismOpaqueSchema") GetPrismOpaqueSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "PrismTransparentSchema") GetPrismTransparentSchemaParameters(asset, ref materialParams);
      else if (asset.Name == "PrismMetalSchema") GetPrismMetalSchemaParameters(asset, ref materialParams);
#if REVIT_2020
      else if (asset.Name == "PrismGlazingSchema") GetPrismGlazingSchemaParameters(asset, ref materialParams);
#endif
      else return false;

      return true;
    }

    static void GetGenericSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      if (asset.FindByName(Generic.GenericDiffuse) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());
        if (asset.FindByName(Generic.GenericDiffuseImageFade) is AssetPropertyDouble diffuseAmount)
          material.DiffuseTextureAmount = diffuseAmount.Value;

        if (asset.FindByName(Generic.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(Generic.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }

        if (asset.FindByName(Generic.GenericIsMetal) is AssetPropertyBoolean isMetal && isMetal.Value)
          material.ReflectivityColor = material.Diffuse;
      }

      if (asset.FindByName(Generic.GenericBumpMap) is AssetPropertyDoubleArray4d bumpMap)
      {
        material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());

        if (asset.FindByName(Generic.GenericBumpAmount) is AssetPropertyDouble bumpAmount)
          material.BumpTextureAmount = bumpAmount.Value;
      }

      if (asset.FindByName(Generic.GenericCutoutOpacity) is AssetPropertyDouble opacity)
      {
        material.OpacityTextureAmount = opacity.Value;
        material.OpacityTexture = ToSimulatedTexture(opacity.GetSingleConnectedAsset());
      }

      if (asset.FindByName(Generic.GenericTransparency) is AssetPropertyDouble transparency)
      {
        material.Transparency = transparency.Value;

        // TODO : Build a Transparency Texture.
        //material.OpacityTexture = ToSimulatedTexture(transparency.GetSingleConnectedAsset());

        //if (asset.FindByName(Generic.GenericTransparencyImageFade) is AssetPropertyDouble transparencyAmount)
        //  material.OpacityTextureAmount = transparencyAmount.Value;
      }

      if (asset.FindByName(Generic.GenericRefractionIndex) is AssetPropertyDouble ior)
        material.Ior = ior.Value;

      if (asset.FindByName(Generic.GenericGlossiness) is AssetPropertyDouble glossiness)
        material.Reflectivity = glossiness.Value;

      if (asset.FindByName(Generic.GenericSelfIllumLuminance) is AssetPropertyDouble luminance)
      {
        material.DisableLighting = luminance.Value > 0.0;
        if (material.DisableLighting)
        {
          if (asset.FindByName(Generic.GenericSelfIllumFilterMap) is AssetPropertyDoubleArray4d emission)
          {
            var luminanceFactor = Arithmetic.Clamp(luminance.Value, 0.0, 2000.0) / 2000.0;
            material.Emission = ToColor4f(emission, luminanceFactor);
          }
        }
      }

      if (asset.FindByName(Generic.GenericReflectivityAt0deg) is AssetPropertyDouble refelectivity0)
        material.PolishAmount = refelectivity0.Value;

      if (asset.FindByName(Generic.GenericReflectivityAt90deg) is AssetPropertyDouble reflectivity90)
        material.Shine = reflectivity90.Value;

      if (asset.FindByName(Generic.GenericRefractionTranslucencyWeight) is AssetPropertyDouble refractionTranslucencyWeight)
        material.ClarityAmount = refractionTranslucencyWeight.Value;
    }

    static void GetGlazingSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      if (asset.FindByName(Glazing.GlazingTransmittanceColor) is AssetPropertyInteger transmittance)
      {
        switch ((GlazingTransmittanceColorType) transmittance.Value)
        {
          case GlazingTransmittanceColorType.Clear: material.TransparencyColor = new Color4f(0.858f, 0.893f, 0.879f, 1.0f); break;
          case GlazingTransmittanceColorType.Green: material.TransparencyColor = new Color4f(0.676f, 0.797f, 0.737f, 1.0f); break;
          case GlazingTransmittanceColorType.Gray: material.TransparencyColor = new Color4f(0.451f, 0.449f, 0.472f, 1.0f); break;
          case GlazingTransmittanceColorType.Blue: material.TransparencyColor = new Color4f(0.367f, 0.514f, 0.651f, 1.0f); break;
          case GlazingTransmittanceColorType.Bluegreen: material.TransparencyColor = new Color4f(0.654f, 0.788f, 0.772f, 1.0f); break;
          case GlazingTransmittanceColorType.Bronze: material.TransparencyColor = new Color4f(0.583f, 0.516f, 0.467f, 1.0f); break;
          case GlazingTransmittanceColorType.Custom:
            if (asset.FindByName(Glazing.GlazingTransmittanceMap) is AssetPropertyDoubleArray4d transmittanceCustomColor)
            {
              material.TransparencyColor = ToColor4f(transmittanceCustomColor);
              material.OpacityTexture = ToSimulatedTexture(transmittanceCustomColor.GetSingleConnectedAsset());
            }
            break;
        }
      }

      material.Diffuse = material.TransparencyColor;
      material.Transparency = 0.9;

      if (asset.FindByName(Glazing.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
      {
        if (asset.FindByName(Glazing.CommonTintColor) is AssetPropertyDoubleArray4d tint)
        {
          var tintColor = ToColor4f(tint);
          material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
        }
      }

      material.Ior = 1.52;
      if (asset.FindByName(Glazing.GlazingNoLevels) is AssetPropertyInteger levels)
        material.Ior += levels.Value * 0.10;

      if (asset.FindByName(Glazing.GlazingReflectance) is AssetPropertyDouble refelectance)
        material.Reflectivity = refelectance.Value;

      material.PolishAmount = 1.0;
      material.ClarityAmount = 1.0;
    }

    static void GetSolidGlassSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      if (asset.FindByName(SolidGlass.SolidglassTransmittance) is AssetPropertyInteger transmittance)
      {
        switch ((SolidglassTransmittanceType) transmittance.Value)
        {
          case SolidglassTransmittanceType.Clear: material.TransparencyColor = new Color4f(0.858f, 0.893f, 0.879f, 1.0f); break;
          case SolidglassTransmittanceType.Green: material.TransparencyColor = new Color4f(0.676f, 0.797f, 0.737f, 1.0f); break;
          case SolidglassTransmittanceType.Gray: material.TransparencyColor = new Color4f(0.451f, 0.449f, 0.472f, 1.0f); break;
          case SolidglassTransmittanceType.Blue: material.TransparencyColor = new Color4f(0.367f, 0.514f, 0.651f, 1.0f); break;
          case SolidglassTransmittanceType.Bluegreen: material.TransparencyColor = new Color4f(0.654f, 0.788f, 0.772f, 1.0f); break;
          case SolidglassTransmittanceType.Bronze: material.TransparencyColor = new Color4f(0.583f, 0.516f, 0.467f, 1.0f); break;
          case SolidglassTransmittanceType.CustomColor:
            if (asset.FindByName(SolidGlass.SolidglassTransmittanceCustomColor) is AssetPropertyDoubleArray4d transmittanceCustomColor)
            {
              material.TransparencyColor = ToColor4f(transmittanceCustomColor);
              material.OpacityTexture = ToSimulatedTexture(transmittanceCustomColor.GetSingleConnectedAsset());
            }
            break;
        }
      }

      material.Transparency = 0.9;
      material.Diffuse = material.TransparencyColor;

      if (asset.FindByName(SolidGlass.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
      {
        if (asset.FindByName(SolidGlass.CommonTintColor) is AssetPropertyDoubleArray4d tint)
        {
          var tintColor = ToColor4f(tint);
          material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
        }
      }

      if (asset.FindByName(SolidGlass.SolidglassRefractionIor) is AssetPropertyDouble ior)
        material.Ior = ior.Value;
      else
        material.Ior = 1.52;

      if (asset.FindByName(SolidGlass.SolidglassReflectance) is AssetPropertyDouble refelectance)
        material.Reflectivity = refelectance.Value;

      if (asset.FindByName(SolidGlass.SolidglassGlossiness) is AssetPropertyDouble glossiness)
      {
        material.PolishAmount = glossiness.Value;
        material.ClarityAmount = glossiness.Value;
      }

      if (asset.FindByName(SolidGlass.SolidglassBumpEnable) is AssetPropertyInteger bumpType)
      {
        switch ((SolidglassBumpEnableType) bumpType.Value)
        {
          case SolidglassBumpEnableType.None: break;
          case SolidglassBumpEnableType.Rippled: break;
          case SolidglassBumpEnableType.Wavy: break;
          case SolidglassBumpEnableType.Custom:
            if (asset.FindByName(SolidGlass.SolidglassBumpMap) is AssetPropertyReference bumpMap)
              material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());

            if (asset.FindByName(SolidGlass.SolidglassBumpAmount) is AssetPropertyDouble bumpAmount)
              material.BumpTextureAmount = bumpAmount.Value;
            break;
        }
      }
    }

    static void GetConcreteSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(Concrete.ConcreteColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(Concrete.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(Concrete.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }
      }

      if (asset.FindByName(Concrete.ConcreteSealant) is AssetPropertyInteger sealant)
      {
        switch ((ConcreteSealantType) sealant.Value)
        {
          case ConcreteSealantType.None: material.Reflectivity = 0.1; break;
          case ConcreteSealantType.Epoxy: material.Reflectivity = 0.6; break;
          case ConcreteSealantType.Acrylic: material.Reflectivity = 0.8; break;
        }
      }

      //if (asset.FindByName(Concrete.ConcreteBrightmode) is AssetPropertyInteger brightmode)
      //{
      //  switch ((ConcreteBrightmodeType) brightmode.Value)
      //  {
      //    case ConcreteBrightmodeType.None: break;
      //    case ConcreteBrightmodeType.Automatic: /*TODO*/ break;
      //    case ConcreteBrightmodeType.Custom:
      //      if (asset.FindByName(Concrete.ConcreteBmMap) is AssetPropertyReference bitmap)
      //        material.?? = ToSimulatedTexture(bitmap.GetSingleConnectedAsset());
      //      break;
      //  }
      //}

      double polish = 0.0;
      if (asset.FindByName(Concrete.ConcreteFinish) is AssetPropertyInteger finish)
      {
        switch ((ConcreteFinishType) finish.Value)
        {
          case ConcreteFinishType.Straight: polish = 0.1; break;
          case ConcreteFinishType.Curved: polish = 0.2; break;
          case ConcreteFinishType.Smooth: polish = 0.8; break;
          case ConcreteFinishType.Polished: polish = 0.9; break;
          case ConcreteFinishType.Custom:
            if (asset.FindByName(Concrete.ConcreteBumpMap) is AssetPropertyReference bumpMap)
              material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());

            if (asset.FindByName(Concrete.ConcreteBumpAmount) is AssetPropertyDouble bumpAmount)
              material.BumpTextureAmount = bumpAmount.Value;
            break;
        }
      }

      material.FresnelEnabled = true;
      material.Shine = polish;
      material.PolishAmount = polish;
    }

    static void GetStoneSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(Stone.StoneColor) is AssetPropertyReference diffuse)
      {
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(Stone.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(Stone.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }
      }

      if (asset.FindByName(Stone.StoneApplication) is AssetPropertyInteger application)
      {
        switch ((StoneApplicationType) application.Value)
        {
          case StoneApplicationType.Polished: material.PolishAmount = 1.0; break;
          case StoneApplicationType.Glossy: material.PolishAmount = 0.8; break;
          case StoneApplicationType.Matte: material.PolishAmount = 0.6; break;
          case StoneApplicationType.Unfinished: material.PolishAmount = 0.2; break;
        }
      }

      if (asset.FindByName(Stone.StoneBump) is AssetPropertyInteger bumpType)
      {
        switch ((StoneBumpType) bumpType.Value)
        {
          case StoneBumpType.None: break;
          case StoneBumpType.Polishedgranite:
            if (asset.FindByName("granite_tex") is AssetPropertyString granite)
              material.BumpTexture = ToSimulatedTexture(granite.Value);
            break;
          case StoneBumpType.Stonewall:
            if (asset.FindByName("stonewall_tex") is AssetPropertyString stonewall)
              material.BumpTexture = ToSimulatedTexture(stonewall.Value);
            break;
          case StoneBumpType.Glossymarble:
            if (asset.FindByName("marble_tex") is AssetPropertyString marble)
              material.BumpTexture = ToSimulatedTexture(marble.Value);
            break;
          case StoneBumpType.Custom:
            if (asset.FindByName(Stone.StoneBumpMap) is AssetPropertyReference bumpMap)
              material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());

            if (asset.FindByName(Stone.StoneBumpAmount) is AssetPropertyDouble bumpAmount)
              material.BumpTextureAmount = bumpAmount.Value;
            break;
        }
      }

      if (asset.FindByName(Stone.StonePattern) is AssetPropertyInteger pattern)
      {
        switch ((StonePatternType) pattern.Value)
        {
          case StonePatternType.None: break;
          case StonePatternType.Custom:
            if (asset.FindByName(Stone.StonePatternMap) is AssetPropertyReference patternMap)
              material.BumpTexture = ToSimulatedTexture(patternMap.GetSingleConnectedAsset());

            if (asset.FindByName(Stone.StonePatternAmount) is AssetPropertyDouble patternAmount)
              material.BumpTextureAmount = patternAmount.Value;
            break;
        }
      }

      material.FresnelEnabled = true;
      material.Reflectivity = 1.0;
      material.Shine = material.PolishAmount;
    }

    static void GetMetalSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      var metalFinishType = MetalFinishType.Polished;
      if (asset.FindByName(Metal.MetalFinish) is AssetPropertyInteger finish)
        metalFinishType = (MetalFinishType) finish.Value;

      if (asset.FindByName(Metal.MetalType) is AssetPropertyInteger metalType)
      {
        switch ((MetalType) metalType.Value)
        {
          case MetalType.Aluminum: material.Diffuse = new Color4f(SD.Color.FromArgb(206, 210, 213)); break;
          case MetalType.GalvanizedAlu:
            material.Diffuse = new Color4f(SD.Color.FromArgb(206, 210, 213));
            if (asset.FindByName(Metal.MetalColor) is AssetPropertyDoubleArray4d metalColor)
            {
              material.Diffuse = ToColor4f(metalColor);
              material.DiffuseTexture = ToSimulatedTexture(metalColor.GetSingleConnectedAsset());
            }
            metalFinishType = MetalFinishType.Polished;
            break;
          case MetalType.Chrome: material.Diffuse = new Color4f(SD.Color.FromArgb(244, 244, 244)); break;
          case MetalType.Copper: material.Diffuse = new Color4f(SD.Color.FromArgb(187, 80, 46)); break;
          case MetalType.Brass: material.Diffuse = new Color4f(SD.Color.FromArgb(202, 154, 58)); break;
          case MetalType.Bronze: material.Diffuse = new Color4f(SD.Color.FromArgb(105, 77, 58)); break;
          case MetalType.StainlessSteel: material.Diffuse = new Color4f(SD.Color.FromArgb(189, 187, 185)); break;
          case MetalType.Zinc:
            material.Diffuse = new Color4f(SD.Color.FromArgb(164, 172, 176));
            metalFinishType = MetalFinishType.Brushed;
            break;
        }
      }

      if (asset.FindByName(Metal.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
      {
        if (asset.FindByName(Metal.CommonTintColor) is AssetPropertyDoubleArray4d tint)
        {
          var tintColor = ToColor4f(tint);
          material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
        }
      }

      material.ReflectivityColor = material.Diffuse;

      if (asset.FindByName(Metal.MetalPattern) is AssetPropertyInteger pattern && pattern.Value == (int) MetalPatternType.Custom)
      {
        if (asset.FindByName(Metal.MetalPatternShader) is AssetPropertyReference patternShader)
          material.BumpTexture = ToSimulatedTexture(patternShader.GetSingleConnectedAsset());
      }

      if (asset.FindByName(Metal.MetalPerforations) is AssetPropertyInteger perforations && perforations.Value == (int) MetalPerforationsType.Custom)
      {
        if (asset.FindByName(Metal.MetalPerforationsShader) is AssetPropertyReference perforationsShader)
          material.OpacityTexture = ToSimulatedTexture(perforationsShader.GetSingleConnectedAsset());
      }

      {
        double polish = 1.0;
        switch (metalFinishType)
        {
          case MetalFinishType.Polished: polish = 1.00; break;
          case MetalFinishType.SemiPolished: polish = 0.80; break;
          case MetalFinishType.Satin: polish = 0.60; break;
          case MetalFinishType.Brushed: polish = 0.20; break;
        }

        material.Shine = polish;
        material.PolishAmount = polish;
        material.Reflectivity = polish;
      }
    }

    static void GetMetallicPaintSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      if (asset.FindByName(MetallicPaint.MetallicpaintBaseColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(MetallicPaint.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(MetallicPaint.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }
      }

      if (asset.FindByName(MetallicPaint.MetallicpaintPearlColor) is AssetPropertyDoubleArray4d specular)
        material.Specular = ToColor4f(specular);

      // TODO: apply some bump texture
      if (asset.FindByName(MetallicPaint.MetallicpaintFinish) is AssetPropertyInteger finish)
      {
        switch ((MetallicpaintFinishType) finish.Value)
        {
          case MetallicpaintFinishType.Smooth: break;
          case MetallicpaintFinishType.Peeling: break;
        }
      }

      double glossines = 0.5;
      double angle = 0.5;
      if (asset.FindByName(MetallicPaint.MetallicpaintTopcoat) is AssetPropertyInteger topCoat)
      {
        switch ((MetallicpaintTopcoatType) topCoat.Value)
        {
          case MetallicpaintTopcoatType.Carpaint:
            glossines = 1.0;
            angle = 0.8;
            break;
          case MetallicpaintTopcoatType.Chrome:
            glossines = 1.0;
            angle = 0.0;
            break;
          case MetallicpaintTopcoatType.Matte:
            glossines = 0.1;
            angle = 0.9;
            break;
          case MetallicpaintTopcoatType.Custom:
            if (asset.FindByName(MetallicPaint.MetallicpaintTopcoatGlossy) is AssetPropertyDouble glossy)
              glossines = glossy.Value;

            if (asset.FindByName(MetallicPaint.MetallicpaintTopcoatFalloff) is AssetPropertyDouble fallof)
              angle = fallof.Value;
            break;
        }
      }

      material.Emission = material.Diffuse;

      material.FresnelEnabled = angle > 0.5;
      material.Diffuse = Color4f.White.BlendTo((float) angle, material.Diffuse);
      material.Shine = glossines;
      material.Reflectivity = glossines;
      material.PolishAmount = glossines;
      material.ClarityAmount = glossines;
    }

    static void GetWallPaintSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(WallPaint.WallpaintColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(WallPaint.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(WallPaint.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }

        material.ReflectivityColor = material.Diffuse;
      }

      // TODO: apply some bump texture
      if (asset.FindByName(WallPaint.WallpaintApplication) is AssetPropertyInteger application)
      {
        switch ((WallpaintApplicationType) application.Value)
        {
          case WallpaintApplicationType.Roller: break;
          case WallpaintApplicationType.Brush: break;
          case WallpaintApplicationType.Spray: break;
        }
      }

      double polish = 0.0;
      if (asset.FindByName(WallPaint.WallpaintFinish) is AssetPropertyInteger finish)
      {
        switch ((WallpaintFinishType) finish.Value)
        {
          case WallpaintFinishType.Flat: polish = 0.02; break;
          case WallpaintFinishType.Eggshell: polish = 0.1; break;
          case WallpaintFinishType.Platinum: polish = 0.3; break;
          case WallpaintFinishType.Pearl: polish = 0.4; break;
          case WallpaintFinishType.Semigloss: polish = 0.75; break;
          case WallpaintFinishType.Gloss: polish = 0.85; break;
        }
      }

      material.Shine = polish;
      material.Specular = new Color4f((float) polish, (float) polish, (float) polish, 1.0f);
      material.Reflectivity = polish;

      material.FresnelEnabled = true;
      material.PolishAmount = polish;
      material.ClarityAmount = 1.0;
    }

    static void GetHardwoodSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(Hardwood.HardwoodColor) is AssetPropertyReference hardwoodColor)
      {
        material.Diffuse = Color4f.White;
        material.DiffuseTexture = ToSimulatedTexture(hardwoodColor.GetSingleConnectedAsset());

        if (asset.FindByName(Hardwood.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(Hardwood.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }

        material.ReflectivityColor = material.Diffuse;
      }

      // TODO: apply some bump texture
      if (asset.FindByName(Hardwood.HardwoodApplication) is AssetPropertyInteger application)
      {
        switch ((HardwoodApplicationType) application.Value)
        {
          case HardwoodApplicationType.looring: break;
          case HardwoodApplicationType.urniture: break;
        }
      }

      double polish = 0.0;
      if (asset.FindByName(Hardwood.HardwoodFinish) is AssetPropertyInteger finish)
      {
        switch ((HardwoodFinishType) finish.Value)
        {
          case HardwoodFinishType.Gloss: polish = 0.85; break;
          case HardwoodFinishType.Semigloss: polish = 0.75; break;
          case HardwoodFinishType.Satin: polish = 0.4; break;
          case HardwoodFinishType.Unfinished: polish = 0.02; break;
        }
      }

      material.Shine = polish;
      material.Specular = new Color4f((float) polish, (float) polish, (float) polish, 1.0f);
      material.Reflectivity = polish;

      material.FresnelEnabled = true;
      material.PolishAmount = polish;
      material.ClarityAmount = 1.0;
    }

    static void GetCeramicSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      if (asset.FindByName(Ceramic.CeramicColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(Ceramic.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(Ceramic.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }

        material.ReflectivityColor = material.Diffuse;
      }

      double polish = 0.0;
      if (asset.FindByName(Ceramic.CeramicApplication) is AssetPropertyInteger application)
      {
        switch ((CeramicApplicationType) application.Value)
        {
          case CeramicApplicationType.HighGlossy: polish = 1.0; break;
          case CeramicApplicationType.Satin: polish = 0.85; break;
          case CeramicApplicationType.Matte: polish = 0.2; break;
        }
      }

      // TODO:
      if (asset.FindByName(Ceramic.CeramicType) is AssetPropertyInteger type)
      {
        switch ((CeramicType) type.Value)
        {
          case CeramicType.Ceramic: break;
          case CeramicType.Porcelain: break;
        }
      }

      if (asset.FindByName(Ceramic.CeramicBump) is AssetPropertyInteger bump)
      {
        switch ((CeramicBumpType) bump.Value)
        {
          case CeramicBumpType.None: break;
          case CeramicBumpType.Wavy: break;
          case CeramicBumpType.Custom:
            if (asset.FindByName(Ceramic.CeramicBumpMap) is AssetPropertyReference bumpMap)
            {
              material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());
              if (asset.FindByName(Ceramic.CeramicBumpMap) is AssetPropertyDouble bumpMapAmount)
                material.BumpTextureAmount = bumpMapAmount.Value;
            }
            break;
        }
      }

      material.Shine = polish;
      material.Reflectivity = polish;

      material.FresnelEnabled = true;
      material.PolishAmount = polish;
      material.ClarityAmount = 0.0;
    }

    static void GetPlasticVinylSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      if (asset.FindByName(PlasticVinyl.PlasticvinylColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(PlasticVinyl.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(PlasticVinyl.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }

        material.ReflectivityColor = material.Diffuse;
      }

      double polish = 0.0;
      if (asset.FindByName(PlasticVinyl.PlasticvinylApplication) is AssetPropertyInteger application)
      {
        switch ((PlasticvinylApplicationType) application.Value)
        {
          case PlasticvinylApplicationType.Polished: polish = 1.0; break;
          case PlasticvinylApplicationType.Glossy: polish = 0.85; break;
          case PlasticvinylApplicationType.Matte: polish = 0.2; break;
        }
      }

      if (asset.FindByName(PlasticVinyl.PlasticvinylType) is AssetPropertyInteger type)
      {
        switch ((PlasticvinylType) type.Value)
        {
          case PlasticvinylType.Plasticsolid: material.Transparency = 0.0; break;
          case PlasticvinylType.Plastictransparent: material.Transparency = 0.9; break;
          case PlasticvinylType.Vinyl: material.Transparency = 0.8; break;
        }
      }

      if (asset.FindByName(PlasticVinyl.PlasticvinylBump) is AssetPropertyInteger bump)
      {
        switch ((PlasticvinylBumpType) bump.Value)
        {
          case PlasticvinylBumpType.None: break;
          case PlasticvinylBumpType.Custom:
            if (asset.FindByName(PlasticVinyl.PlasticvinylBumpMap) is AssetPropertyReference bumpMap)
            {
              material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());
              if (asset.FindByName(PlasticVinyl.PlasticvinylBumpAmount) is AssetPropertyDouble bumpMapAmount)
                material.BumpTextureAmount = bumpMapAmount.Value;
            }
            break;
        }
      }

      material.Shine = polish;
      material.Reflectivity = polish;

      material.FresnelEnabled = true;
      material.PolishAmount = polish;
      material.ClarityAmount = 1.0;
    }

    static void GetWaterSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Plane;
      material.Ior = 1.325;
      material.Reflectivity = 1.00;
      material.Transparency = 0.75;

      if (asset.FindByName(Water.WaterType) is AssetPropertyInteger waterType)
      {
        switch ((WaterType) waterType.Value)
        {
          case WaterType.SwimmingPool:
            material.BumpTexture = new SimulatedProceduralTexture(ContentUuids.NoiseTextureType)
            {
              ProjectionMode = SimulatedTexture.ProjectionModes.Wcs,
              Repeat = new Rhino.Geometry.Vector2d(1.0 / 200.0, 1.0 / 200.0),
              Fields =
              {
                { "texture-on-one", false },
                { "texture-on-two", false },
                { "octave-count", 3 },
                { "frequency-multiplier", 2.0 },
                { "amplitude-multiplier", 0.5 },
                { "gain", 0.5 },
              }
            };
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(0, 227, 255));
            material.Transparency = 0.85;
            material.ClarityAmount = 0.5;
            break;
          case WaterType.ReflectingPool:
            material.FresnelEnabled = true;
            material.Diffuse = new Color4f(SD.Color.FromArgb(35, 48, 78));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(139, 155, 159));
            material.Transparency = 0.0;
            material.PolishAmount = 1.00;
            material.ClarityAmount = 0.00;
            break;
          case WaterType.River:
          case WaterType.Lake:
          case WaterType.Ocean:
            material.BumpTexture = new SimulatedProceduralTexture(ContentUuids.NoiseTextureType)
            {
              ProjectionMode = SimulatedTexture.ProjectionModes.Wcs,
              Repeat = new Rhino.Geometry.Vector2d(1.0 / 200.0, 1.0 / 200.0),
              Fields =
              {
                { "texture-on-one", false },
                { "texture-on-two", false },
                { "octave-count", 3 },
                { "frequency-multiplier", 2.0 },
                { "amplitude-multiplier", 0.5 },
                { "gain", 0.5 },
              }
            };
            break;
        }
      }

      if (asset.FindByName(Water.WaterTintEnable) is AssetPropertyInteger tintType)
      {
        switch ((WaterTintEnableType) tintType.Value)
        {
          case WaterTintEnableType.Tropical:
            material.FresnelEnabled = false;
            material.Diffuse = new Color4f(SD.Color.FromArgb(49, 154, 134));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(93, 171, 194));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(206, 203, 169));
            material.Emission = new Color4f(SD.Color.FromArgb(38, 120, 105));
            material.Transparency = 0.75;
            material.PolishAmount = 0.90;
            material.ClarityAmount = 0.75;
            break;
          case WaterTintEnableType.Algae:
            material.FresnelEnabled = false;
            material.Diffuse = new Color4f(SD.Color.FromArgb(139, 134, 78));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(175, 170, 113));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(77, 139, 123));
            material.Transparency = 0.95;
            material.PolishAmount = 0.80;
            material.ClarityAmount = 1.00;
            break;
          case WaterTintEnableType.Murky:
            material.FresnelEnabled = false;
            material.Diffuse = new Color4f(SD.Color.FromArgb(35, 48, 78));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(93, 171, 194));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(59, 82, 133));
            material.Transparency = 0.90;
            material.PolishAmount = 0.90;
            material.ClarityAmount = 0.25;
            break;
          case WaterTintEnableType.ReflectingPool:
            material.FresnelEnabled = true;
            material.Diffuse = new Color4f(SD.Color.FromArgb(35, 48, 78));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(139, 155, 159));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(255, 255, 255));
            material.Transparency = 0.0;
            material.PolishAmount = 1.00;
            material.ClarityAmount = 0.00;
            break;
          case WaterTintEnableType.River:
            material.FresnelEnabled = false;
            material.Diffuse = new Color4f(SD.Color.FromArgb(63, 86, 139));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(206, 203, 169));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(93, 171, 194));
            material.Transparency = 0.75;
            material.PolishAmount = 0.90;
            material.ClarityAmount = 0.40;
            break;
          case WaterTintEnableType.Lake:
            material.FresnelEnabled = false;
            material.Diffuse = new Color4f(SD.Color.FromArgb(48, 65, 106));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(206, 203, 169));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(93, 171, 194));
            material.Transparency = 0.75;
            material.PolishAmount = 0.90;
            material.ClarityAmount = 0.75;
            break;
          case WaterTintEnableType.Ocean:
            material.FresnelEnabled = false;
            material.Diffuse = new Color4f(SD.Color.FromArgb(35, 48, 78));
            material.ReflectivityColor = new Color4f(SD.Color.FromArgb(93, 171, 194));
            material.TransparencyColor = new Color4f(SD.Color.FromArgb(59, 82, 133));
            material.Transparency = 0.90;
            material.PolishAmount = 0.90;
            material.ClarityAmount = 0.80;
            break;
          case WaterTintEnableType.Custom:
            if (asset.FindByName(Water.WaterTintColor) is AssetPropertyDoubleArray4d diffuse)
            {
              material.FresnelEnabled = false;
              material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());
              material.Diffuse = ToColor4f(diffuse);
              material.ReflectivityColor = ToColor4f(diffuse);
              material.TransparencyColor = ToColor4f(diffuse);
              material.Transparency = 0.75;
              material.PolishAmount = 0.90;
              material.ClarityAmount = 0.75;
            }
            break;
        }
      }

      if (asset.FindByName(Water.WaterBumpAmount) is AssetPropertyDouble bumpAmount)
        material.BumpTextureAmount = bumpAmount.Value;

      if (asset.FindByName(Water.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
      {
        if (asset.FindByName(Water.CommonTintColor) is AssetPropertyDoubleArray4d tint)
        {
          var tintColor = ToColor4f(tint);
          material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
        }
      }

      material.Shine = material.PolishAmount;
    }

    static void GetMirrorSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Plane;

      material.Shine = 1.0;
      material.Reflectivity = 1.0;
      material.PolishAmount = 1.0;

      if (asset.FindByName(Mirror.MirrorTintcolor) is AssetPropertyDoubleArray4d mirrorTintColor)
      {
        material.Diffuse = ToColor4f(mirrorTintColor);
        material.DiffuseTexture = ToSimulatedTexture(mirrorTintColor.GetSingleConnectedAsset());
        material.ReflectivityColor = material.Diffuse;
      }

      if (asset.FindByName(Mirror.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
      {
        if (asset.FindByName(Mirror.CommonTintColor) is AssetPropertyDoubleArray4d tint)
        {
          var tintColor = ToColor4f(tint);
          material.ReflectivityColor = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
        }
      }
    }

    static void GetMasonryCMUSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(MasonryCMU.MasonryCMUColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());

        if (asset.FindByName(MasonryCMU.CommonTintToggle) is AssetPropertyBoolean tintToggle && tintToggle.Value)
        {
          if (asset.FindByName(MasonryCMU.CommonTintColor) is AssetPropertyDoubleArray4d tint)
          {
            var tintColor = ToColor4f(tint);
            material.Diffuse = new Color4f(material.Diffuse.R * tintColor.R, material.Diffuse.G * tintColor.G, material.Diffuse.B * tintColor.B, material.Diffuse.A * tintColor.A);
          }
        }

        material.ReflectivityColor = material.Diffuse;
      }

      if (asset.FindByName(MasonryCMU.MasonryCMUPatternMap) is AssetPropertyReference bumpMap)
        material.BumpTexture = ToSimulatedTexture(bumpMap.GetSingleConnectedAsset());

      if (asset.FindByName(MasonryCMU.MasonryCMUPatternHeight) is AssetPropertyDouble bumpAmount)
        material.BumpTextureAmount = bumpAmount.Value * 0.5;

      double polish = 0.0;
      if (asset.FindByName(MasonryCMU.MasonryCMUApplication) is AssetPropertyInteger application)
      {
        switch ((MasonryCMUApplicationType) application.Value)
        {
          case MasonryCMUApplicationType.Glossy: polish = 0.85; break;
          case MasonryCMUApplicationType.Matte: polish = 0.3; break;
          case MasonryCMUApplicationType.Unfinished: polish = 0.1; break;
        }
      }

      if (asset.FindByName(MasonryCMU.MasonryCMUType) is AssetPropertyInteger type)
      {
        switch ((MasonryCMUType) type.Value)
        {
          case MasonryCMUType.Cmu: break;
          case MasonryCMUType.Masonry: break;
        }
      }

      material.Shine = polish;
      material.Specular = new Color4f((float) polish, (float) polish, (float) polish, 1.0f);
      material.Reflectivity = polish;

      material.FresnelEnabled = true;
      material.PolishAmount = polish;
      material.ClarityAmount = 1.0;
    }

    static void GetPrismOpaqueSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(AdvancedOpaque.OpaqueAlbedo) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedOpaque.OpaqueLuminance) is AssetPropertyDouble luminance)
      {
        material.DisableLighting = luminance.Value > 0.0;
        if (material.DisableLighting)
        {
          if (asset.FindByName(AdvancedOpaque.OpaqueLuminanceModifier) is AssetPropertyDoubleArray4d emission)
          {
            var luminanceFactor = Arithmetic.Clamp(luminance.Value, 0.0, 2000.0) / 2000.0;
            material.Emission = ToColor4f(emission, luminanceFactor);
          }
        }
      }

      if (asset.FindByName(AdvancedOpaque.SurfaceRoughness) is AssetPropertyDouble roughness)
      {
        material.Shine = 1.0 - roughness.Value;
        material.PolishAmount = 1.0 - roughness.Value;
      }

      if (asset.FindByName(AdvancedOpaque.SurfaceNormal) is AssetPropertyReference normal)
      {
        material.BumpTexture = ToSimulatedTexture(normal.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedOpaque.SurfaceCutout) is AssetPropertyReference cutout)
      {
        material.OpacityTexture = ToSimulatedTexture(cutout.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedOpaque.SurfaceAlbedo) is AssetPropertyDoubleArray4d specular)
      {
        material.Reflectivity = 1.0;
        material.ReflectivityColor = ToColor4f(specular);
      }
    }

    static void GetPrismTransparentSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Plane;

      if (asset.FindByName(AdvancedTransparent.TransparentColor) is AssetPropertyDoubleArray4d transparency)
      {
        material.TransparencyColor = ToColor4f(transparency);
      }

      if (asset.FindByName(AdvancedTransparent.TransparentIor) is AssetPropertyDouble ior)
      {
        material.Transparency = 0.92;
        material.Ior = ior.Value;
        material.FresnelEnabled = true;
      }

      if (asset.FindByName(AdvancedTransparent.SurfaceRoughness) is AssetPropertyDouble roughness)
      {
        material.Shine = 1.0 - roughness.Value;
        material.PolishAmount = 1.0 - roughness.Value;
      }

      if (asset.FindByName(AdvancedTransparent.SurfaceNormal) is AssetPropertyReference normal)
      {
        material.BumpTexture = ToSimulatedTexture(normal.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedTransparent.SurfaceCutout) is AssetPropertyReference cutout)
      {
        material.OpacityTexture = ToSimulatedTexture(cutout.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedTransparent.SurfaceAlbedo) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());
      }
    }

    static void GetPrismMetalSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Cube;

      if (asset.FindByName(AdvancedMetal.MetalF0) is AssetPropertyDoubleArray4d diffuse)
      {
        material.Diffuse = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedMetal.SurfaceRoughness) is AssetPropertyDouble roughness)
      {
        material.Shine = 1.0 - roughness.Value;
        material.PolishAmount = 1.0 - roughness.Value;
      }

      if (asset.FindByName(AdvancedMetal.SurfaceNormal) is AssetPropertyReference normal)
      {
        material.BumpTexture = ToSimulatedTexture(normal.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedMetal.SurfaceCutout) is AssetPropertyReference cutout)
      {
        material.OpacityTexture = ToSimulatedTexture(cutout.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedMetal.SurfaceAlbedo) is AssetPropertyDoubleArray4d albeldo)
      {
        material.Reflectivity = 0.25;
        material.ReflectivityColor = ToColor4f(albeldo);
      }
    }

#if REVIT_2020
    static void GetPrismGlazingSchemaParameters(Asset asset, ref BasicMaterialParameters material)
    {
      material.PreviewGeometryType = RenderMaterial.PreviewGeometryType.Plane;

      material.Transparency = 0.92;
      material.Ior = 1.52;
      material.FresnelEnabled = true;

      if (asset.FindByName(AdvancedGlazing.GlazingTransmissionColor) is AssetPropertyDoubleArray4d diffuse)
      {
        material.TransparencyColor = ToColor4f(diffuse);
        material.DiffuseTexture = ToSimulatedTexture(diffuse.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedGlazing.GlazingTransmissionRoughness) is AssetPropertyDouble clarity)
        material.ClarityAmount = 1.0 - clarity.Value;

      if (asset.FindByName(AdvancedGlazing.GlazingF0) is AssetPropertyDoubleArray4d emission)
        material.Emission = ToColor4f(emission);

      if (asset.FindByName(AdvancedGlazing.SurfaceRoughness) is AssetPropertyDouble roughness)
      {
        material.Shine = 1.0 - roughness.Value;
        material.Reflectivity = 1.0 - roughness.Value;
        material.PolishAmount = 1.0 - roughness.Value;
      }

      if (asset.FindByName(AdvancedGlazing.SurfaceNormal) is AssetPropertyReference normal)
      {
        material.BumpTexture = ToSimulatedTexture(normal.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedGlazing.SurfaceCutout) is AssetPropertyReference cutout)
      {
        material.OpacityTexture = ToSimulatedTexture(cutout.GetSingleConnectedAsset());
      }

      if (asset.FindByName(AdvancedGlazing.SurfaceAlbedo) is AssetPropertyDoubleArray4d albeldo)
      {
        material.Reflectivity = 1.0;
        material.ReflectivityColor = ToColor4f(albeldo);
      }
    }
#endif
#endif
  }
}
