using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.Native;
using RhinoInside.Revit.Settings;
using UIX = RhinoInside.Revit.External.UI;
using ASX = RhinoInside.Revit.External.ApplicationServices;

namespace RhinoInside.Revit
{
  enum AddinStartupMode
  {
    Cancelled = -2,
    Disabled = -1,
    Default = 0,
    WhenNeeded = 1,
    AtStartup = 2,
    Scripting = 3
  }

  public class AddIn : UIX.ExternalApplication
  {
    #region AddinInfo
    public static string AddinCompany => "McNeel";
    public static string AddinName => "Rhino.Inside";
    #endregion

    #region Status
    internal enum Status
    {
      Crashed       = int.MinValue,     // Loaded and crashed
      Failed        = int.MinValue + 1, // Failed to load
      Obsolete      = int.MinValue + 2, // Rhino.Inside is obsolete version
      Expired       = int.MinValue + 3, // License is expired

      Unavailable   = 0,                // Not installed or not supported version
      Available     = 1,                // Available to load
      Ready         = int.MaxValue,     // Fully functional
    }
    static Status status = default;

    internal static Status CurrentStatus
    {
      get =>  status;

      set
      {
        if (status < Status.Available && value > status)
          throw new ArgumentException();

        status = value;
      }
    }
    #endregion

    #region StartupMode
    static AddinStartupMode GetStartupMode()
    {
      if (!Enum.TryParse(Environment.GetEnvironmentVariable("RhinoInside_StartupMode"), out AddinStartupMode mode))
        mode = AddinStartupMode.Default;

      if (mode == AddinStartupMode.Default)
        mode = AddinStartupMode.WhenNeeded;

      return mode;
    }
    internal static readonly AddinStartupMode StartupMode = GetStartupMode();
    #endregion

    #region Constructor
    static readonly string SystemDir =
#if DEBUG
      Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\McNeel\Rhinoceros\7.0-WIP-Developer-Debug-trunk\Install", "Path", null) as string ??
#endif
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System");

    internal static readonly string RhinoExePath = Path.Combine(SystemDir, "Rhino.exe");
    internal static readonly FileVersionInfo RhinoVersionInfo = File.Exists(RhinoExePath) ? FileVersionInfo.GetVersionInfo(RhinoExePath) : null;
    static readonly Version MinimumRhinoVersion = new Version(7, 3, 0);
    static readonly Version RhinoVersion = new Version
    (
      RhinoVersionInfo?.FileMajorPart ?? 0,
      RhinoVersionInfo?.FileMinorPart ?? 0,
      RhinoVersionInfo?.FileBuildPart ?? 0,
      RhinoVersionInfo?.FilePrivatePart ?? 0
    );

    static AddIn()
    {
      if (StartupMode == AddinStartupMode.Cancelled)
        return;

      if (RhinoVersion >= MinimumRhinoVersion)
        status = Status.Available;

      if (DaysUntilExpiration < 1)
        status = Status.Obsolete;

      if (!NativeLoader.IsolateOpenNurbs())
        status = Status.Unavailable;
    }

