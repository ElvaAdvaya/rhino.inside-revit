using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandStart : Command
  {
    static RibbonPanel rhinoPanel;
    static RibbonPanel grasshopperPanel;

    public static string CommandName => "Start";
    public static string CommandIcon => AddinUpdater.ActiveChannel.IsStable ? "RIR-logo.png" : "RIR-WIP-logo.png";

    /// <summary>
    /// Initialize the Ribbon tab and first panel
    /// </summary>
    /// <param name="uiCtrlApp"></param>
    public static void CreateUI(UIControlledApplication uiCtrlApp)
    {
      CreateMainPanel(uiCtrlApp);

      // add the rest of the ui
      // they will all be 'unavailable' (set by the availability type) since
      // RIR is not loaded yet. This allows keyboard shortcuts to be assigned
      if (!Settings.AddinOptions.Session.CompactTab)
      {
        AddIn.Host.ActivateRibbonTab(AddIn.AddinName);
        CreateRhinoButtons();
        CreateGHButtons();
      }
    }

    static void SetupButton(PushButton pushButton)
    {
      if (AddIn.RhinoVersionInfo is FileVersionInfo rhInfo)
      {
        pushButton.ToolTip = $"Loads {rhInfo.ProductName} inside this Revit session";
        pushButton.LongDescription =
          $"Rhino: {rhInfo.ProductVersion} ({rhInfo.FileDescription}){Environment.NewLine}" +
          $"Rhino.Inside: {AddIn.DisplayVersion}{Environment.NewLine}{rhInfo.LegalCopyright}";
      }

      if (AddIn.StartupMode == AddinStartupMode.Disabled)
      {
        pushButton.Enabled = false;
        pushButton.ToolTip = "Add-In is disabled";
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      if
      (
        (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
        (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
      )
        return ErrorReport.ShowLoadError();

      if (AddIn.CurrentStatus == AddIn.Status.Ready)
      {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          return Rhinoceros.RunCommandAbout();

        if
        (
          Rhinoceros.MainWindow.Visible ||
          Rhinoceros.MainWindow.ActivePopup?.IsInvalid == false
        )
        {
          Rhinoceros.MainWindow.BringToFront();
          return Result.Succeeded;
        }

        AddIn.Host.ActivateRibbonTab(AddIn.AddinName);
        return Result.Succeeded;
      }

      return Start();
    }

    internal static Result Start()
    {
      AddinStarting?.Invoke(null, new AddinStartingArgs());

      var result = Result.Failed;
      var button = RestoreButton(CommandName);

      switch (result = Revit.OnStartup(AddIn.Host))
      {
        case Result.Succeeded:
          // Update Rhino button Tooltip
          button.ToolTip = $"Restores previously visible Rhino windows on top of Revit window";
          button.LongDescription = $"Use CTRL key to open a Rhino model";
          button.ShowText(false);

          var assemblies = AppDomain.CurrentDomain.GetAssemblies();

          if (Settings.AddinOptions.Session.CompactTab)
          {
            AddIn.Host.CreateRibbonTab(AddIn.AddinName);

            // Register UI on Revit
            if (assemblies.Any(x => x.GetName().Name == "RhinoCommon"))
            {
              CreateRhinoButtons();
            }

            if (assemblies.Any(x => x.GetName().Name == "Grasshopper"))
            {
              CreateGHButtons();
            }
          }

          CreateGHScriptButtons();
          UpdateRibbonCompact();

          result = Result.Succeeded;
          break;

        case Result.Cancelled:
          button.Enabled = false;

          if (AddIn.CurrentStatus == AddIn.Status.Unavailable)
            button.ToolTip = "Rhino.Inside failed to found a valid copy of Rhino installed.";
          else if (AddIn.CurrentStatus == AddIn.Status.Obsolete)
            button.ToolTip = "Rhino.Inside has expired.";
          else
            button.ToolTip = "Rhino.Inside load was cancelled.";

          break;

        case Result.Failed:
          button.Enabled = false;
          button.ToolTip = "Rhino.Inside failed to load.";
          return Result.Failed;
      }

      var res = (result == Result.Failed) ? ErrorReport.ShowLoadError() : result;

      AddinStarted?.Invoke(null, new AddinStartedArgs()
      {
        ResultCode = res
      });

      return res;
    }

    #region Events
    public class AddinStartingArgs : EventArgs
    {
    }
    public static event EventHandler<AddinStartingArgs> AddinStarting;

    public class AddinStartedArgs : EventArgs
    {
      public Result ResultCode { get; set; }
    }
    public static event EventHandler<AddinStartedArgs> AddinStarted;
    #endregion

    #region UI Panels and Buttons
    static void CreateMainPanel(UIControlledApplication uiCtrlApp)
    {
      RibbonPanel ribbonPanel;

      void CreateStartButton(string tabName)
      {
        var buttonData = NewPushButtonData<CommandStart, AvailableWhenNotObsolete>(CommandName, CommandIcon, "");
        if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
        {
          StoreButton(CommandName, pushButton);
          SetupButton(pushButton);

          if (AddIn.CurrentStatus >= AddIn.Status.Available && AddIn.StartupMode != AddinStartupMode.Disabled)
          {
            if (Settings.KeyboardShortcuts.RegisterDefaultShortcut(tabName, ribbonPanel.Name, typeof(CommandStart).Name, CommandName, "R#Ctrl+R"))
              External.ActivationGate.Exit += ShowShortcutHelp;
          }
        }

        // add listener for ui compact changes
        Settings.AddinOptions.CompactRibbonChanged += AddinOptions_CompactRibbonChanged;
        Settings.AddinOptions.UpdateChannelChanged += AddinOptions_UpdateChannelChanged;
      }

      if (Settings.AddinOptions.Session.CompactTab)
      {
        ribbonPanel = uiCtrlApp.CreateRibbonPanel(AddIn.AddinName);

        // Add launch RhinoInside push button,
        CreateStartButton("Add-Ins");
        // addin options, has Eto window and requires Eto to be loaded
        CommandAddinOptions.CreateUI(ribbonPanel);
      }
      else
      {
        uiCtrlApp.CreateRibbonTab(AddIn.AddinName);
        ribbonPanel = uiCtrlApp.CreateRibbonPanel(AddIn.AddinName, "More");

        // Add launch RhinoInside push button,
        CreateStartButton(AddIn.AddinName);
        // add slideout and the rest of the buttons
      }

      // about and help links
      ribbonPanel.AddSlideOut();
      CommandAbout.CreateUI(ribbonPanel);
      CommandGuides.CreateUI(ribbonPanel);
      CommandForums.CreateUI(ribbonPanel);
      CommandHelpLinks.CreateUI(ribbonPanel);
      if (!Settings.AddinOptions.Session.CompactTab)
      {
        ribbonPanel.AddSeparator();
        CommandAddinOptions.CreateUI(ribbonPanel);
      }
    }

    static void CreateRhinoButtons()
    {
      rhinoPanel = AddIn.Host.CreateRibbonPanel(AddIn.AddinName, AddIn.RhinoVersionInfo?.ProductName ?? "Rhinoceros");
      CommandRhino.CreateUI(rhinoPanel);
      CommandRhinoOpenViewport.CreateUI(rhinoPanel);
      CommandToggleRhinoPreview.CreateUI(rhinoPanel);
      CommandPython.CreateUI(rhinoPanel);
      rhinoPanel.AddSlideOut();
      CommandImport.CreateUI(rhinoPanel);
      CommandRhinoOptions.CreateUI(rhinoPanel);
    }

    static void CreateGHButtons()
    {
      grasshopperPanel = AddIn.Host.CreateRibbonPanel(AddIn.AddinName, "Grasshopper");
      CommandGrasshopper.CreateUI(grasshopperPanel);
      CommandGrasshopperPreview.CreateUI(grasshopperPanel);
      CommandGrasshopperSolver.CreateUI(grasshopperPanel);
      CommandGrasshopperRecompute.CreateUI(grasshopperPanel);
      CommandGrasshopperBake.CreateUI(grasshopperPanel);
      grasshopperPanel.AddSeparator();
      CommandGrasshopperPlayer.CreateUI(grasshopperPanel);
      grasshopperPanel.AddSlideOut();
      CommandGrasshopperPackageManager.CreateUI(grasshopperPanel);
      CommandGrasshopperFolders.CreateUI(grasshopperPanel);
      CommandGrasshopperCaptureElements.CreateUI(grasshopperPanel);
      CommandGrasshopperReleaseElements.CreateUI(grasshopperPanel);
    }

    static void CreateGHScriptButtons()
    {
      // Script Packages UI
      LinkedScripts.CreateUI(new RibbonHandler(AddIn.Host));
    }
    #endregion

    #region Shortcuts
    static void ShowShortcutHelp(object sender, EventArgs e)
    {
      if (sender is IExternalCommand)
      {
        External.ActivationGate.Exit -= ShowShortcutHelp;

        using
        (
          var taskDialog = new TaskDialog("New Shortcut")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
            MainIcon = UIX.TaskDialogIcons.IconInformation,
            TitleAutoPrefix = true,
            AllowCancellation = true,
            MainInstruction = $"Keyboard shortcut 'R' is now assigned to Rhino",
            MainContent = $"You can use R key to restore previously visible Rhino windows over Revit window every time you need them.",
            FooterText = "This is a one time message",
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Customize keyboard shortcuts…");
          if (taskDialog.Show() == TaskDialogResult.CommandLink1)
          {
            Revit.ActiveUIApplication.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.KeyboardShortcuts));
          }
        }
      }
    }
    #endregion

    #region Update
    private static void AddinOptions_CompactRibbonChanged(object sender, EventArgs e) => UpdateRibbonCompact();

    static void UpdateRibbonCompact()
    {
      // collapse panel if in compact mode
      if (Settings.AddinOptions.Current.CompactRibbon)
      {
        rhinoPanel?.Collapse(AddIn.AddinName);
        grasshopperPanel?.Collapse(AddIn.AddinName);
      }
      else
      {
        rhinoPanel?.Expand(AddIn.AddinName);
        grasshopperPanel?.Expand(AddIn.AddinName);
      }
    }

    private static void AddinOptions_UpdateChannelChanged(object sender, EventArgs e)
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.Image = ImageBuilder.LoadRibbonButtonImage(CommandIcon, true);
        button.LargeImage = ImageBuilder.LoadRibbonButtonImage(CommandIcon);
      }
    }


    public static void NotifyUpdateAvailable(ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!Settings.AddinOptions.IsReadOnly)
      {
        if (RestoreButton(CommandName) is PushButton button)
        {
          ClearUpdateNotifiy();
          button.Highlight();
          button.ToolTip = "New Release Available for Download!\n"
                         + $"Version: {releaseInfo.Version}\n"
                         + button.ToolTip;
        }
      }
    }

    public static void ClearUpdateNotifiy()
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.ClearHighlight();
        // init resets the tooltip to default
        SetupButton(button);
      }
    }
    #endregion
  }
}
