using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Grasshopper.Kernel;
using Rhino.PlugIns;
using RhinoInside.Revit.External.UI.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  abstract class CommandGrasshopperPreview : GrasshopperCommand
  {
    public static string CommandName => "GrasshopperPreview";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if REVIT_2018
      var radioData = new RadioButtonGroupData(CommandName);

      if (ribbonPanel.AddItem(radioData) is RadioButtonGroup radioButton)
      {
        CommandGrasshopperPreviewOff.CreateUI(radioButton);
        CommandGrasshopperPreviewWireframe.CreateUI(radioButton);
        CommandGrasshopperPreviewShaded.CreateUI(radioButton);
        StoreButton(CommandName, radioButton);
      }

      CommandStart.AddinStarted += CommandStart_AddinStarted;
#endif
    }

#if REVIT_2018
    private static void CommandStart_AddinStarted(object sender, CommandStart.AddinStartedArgs e)
    {
      if (RestoreButton(CommandName) is RadioButtonGroup radioButton)
      {
        CommandGrasshopperPreviewOff.SetState(radioButton);
        CommandGrasshopperPreviewWireframe.SetState(radioButton);
        CommandGrasshopperPreviewShaded.SetState(radioButton);
      }
      CommandStart.AddinStarted -= CommandStart_AddinStarted;
    }
#endif

    /// <summary>
    /// Available when current Revit document is a project, not a family.
    /// </summary>
    protected new class Availability : GrasshopperCommand.Availability
    {
      public override bool IsCommandAvailable(UIApplication app, DB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        Revit.ActiveUIDocument?.Document?.IsFamilyDocument == false;
    }
  }

#if REVIT_2018
  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewOff : CommandGrasshopperPreview
  {
    public static new string CommandName => "Off";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewOff, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Off.png",
        tooltip: "Don't draw any preview geometry"
      );

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        // add spacing to title to get it to be a consistent width
        pushButton.SetText("   Off    ");
        StoreButton(CommandName, pushButton);
      }
    }

    public static void SetState(RadioButtonGroup radioButtonGroup)
    {
      if (RestoreButton(CommandName) is ToggleButton pushButton)
        if (GH.PreviewServer.PreviewMode == GH_PreviewMode.Disabled)
        {
          radioButtonGroup.Current = pushButton;
        }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Disabled;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewWireframe : CommandGrasshopperPreview
  {
    public static new string CommandName => "Wire";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewWireframe, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Wireframe.png",
        tooltip: "Draw wireframe preview geometry"
      );

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        // add spacing to title to get it to be a consistent width
        pushButton.SetText("  Wire   ");
        StoreButton(CommandName, pushButton);
      }
    }

    public static void SetState(RadioButtonGroup radioButtonGroup)
    {
      if (RestoreButton(CommandName) is ToggleButton pushButton)
        if (GH.PreviewServer.PreviewMode == GH_PreviewMode.Wireframe)
        {
          radioButtonGroup.Current = pushButton;
        }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Wireframe;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.ReadOnly), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPreviewShaded : CommandGrasshopperPreview
  {
    public static new string CommandName => "Shaded";

    public static void CreateUI(RadioButtonGroup radioButtonGroup)
    {
      var buttonData = NewToggleButtonData<CommandGrasshopperPreviewShaded, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Shaded.png",
        tooltip: "Draw shaded preview geometry"
      );

      if (radioButtonGroup.AddItem(buttonData) is ToggleButton pushButton)
      {
        StoreButton(CommandName, pushButton);
      }
    }

    public static void SetState(RadioButtonGroup radioButtonGroup)
    {
      if (RestoreButton(CommandName) is ToggleButton pushButton)
        if (GH.PreviewServer.PreviewMode == GH_PreviewMode.Shaded)
        {
          radioButtonGroup.Current = pushButton;
        }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      GH.PreviewServer.PreviewMode = GH_PreviewMode.Shaded;
      data.Application.ActiveUIDocument.RefreshActiveView();
      return Result.Succeeded;
    }
  }
#endif
}
