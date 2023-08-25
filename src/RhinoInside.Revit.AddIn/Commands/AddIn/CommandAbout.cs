using System;
using System.Reflection;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using static RhinoInside.Revit.Diagnostics;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAbout : Command
  {
    public static string CommandName = "About";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandAbout, AlwaysAvailable>
      (
        name: CommandName,
        iconName: "About-icon.png",
        tooltip: "",
        url: "reference/release-notes"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var details = new StringBuilder();

      var rhino = Core.Distribution.VersionInfo;
      details.AppendLine($"Rhino: {rhino?.ProductVersion} ({rhino?.FileDescription ?? "not found"})");

      var revit = data.Application.Application;
      details.AppendLine($"Revit: {revit.GetSubVersionNumber()} ({revit.VersionBuild})");

      details.AppendLine($"CLR: {ErrorReport.CLRVersion}");
      details.AppendLine($"OS: {Environment.OSVersion}");

      using
      (
        var taskDialog = new TaskDialog("About")
        {
          Id = MethodBase.GetCurrentMethod().DeclaringType.FullName,
          MainIcon = External.UI.TaskDialogIcons.IconInformation,
          TitleAutoPrefix = true,
          AllowCancellation = true,
          MainInstruction = $"Rhino.Inside© for Revit",
          MainContent = $"Rhino.Inside Revit: {Core.DisplayVersion}",
          ExpandedContent = details.ToString(),
          CommonButtons = TaskDialogCommonButtons.Ok,
          DefaultButton = TaskDialogResult.Ok,
          FooterText = "Press CTRL+C to copy this information to Clipboard"
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Web site");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Read license");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "See source code");

        switch (taskDialog.Show())
        {
          case TaskDialogResult.CommandLink1:
            using (System.Diagnostics.Process.Start(Core.WebSite)) { }
            break;
          case TaskDialogResult.CommandLink2:
            using (System.Diagnostics.Process.Start($@"https://github.com/mcneel/rhino.inside-revit/blob/{Core.Version.Major}.x/LICENSE")) { }
            break;
          case TaskDialogResult.CommandLink3:
            using (System.Diagnostics.Process.Start($@"https://github.com/mcneel/rhino.inside-revit/tree/{Core.Version.Major}.x")) { }
            break;
        }
      }

      return Result.Succeeded;
    }
  }
}