    public AddIn() : base(new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74")) => Instance = this;

    ~AddIn() => Instance = default;

    internal static AddIn Instance { get; set; }
    public static AddInId Id => Instance;
    #endregion

    #region IExternalApplication Members
    internal static UIControlledApplication ApplicationUI { get; private set; }

    protected override Result OnStartup(UIControlledApplication uiCtrlApp)
    {
      if (!CanLoad(uiCtrlApp))
        return Result.Failed;

      if (StartupMode == AddinStartupMode.Cancelled)
        return Result.Cancelled;

      // Report if opennurbs.dll is loaded
      NativeLoader.SetStackTraceFilePath
      (
        Path.ChangeExtension(uiCtrlApp.ControlledApplication.RecordingJournalFilename, "log.md")
      );

      NativeLoader.ReportOnLoad("opennurbs.dll", enable: true);

      AssemblyResolver.Enabled = true;
      ApplicationUI = uiCtrlApp;

      // Initialize UI framework provided by Rhino
      EtoFramework.Init();

      // Register Revit Failures
      External.DB.ExternalFailures.CreateFailureDefinitions();

      if (uiCtrlApp.IsLateAddinLoading)
      {
        EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs> applicationIdling = null;
        uiCtrlApp.Idling += applicationIdling = (sender, args) =>
        {
          if (sender is UIApplication app)
          {
            uiCtrlApp.Idling -= applicationIdling;
            DoStartUp(app.Application);
          }
        };
      }
      else
      {
        EventHandler<ApplicationInitializedEventArgs> applicationInitialized = null;
        uiCtrlApp.ControlledApplication.ApplicationInitialized += applicationInitialized = (sender, args) =>
        {
          uiCtrlApp.ControlledApplication.ApplicationInitialized -= applicationInitialized;
          DoStartUp(sender as Autodesk.Revit.ApplicationServices.Application);
        };
      }

      // initialize the Ribbon tab and first panel
      RibbonPanel addinRibbon;
      if (AddinOptions.Session.CompactTab)
      {
        addinRibbon = uiCtrlApp.CreateRibbonPanel(AddIn.AddinName);

        // Add launch RhinoInside push button,
        UI.CommandStart.CreateUI(addinRibbon);
        // addin options, has Eto window and requires Eto to be loaded
        UI.CommandAddinOptions.CreateUI(addinRibbon);
      }
      else
      {
        uiCtrlApp.CreateRibbonTab(AddIn.AddinName);
        addinRibbon = uiCtrlApp.CreateRibbonPanel(AddIn.AddinName, "More");

        // Add launch RhinoInside push button,
        UI.CommandStart.CreateUI(addinRibbon);
        // add slideout and the rest of the buttons
      }

      // about and help links
      addinRibbon.AddSlideOut();
      UI.CommandAbout.CreateUI(addinRibbon);
      UI.CommandGuides.CreateUI(addinRibbon);
      UI.CommandForums.CreateUI(addinRibbon);
      UI.CommandHelpLinks.CreateUI(addinRibbon);
      if (!AddinOptions.Session.CompactTab)
      {
        addinRibbon.AddSeparator();
        UI.CommandAddinOptions.CreateUI(addinRibbon);
      }

      // add option change listeners
      AddinOptions.UpdateChannelChanged += AddinOptions_UpdateChannelChanged;
      // check for updates if requested (buttons must be created by now)
      if (AddinOptions.Current.CheckForUpdatesOnStartup)
        CheckUpdates();

      // automatically load Rhino?
      if (AddinOptions.Session.LoadOnStartup)
        LoadRhinoOnStartup(uiCtrlApp);

      return Result.Succeeded;
    }

    private void AddinOptions_UpdateChannelChanged(object sender, EventArgs e) => CheckUpdates();

    static void CheckUpdates()
    {
      AddinUpdater.GetReleaseInfo(
        (ReleaseInfo releaseInfo) => {
          // if release info is received,
          if (releaseInfo != null)
          {
            // if current version on the active update channel is newer
            if (releaseInfo.Version > Version)
            {
              // ask UI to notify user of updates
              if (!AddinOptions.Session.CompactTab)
                UI.CommandStart.NotifyUpdateAvailable(releaseInfo);
              UI.CommandAddinOptions.NotifyUpdateAvailable(releaseInfo);
              return;
            }
          }
          // otherwise clear updates
          UI.CommandStart.ClearUpdateNotifiy();
          UI.CommandAddinOptions.ClearUpdateNotifiy();
        }
      );
    }

    void DoStartUp(Autodesk.Revit.ApplicationServices.Application app)
    {
      Revit.ActiveUIApplication = new UIApplication(app);

      if (StartupMode < AddinStartupMode.AtStartup)
        return;

      if (Revit.OnStartup(ApplicationUI) == Result.Succeeded)
      {
        if (StartupMode == AddinStartupMode.Scripting)
          Revit.ActiveUIApplication.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
      }
    }

    static async void LoadRhinoOnStartup(UIControlledApplication uiCtrlApp)
    {
      // wait for Revit to be ready
      await External.ActivationGate.Yield();

      // ask to load rhino
      UI.CommandStart.Start(new UI.RibbonHandler(uiCtrlApp));
    }

    protected override Result OnShutdown(UIControlledApplication applicationUI)
    {
      try
      {
        return Revit.OnShutdown(applicationUI);
      }
      catch
      {
        return Result.Failed;
      }
      finally
      {
        ApplicationUI = null;
        AssemblyResolver.Enabled = false;
      }
    }

    public override bool CatchException(Exception e, UIApplication app, object sender)
    {
      // There is a wild pointer somewhere, is better to close Revit.
      bool fatal = e is AccessViolationException;

      if (fatal)
        CurrentStatus = Status.Crashed;

      var RhinoInside_dmp = Path.Combine
      (
        Path.GetDirectoryName(app.Application.RecordingJournalFilename),
        Path.GetFileNameWithoutExtension(app.Application.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
      );

      return MiniDumper.Write(RhinoInside_dmp);
    }

    public override void ReportException(Exception e, UIApplication app, object sender)
    {
      // A serious error has occurred. The current action has ben cancelled.
      // It is stringly recommended that you save your work in a new file before continuing.
      //
      // Would you like to save a recovery file? "{TileName}(Recovery)".rvt

      // Show the most inner exception
      while (e.InnerException is object)
        e = e.InnerException;

      if (MessageBox.Show
      (
        caption: $"{app.ActiveAddInId.GetAddInName()} {Version} - Oops! Something went wrong :(",
        icon: MessageBoxImage.Error,
        messageBoxText: $"'{e.GetType().FullName}' at {e.Source}." + Environment.NewLine +
                        Environment.NewLine + e.Message + Environment.NewLine +
                        Environment.NewLine + "Do you want to report this problem by email to tech@mcneel.com?",
        button: MessageBoxButton.YesNo,
        defaultResult: MessageBoxResult.Yes
      ) == MessageBoxResult.Yes)
      {
        var RhinoInside_dmp = Path.Combine
        (
          Path.GetDirectoryName(app.Application.RecordingJournalFilename),
          Path.GetFileNameWithoutExtension(app.Application.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
        );

        ErrorReport.SendEmail
        (
          app,
          $"Rhino.Inside Revit failed - {e.GetType().FullName}",
          false,
          new string[]
          {
            app.Application.RecordingJournalFilename,
            RhinoInside_dmp
          }
        );
      }
    }
    #endregion

    #region Version
    static bool CheckIsExpired(bool quiet = true)
    {
      if (DaysUntilExpiration > 0 && quiet)
        return false;

      using
      (
        var taskDialog = new TaskDialog("Days left")
        {
          Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
          MainIcon = UIX.TaskDialogIcons.IconInformation,
          TitleAutoPrefix = true,
          AllowCancellation = true,
          MainInstruction = DaysUntilExpiration < 1 ?
          "Rhino.Inside WIP has expired" :
          $"Rhino.Inside WIP expires in {DaysUntilExpiration} days",
          MainContent = "While in WIP phase, you do need to update Rhino.Inside addin at least every 45 days.",
          FooterText = "Current version: " + DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Check for updates…", "Open Rhino.Inside download page");
        if (taskDialog.Show() == TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino.inside-revit/7/wip")) { }
        }
      }

      return DaysUntilExpiration < 1;
    }

    static bool IsValid(ASX.HostServices app)
    {
#if REVIT_2021
      return app.VersionNumber == "2021";
#elif REVIT_2020
      return app.VersionNumber == "2020";
#elif REVIT_2019
      return app.VersionNumber == "2019";
#elif REVIT_2018
      return app.VersionNumber == "2018";
#elif REVIT_2017
      return app.VersionNumber == "2017";
#else
      return false;
#endif
    }

    static bool CanLoad(UIX.UIHostApplication app)
    {
      return IsValid(app.Services);
    }

    internal static Result CheckSetup(UIX.UIHostApplication app)
    {
      var services = app.Services;

      // Check if Rhino.Inside is expired
      if (CheckIsExpired(DaysUntilExpiration > 10))
        return Result.Cancelled;

      // Check if Rhino.exe is a supported version
      if (RhinoVersion < MinimumRhinoVersion)
      {
        using
        (
          var taskDialog = new TaskDialog("Update Rhino")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.UpdateRhino",
            MainIcon = UIX.TaskDialogIcons.IconInformation,
            AllowCancellation = true,
            MainInstruction = "Unsupported Rhino version",
            MainContent = $"Expected Rhino version is ({MinimumRhinoVersion}) or above.",
            ExpandedContent =
            RhinoVersionInfo is null ? "Rhino\n" :
            $"{RhinoVersionInfo.ProductName} {RhinoVersionInfo.ProductMajorPart}\n" +
            $"• Version: {RhinoVersion}\n" +
            $"• Path: '{SystemDir}'" + (!File.Exists(RhinoExePath) ? " (not found)" : string.Empty) + "\n" +
            $"\n{services.VersionName}\n" +
#if REVIT_2019
            $"• Version: {services.SubVersionNumber} ({services.VersionBuild})\n" +
#else
            $"• Version: {revit.VersionNumber} ({revit.VersionBuild})\n" +
#endif
            $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
            $"• Language: {services.Language}",
            FooterText = $"Current Rhino version: {RhinoVersion}"
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Download latest Rhino…");
          if (taskDialog.Show() == TaskDialogResult.CommandLink1)
          {
            using (Process.Start(@"https://www.rhino3d.com/download/rhino/7.0/latest")) { }
          }
        }

        return Result.Cancelled;
      }

      // Check if 'opennurbs.dll' is already loaded
      var openNURBS = LibraryHandle.GetLoadedModule("opennurbs.dll");
      if (openNURBS != LibraryHandle.Zero)
      {
        var openNURBSVersion = FileVersionInfo.GetVersionInfo(openNURBS.ModuleFileName);

        using
        (
          var taskDialog = new TaskDialog($"Rhino.Inside {Version} - openNURBS Conflict")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.OpenNURBSConflict",
            MainIcon = UIX.TaskDialogIcons.IconError,
            TitleAutoPrefix = false,
            AllowCancellation = true,
            MainInstruction = "An unsupported openNURBS version is already loaded. Rhino.Inside cannot run.",
            MainContent = "Please restart Revit and load Rhino.Inside first to work around the problem.",
            FooterText = $"Currently loaded openNURBS version: {openNURBSVersion.FileMajorPart}.{openNURBSVersion.FileMinorPart}.{openNURBSVersion.FileBuildPart}.{openNURBSVersion.FilePrivatePart}"
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "More information…");
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Report Error…", "by email to tech@mcneel.com");
          taskDialog.DefaultButton = TaskDialogResult.CommandLink2;
          switch(taskDialog.Show())
          {
            case TaskDialogResult.CommandLink1:
              using (Process.Start(@"https://www.rhino3d.com/inside/revit/beta/reference/known-issues")) { }
              break;
            case TaskDialogResult.CommandLink2:

              var RhinoInside_dmp = Path.Combine
              (
                Path.GetDirectoryName(services.RecordingJournalFilename),
                Path.GetFileNameWithoutExtension(services.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
              );

              MiniDumper.Write(RhinoInside_dmp);

              ErrorReport.SendEmail
              (
                app,
                $"Rhino.Inside Revit failed - openNURBS Conflict",
                true,
                new string[]
                {
                  services.RecordingJournalFilename,
                  RhinoInside_dmp
                }
              );

              CurrentStatus = Status.Failed;
              break;
          }
        }

        return Result.Cancelled;
      }

      // Disable report opennurbs.dll is loaded 
      NativeLoader.ReportOnLoad("opennurbs.dll", enable: false);

      return Result.Succeeded;
    }

    static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
    public static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());
    public static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
    public static int DaysUntilExpiration => Math.Max(0, 45 - (DateTime.Now - BuildDate).Days);

    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static string DisplayVersion => $"{Version} ({BuildDate})";
    #endregion

    #region Eto UI Framework
    internal static bool IsEtoFrameworkReady { get; set; }  = false;
    static class EtoFramework
    {
      /// <summary>
      /// Initialize the ui framework
      /// This method needs to be independent since at calling of this method,
      /// the CLR runtime expects the Rhino UI framework to be already loaded
      /// </summary>
      public static void Init()
      {
        if (Eto.Forms.Application.Instance is null)
          new Eto.Forms.Application(Eto.Platforms.Wpf).Attach();

        IsEtoFrameworkReady = true;
      }
    }
    #endregion
  }
}
