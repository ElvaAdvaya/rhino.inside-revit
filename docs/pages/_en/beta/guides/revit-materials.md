---
title: Materials
order: 49
group: Modeling
---

Materials are one of the more complicated data types in Revit. They are regularly used to (a) assign graphical properties to Revit elements for drafting (e.g. tile pattern on a bathroom wall), (b) embed architectural finish information in the building model for the purpose of scheduling and takeouts, (c) assign shading (rendering) properties to surfaces for architectural visualizations, and (d) assign physical and (e) thermal properties to elements for mathematical analysis of all kinds.

Therefore a single Material in Revit has 5 main aspects:

- **Identity**
- **Graphics**
- **Shading (Rendering) Properties**
- **Physical Properties**
- **Thermal Properties**

Each one of these aspects is represented by a tab in the Revit material editor window:

![](https://via.placeholder.com/800x100.png?text=Material+Aspects+Tabs)

In the sections below, we will discuss how to deal with all of these 5 aspects using {{ site.terms.rir }}

## Querying Materials

{% capture api_note %}
In Revit API, Materials are represented by the {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %}. The {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %} type in Revit API, handles the *Identity* and *Graphics* of a material and provides methods to query and modify the *Shading*, *Physical*, and *Thermal* properties.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The first challenge is to be able to query available materials in a Revit model or find a specific we want to work with. For this we use the {% include ltr/comp.html uuid='94af13c1-' %} component. The component outputs all the materials in a Revit model by default, and also has optional inputs to filter the existing materials by class or name, and also accepts customs filters as well:

![](https://via.placeholder.com/800x300.png?text=Query+Materials)

{% capture tip_note %}
The Class and Name inputs accept Grasshopper string filtering patterns. See ************
{% endcapture %}
{% include ltr/bubble_note.html note=tip_note %}

### Extracting Materials from Geometry

To extract the set of materials assigned to faces of a geometry, use the *Geometry Materials* component shared here. In this example, a custom component is used to extract the geometry objects from Revit API ({% include api_type.html type='Autodesk.Revit.DB.Solid' title='DB.Solid' %} - See [Extracting Type Geometry by Category]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}#extracting-type-geometry-by-category)). These objects are then passed to the *Geometry Materials* component to extract materials. Finally the *Element.Decompose* component is used to extract the material name.

![]({{ "/static/images/guides/revit-materials01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Geometry Materials.ghuser' name='Geometry Materials' %}

## Material Identity and Graphics

Use the {% include ltr/comp.html uuid='06e0cf55-' %} component to access the material identity and graphics:

![](https://via.placeholder.com/800x300.png?text=Material+Id)

![](https://via.placeholder.com/800x300.png?text=Material+Graphics)

### Modifying Material Identity

{% include ltr/en/wip_note.html %}

### Customizing Material Graphics

{% include ltr/en/wip_note.html %}

## Creating Materials

To quickly create a material using a single color input use the {% include ltr/comp.html uuid='273ff43d-' %} component. This component has been created to help with quickly color coding Revit elements. Avoid using this component on final BIM models since the material is named by the color that is used to create it. {% include ltr/comp_doc.html uuid='273ff43d-' %}

![](https://via.placeholder.com/800x300.png?text=Add+Color+Material)

A better way to create materials is to use the {% include ltr/comp.html uuid='0d9f07e2-' %} component. This ways you can assign an appropriate name to the component:

![](https://via.placeholder.com/800x300.png?text=Add+Material)

## Material Assets

So far, we have learned how to analyze material identify and graphics, and to create simple materials. To be able to take full advantage of the materials in Revit, we need to be familiar with the underlying concepts behind the other three aspects of a material: *Shading*, *Physical*, and *Thermal* properties.

### Assets

Assets are the underlying concept behind the *Shading*, *Physical*, and *Thermal* aspects of a material in Revit. {{ site.terms.rir }} provides a series of components to Create, Modify, and Analyze these assets in a Grasshopper-friendly manner. It also provides components to extract and replace these assets on a Revit material.

Remember that Assets and Materials are different data types. Each Revit Material had identity and graphics properties, and also can be assigned Assets to apply *Shading*, *Physical*, and *Thermal* properties to the Material. Having *Physical*, and *Thermal* assets is completely optional.

{% capture api_note %}
Revit API support for assets is very limited. This note section, attempts to describe the inner-workings of Revit Visual API

#### Shading Assets

All *Shading* assets are of type {% include api_type.html type='Autodesk.Revit.DB.Visual.Asset' title='DB.Visual.Asset' %} and are basically a collection of visual properties that have a name e.g. `generic_diffuse`, a type, and a value. The {% include api_type.html type='Autodesk.Revit.DB.Visual.Asset' title='DB.Visual.Asset' %} has lookup methods to find and return these properties. These properties are wrapped by the type {% include api_type.html type='Autodesk.Revit.DB.Visual.AssetProperty' title='DB.Visual.AssetProperty' %} in Revit API. This type provides getters to extract the value from the property.

&nbsp;

There are many different *Shading* assets in Revit e.g. **Generic**, **Ceramic**, **Metal**, **Layered**, **Glazing** to name a few. Each asset has a different set of properties. To work with these *Shading* assets, we need a way to know the name of the properties that are available for each of the asset types. Revit API provides static classes with static readonly string properties that provide an easy(?) way to get the name of these properties. For example the `GenericDiffuse` property of {% include api_type.html type='Autodesk.Revit.DB.Visual.Generic' title='DB.Visual.Generic' %}, returns the name `generic_diffuse` which is the name of the diffuse property for a **Generic** Shading asset.

&nbsp;

*Shading* assets are then wrapped by {% include api_type.html type='Autodesk.Revit.DB.AppearanceAssetElement' title='DB.AppearanceAssetElement' %} so they can be assigned to a Revit Material ({% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %})

#### Physical and Thermal Assets

*Physical*, and *Thermal* assets are completely different although operating very similarly to *Shading* assets. They are still a collection of properties, however, the properties are modeled as Revit parameters ({% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}) and are collected by an instance of {% include api_type.html type='Autodesk.Revit.DB.PropertySetElement' title='DB.PropertySetElement' %}. Instead of having static classes as accessors for the names, they must be accessed by looking up the parameter based on a built-in Revit parameter e.g. `THERMAL_MATERIAL_PARAM_REFLECTIVITY` of {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %}

&nbsp;

Revit API provides {% include api_type.html type='Autodesk.Revit.DB.StructuralAsset' title='DB.StructuralAsset' %} and {% include api_type.html type='Autodesk.Revit.DB.ThermalAsset' title='DB.ThermalAsset' %} types to provide easy access to the *Physical*, and *Thermal* properties, however, not all the properties are included in these types and the property values are not checked for validity either.

#### Grasshopper as Playground

The Grasshopper definition provided here, has custom python components that help you interrogate the properties of these assets:

&nbsp;

![](https://via.placeholder.com/800x300.png?text=Interrogate+Assets)

&nbsp;

{% include ltr/download_def.html archive='/static/ghdefs/AssetsPlayground.ghx' name='Assets Playground' %}

{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the {% include ltr/comp.html uuid='1f644064-' %} to extract assets of a material:

![](https://via.placeholder.com/800x300.png?text=Extract+Assets)


To replace assets of a material with a different asset, use the {% include ltr/comp.html uuid='2f1ec561-' %} component:

![](https://via.placeholder.com/800x300.png?text=Replace+Assets)

## Shader (Appearance) Assets

There are many *Shading* assets in Revit API. As an example, you can use {% include ltr/comp.html uuid='0f251f87-' %} to create a *Generic* shader asset and assign that to a Revit material using the {% include ltr/comp.html uuid='2f1ec561-' %} component:

![](https://via.placeholder.com/800x300.png?text=Create+Shader)

The {% include ltr/comp.html uuid='5b18389b-' %} and {% include ltr/comp.html uuid='73b2376b-' %} components can be used to easily manipulate an existing asset, or analyze and extract the known property values:

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Shader)

## Texture Assets

Shading assets have a series of properties that can accept a nested asset (called *Texture* assets in this guide). For example, the diffuse property of a **Generic** shading asset can either have a color value, or be connected to another asset of type **Bitmap** (or other *Texture* assets).

{{ site.terms.rir }} provides component to construct and destruct these asset types. The *Shading* asset component also accept a *Texture* asset where applicable. For example, use {% include ltr/comp.html uuid='37b63660-' %} and {% include ltr/comp.html uuid='77b391db-' %} to construct and destruct **Bitmap** texture assets:

![](https://via.placeholder.com/800x300.png?text=Construct+Apply+Texture)

{% include ltr/bubble_note.html note='Note that texture components only add the asset to the Revit model when they are connected to the input property of a Shading asset component' %}


## Physical (Structural) Assets

Use {% include ltr/comp.html uuid='af2678c8-' %} to create a *Physical* asset and assign to a material using {% include ltr/comp.html uuid='2f1ec561-' %} component. Use {% include ltr/comp.html uuid='6f5d09c7-' %} and {% include ltr/comp.html uuid='c907b51e-' %} as inputs, to set the type and behavior of the *Physical* asset, respectively:

![](https://via.placeholder.com/800x300.png?text=Create+Asset)

Use {% include ltr/comp.html uuid='ec93f8e0-' %} and {% include ltr/comp.html uuid='67a74d31-' %} to modify or analyze existing *Physical* assets:

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Asset)

## Thermal Assets

Use {% include ltr/comp.html uuid='bd9164c4-' %} to create a *Thermal* asset and assign to a material using {% include ltr/comp.html uuid='2f1ec561-' %} component. Use {% include ltr/comp.html uuid='9d9d0211-' %} and {% include ltr/comp.html uuid='c907b51e-' %} as inputs, to set the type and behavior of the *Thermal* asset, respectively:

![](https://via.placeholder.com/800x300.png?text=Create+Asset)

Use {% include ltr/comp.html uuid='c3be363d-' %} and {% include ltr/comp.html uuid='2c8f541a-' %} to modify or analyze existing *Thermal* assets:

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Asset)
