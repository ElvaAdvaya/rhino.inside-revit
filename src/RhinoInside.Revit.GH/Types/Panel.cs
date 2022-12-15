using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Panel")]
  public class Panel : FamilyInstance
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB.Panel ||
             (element is ARDB.FamilyInstance instance && instance.Symbol.Family.IsCurtainPanelFamily);
    }

    public Panel() { }
    public Panel(ARDB.FamilyInstance value) : base(value) { }

    public override ElementType Type
    {
      get
      {
        if
        (
          Value is ARDB.Panel panel &&
          panel.Document.GetElement(panel.FindHostPanel()) is ARDB.HostObject host
        )
        {
          return ElementType.FromElementId(panel.Document, host.GetTypeId()) as ElementType;
        }
        else return base.Type;
      }
      set
      {
        if
        (
          Value is ARDB.Panel panel &&
          panel.Document.GetElement(panel.FindHostPanel()) is ARDB.HostObject host &&
          value?.Value is ARDB.HostObjAttributes hostType
        )
        {
          AssertValidDocument(value, nameof(Type));
          InvalidateGraphics();

          host.ChangeTypeId(hostType.Id);
        }
        else base.Type = value;
      }
    }
  }

  [Kernel.Attributes.Name("Panel Type")]
  public class PanelType : FamilySymbol
  {
    protected override Type ValueType => typeof(ARDB.PanelType);
    public new ARDB.PanelType Value => base.Value as ARDB.PanelType;

    public PanelType() { }
    public PanelType(ARDB.PanelType value) : base(value) { }
  }
}
