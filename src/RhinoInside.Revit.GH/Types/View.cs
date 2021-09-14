using System;
using System.Globalization;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("View")]
  public interface IGH_View : IGH_Element { }

  [Kernel.Attributes.Name("View")]
  public class View : Element, IGH_View
  {
    protected override Type ValueType => typeof(DB.View);
    public static explicit operator DB.View(View value) => value?.Value;
    public new DB.View Value => base.Value as DB.View;

    string IGH_Goo.TypeName => Value?.IsTemplate == true ? "Revit View Template" : "Revit View";

    public View() { }
    public View(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public View(DB.View view) : base(view) { }

    public override string DisplayName
    {
      get
      {
        if (Value is DB.View view && !view.IsTemplate && ViewType is ViewType viewType)
        {
          FormattableString formatable = $"{viewType} : {view.Name}";
          return formatable.ToString(CultureInfo.CurrentUICulture);
        }

        return base.DisplayName;
      }
    }

    public ViewType ViewType => Value is DB.View view ? new ViewType(view.ViewType) : default;
  }
}
