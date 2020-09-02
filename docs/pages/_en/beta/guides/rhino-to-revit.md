---
title: Rhino Geometry to Revit
order: 21
---

{% include ltr/en/wip_note.html %}

In this guide we will take a look at how to send Rhino geometry to Revit using Grasshopper. {{ site.terms.rir }} allows Rhino shapes and forms to be encoded into structured Revit elements.  It is important to note that the easiest and quickest way of moving geometry into Revit may not be the best method. Determining which the final goal of the forms in Revit can improve the quality of the final Revit data structure and improve project efficiency.

Each of these processes increases the integration within a BIM model, but also each strategy takes just a bit more planning then the next.  The 3 main ways to classify Rhino geometry in Revit are: 

1. The [Directshapes](#rhino-objects-as-directshapes) can be quite fast and takes the least amount of organizing geomtry objects. The limited orgainzation and speed make the directshaps process best for temprary drawing sets such as ceompetitions and early design presetnations.
1. Orgainzing as a part of [Loadable Families with subcategories](#rhino-objects-as-loadable-families) works well for standalone elements in a model or elemetns that might be ordered or customer fabiracated.  Being part of a Family, these objects could have their own set of drawings int he set, in addition to being part of the larger project drawings.
1. [Use Rhino geomtry to generate Native Revit elements](#using-revit-built-in-system-families) is the best way to generate final elements if possible for integration with the rest o fhte Revit team.  These objects potaetially can be editing without the precess of Rhino/GH.  While the objects thta can be created in this way is limited, the resulting elements are native Revit elements.
  
Here is a Rhino model for a competition:
![Competition model in Rhino]({{ "/static/images/guides/rhino-office-display.jpg" | prepend: site.baseurl }})

Through a simple Grasshopper script, objects can be categorized for elevations:
![A Quick Elevation in Revit]({{ "/static/images/guides/revit-office-elevation.jpg" | prepend: site.baseurl }})

And plan views:
![A Quick Plan in Revit]({{ "/static/images/guides/revit-office-plan.jpg" | prepend: site.baseurl }})

### Rhino objects as DirectShapes

Directshapes are the most obvious and many times the easiest way to get Geometry from Rhino into Revit. It is normally where everyone starts.  While it is the easiest, it it is important to understand that DirectShapes may not always be the best way to transfer Rhino Goemetry into Revit. 

Good reasons for Directshapes include:
1. Temporary models used in a competition submission for quick drawings.
1. Placeholders for part of the building that is still changing during design development.  For instance, while the floor plates might be done, the facade might be in flux in Grasshopper.  Using a directshape as a placeholder for elevations and other design development drawings may work well.
1. A completely bespoke component or assembly that cannot be modeled using Revit native Families.

{% include youtube_player.html id="HAMPkiA5_Ug" %}

Directshapes can be placed in any top level Category enabling graphic and material control thru Graphic Styles.  
![Create a Directshape]({{ "/static/images/guides/rhino-to-revit-directshape.png" | prepend: site.baseurl }})

For additional graphic controls between elements within a category, [Rule-based View Filters](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2019/ENU/Revit-DocumentPresent/files/GUID-145815E2-5699-40FE-A358-FFC739DB7C46-htm.html) can be applied with custom parameter values. Directshapes cannot be places in Sub-Categories. 
![Add a Shared Parameter for a filter]({{ "/static/images/guides/directshape-filter-gh.png" | prepend: site.baseurl }})

In addition to pushing Rhino gemoetry into Revit as single direct shapes, it is also possible to create directshape types that can be inserted multiple times for repetative elements.
![Insert multiple directshape instances]({{ "/static/images/guides/rhino-to-revit-directshape-instance.png" | prepend: site.baseurl }})


{% capture api_warning_note %}
Directshapes created from smooth NURBS surfaces in Rhino may some in as smooth solid or converted to a mesh by Revit.  If the NURBS is converted to a mesh, that is a symptom that the NURBS geometry was rejected by Revit.  There are many reasons for this, but very often this projecm can be fixed in Rhino.
{% endcapture %}
{% include ltr/warning_note.html note=api_warning_note %}


### [Rhino objects as Loadable Families]()

Rhino objects set in a family allow to insert multiple instances of an object and also allow for [subcategories](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2018/ENU/Revit-Customize/files/GUID-8C1F9882-E4AB-4E03-A735-8C44F19E194B-htm.html).  You can use subcategories can be used to control the visibility and graphics of portions of a family within a top level category. It is important to undertand this only can be done in a Family.  It is also worth thinking if instances need to be used, or larger more complex Families would be enough?

Revit recomends loadable families when:
* Building components that would usually be purchased, delivered, and installed in and around a building, such as windows, doors, casework, fixtures, furniture, and planting.
* System components that would usually be purchased, delivered, and installed in and around a building, such as boilers, water heaters, air handlers, and plumbing fixtures.
* Some annotation elements that are routinely customized, such as symbols and title blocks.
* Rhino gemeotry that is complex and may need to be placed in Revit for drawings.

Wrapping Rhino gemoetry inside Loadable Families have many advantages:
* Repeated objects can be inserted mutiple times allowing forms to be scheduled and counted correctly
* Forms in loadable families can be edited by Revit if needed.
* Forms placed inside Family/Types can be placed in sub-Categories for further graphics control and scheduling. 

For example, here is an exterior walkway canopy in Rhino.  It is a structure that will built by a specialty fabricator and brought out to the site.  The small footing will be poored on-site.  So here the footings are part of one family and the rest of the structure part of another family.

![An Exterior Walkway in Rhino]({{ "/static/images/guides/canopy-rhino.png" | prepend: site.baseurl }})

By using mapping layers in Rhino to sub-categories in Revit, graphics and materials can be controlled in Revit per view:

![Plan view with Sub-categories]({{ "/static/images/guides/canopy-plan.png" | prepend: site.baseurl }})

![Elevation view with Sub-categories]({{ "/static/images/guides/canopy-elevation.png" | prepend: site.baseurl }})

Th process of creating sub categories is here in this video:

{% include youtube_player.html id="z57Ic0-4r2I" %}

Use the subcategory component to *wrap* the objects in the subcategory before sending it to a Family definition:
![Creating subcategory]({{ "/static/images/guides/subcategory-rhino-revit-gh.png" | prepend: site.baseurl }})

The subcategory component will create a new sub-category if it does not allready exist.

Subcategories properties can be editing in the Object Styles dialog:
![An Exterior Walkway in Rhino]({{ "/static/images/guides/revit-objectstyles.jpg" | prepend: site.baseurl }})

Subcategories can also be used with [Rule-based View Filters](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2019/ENU/Revit-DocumentPresent/files/GUID-145815E2-5699-40FE-A358-FFC739DB7C46-htm.html) for additional graphic control.

### Using Revit built-in System Families

Trying to work within Categories of objects which include built-in Revit systems Families such as Walls, Floors, Ceiling and Roofs can take the most amount of thought. But the extra effeort can be worth it.  Native Revit elements can: 

1. Native elements work best in the BIM schema including Graphic control, Materials and all office standard BIM parameters as any native objects would have.
1. Most of the objects can be edited after Rhino has been disconnected.  The objects may have dimensions attached to them. The elements may be used to host other objects.
1. Many Revit users downstream may not realize they were created with Rhino/GH.

Here is video on Creating native Levels, Floors, Columns and Facade panels:

{% include youtube_player.html id="cc3WLvGkWcc" %}

For more information on each type of Revit element, see the [Modeling section in Guides]({{ site.baseurl }}{% link _en/beta/guides/index.md %})
