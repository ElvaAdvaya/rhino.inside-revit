using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class BasePoint : GraphicalElementT<Types.IGH_BasePoint, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("16F8DAF7-B63C-4A8B-A2E1-ACA0A08CDCB8");
    protected override string IconTag => "⌖";

    protected override Types.IGH_BasePoint InstantiateT() => new Types.BasePoint();

    public BasePoint() : base
    (
      name: "Base Point",
      nickname: "Base Point",
      description: "Contains a collection of Revit base point elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }

    #region ISelectionFilter
    static readonly ARDB.ElementFilter ElementFilter = External.DB.CompoundElementFilter.Intersect
    (
      new ARDB.ElementIsElementTypeFilter(inverted: true),
      new ARDB.ElementMulticategoryFilter
      (
        new ARDB.BuiltInCategory[]
        {
          ARDB.BuiltInCategory.OST_IOS_GeoSite,       // Internal Origin
          ARDB.BuiltInCategory.OST_ProjectBasePoint,  // Project Base Point
          ARDB.BuiltInCategory.OST_SharedBasePoint    // Survey Point
        }
      )
    );

    public override bool AllowElement(ARDB.Element elem) => ElementFilter.PassesFilter(elem) && Types.Element.FromElement(elem) is Types.IGH_BasePoint;
    #endregion

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      if (MutableNickName)
      {
        var listBox = new ListBox
        {
          BorderStyle = BorderStyle.FixedSingle,
          Width = (int) (250 * GH_GraphicsUtil.UiScale),
          Height = (int) (100 * GH_GraphicsUtil.UiScale),
          SelectionMode = SelectionMode.MultiExtended
        };
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

        Menu_AppendCustomItem(menu, listBox);
        RefreshBasePointsList(listBox);
      }

      base.Menu_AppendPromptOne(menu);
    }

    private void RefreshBasePointsList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.DisplayMember = "DisplayName";
      listBox.Items.Clear();

      {
        var points = new Types.IGH_BasePoint[]
        {
          new Types.InternalOrigin(InternalOriginExtension.Get(doc)),
          new Types.BasePoint(BasePointExtension.GetProjectBasePoint(doc)),
          new Types.BasePoint(BasePointExtension.GetSurveyPoint(doc))
        };

        foreach (var point in points.Where(x => x.IsValid))
          listBox.Items.Add(point);

        var selectedItems = points.Intersect(PersistentData.OfType<Types.IGH_BasePoint>());

        foreach (var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordPersistentDataEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.IGH_BasePoint>());
        OnObjectChanged(GH_ObjectEventType.PersistentData);

        ExpireSolution(true);
      }
    }
    #endregion
  }
}
