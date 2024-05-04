using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using static RhinoInside.Revit.Diagnostics;

namespace RhinoInside.Revit.AddIn.Commands
{
  abstract class CommandHelpLinks : Command
  {
    internal static void CreateUI(RibbonPanel ribbonPanel)
    {
      ribbonPanel.AddStackedItems(
        NewPushButtonData<CommandAPIDocs, AlwaysAvailable>
        (
          name: CommandAPIDocs.CommandName,
          iconName: "Link-icon.png",
          tooltip: "Opens apidocs.co (Revit API documentation) website",
          url: "reference/rir-interface#more-slideout"
        ),
        NewPushButtonData<CommandTheBuildingCoder, AlwaysAvailable>
        (
          name: CommandTheBuildingCoder.CommandName,
          iconName: "Link-icon.png",
          tooltip: "Opens TheBuildingCode website",
          url: "reference/rir-interface#more-slideout"
        ),
        NewPushButtonData<CommandRhinoDevDocs, AlwaysAvailable>
        (
          name: CommandRhinoDevDocs.CommandName,
          iconName: "Link-icon.png",
          tooltip: "Opens Rhino Developer documentation website",
          url: "reference/rir-interface#more-slideout"
        )
      );
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAPIDocs : Command
  {
    public static string CommandName = "Revit API Docs";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Browser.Start($@"https://apidocs.co/apps/revit/{data.Application.Application.VersionNumber}/");
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandTheBuildingCoder : Command
  {
    public static string CommandName = "TheBuildingCoder";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Browser.Start(@"https://thebuildingcoder.typepad.com/");
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoDevDocs : Command
  {
    public static string CommandName = "Rhino Dev Docs";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Browser.Start(@"https://developer.rhino3d.com/");
      return Result.Succeeded;
    }
  }
}
