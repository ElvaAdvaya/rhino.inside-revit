using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  using ARDB_SectionBox = ARDB.Element;

  [Kernel.Attributes.Name("Section Box")]
  public class SectionBox : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB_SectionBox);
    public new ARDB_SectionBox Value => base.Value as ARDB_SectionBox;

    protected override bool SetValue(ARDB_SectionBox element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_SectionBox element)
    {
      return element.GetType() == typeof(ARDB_SectionBox) &&
             element.Category?.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_SectionBox;
    }

    public SectionBox() { }
    public SectionBox(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public SectionBox(ARDB_SectionBox box) : base(box)
    {
      if (!IsValidElement(box))
        throw new ArgumentException("Invalid Element", nameof(box));
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var box = Box;
      if (box.IsValid)
        args.Pipeline.DrawBox(box, args.Color, args.Thickness);
    }
    #endregion

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var box = Box;
      return box.IsValid ? box.GetBoundingBox(xform) : NaN.BoundingBox;
    }

    #region Properties
    public override Box Box
    {
      get
      {
        if (Value is ARDB_SectionBox box)
        {
          if (box.GetFirstDependent<ARDB.View>() is ARDB.View3D view)
          {
            var sectionBox = view.GetSectionBox();
            sectionBox.Enabled = true;
            return sectionBox.ToBox();
          }
        }

        return NaN.Box;
      }
    }

    public override Plane Location
    {
      get
      {
        var box = Box;
        return box.IsValid ? box.Plane : NaN.Plane;
      }
    }
    #endregion
  }
}
