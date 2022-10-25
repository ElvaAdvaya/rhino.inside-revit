using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;

namespace RhinoInside.Revit.External.ApplicationServices
{
  using Extensions;

  #region HostServices
  public abstract class HostServices : IDisposable
  {
    protected HostServices() { }
    public abstract void Dispose();

    public static implicit operator HostServices(Application value) => new HostServicesU(value);
    public static implicit operator HostServices(ControlledApplication value) => new HostServicesC(value);

    public abstract object Value { get; }

    #region Version
    public abstract string VersionName { get; }
    public abstract string VersionNumber { get; }
    public abstract string VersionBuild { get; }
    public abstract string SubVersionNumber { get; }

    public abstract ProductType Product { get; }
    public abstract LanguageType Language { get; }
    #endregion

    #region Journaling
    public abstract string RecordingJournalFilename { get; }
    public abstract void WriteJournalComment(string comment, bool timeStamp);
    #endregion

    #region Folders
    public abstract string CurrentUsersDataFolderPath { get; }
    public abstract string CurrentUserAddinsLocation { get; }
    #endregion

    #region Events
    public abstract event EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> ApplicationInitialized;
    public abstract event EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs> DocumentChanged;
    public abstract event EventHandler<Autodesk.Revit.DB.Events.DocumentClosingEventArgs> DocumentClosing;
    #endregion
  }

  class HostServicesC : HostServices
  {
    readonly ControlledApplication _app;
    public HostServicesC(ControlledApplication app) => _app = app;
    public override void Dispose() { }

    public override object Value => _app;

    #region Version
    public override string VersionName => _app.VersionName;
    public override string VersionNumber => _app.VersionNumber;
    public override string VersionBuild => _app.VersionBuild;
    public override string SubVersionNumber => _app.GetSubVersionNumber();

    public override ProductType Product => _app.Product;
    public override LanguageType Language => _app.Language;
    #endregion

    #region Journaling
    public override string RecordingJournalFilename => _app.RecordingJournalFilename;
    public override void WriteJournalComment(string comment, bool timeStamp) => _app.WriteJournalComment(comment, timeStamp);
    #endregion

    #region Folders
    public override string CurrentUsersDataFolderPath =>
#if REVIT_2019
      _app.CurrentUsersDataFolderPath;
#else
      System.IO.Path.Combine
      (
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
        "Autodesk",
        "Revit",
        _app.VersionName
      );
#endif
    public override string CurrentUserAddinsLocation => _app.CurrentUserAddinsLocation;
    #endregion

    #region Events
    public override event EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> ApplicationInitialized
    {
      add    => _app.ApplicationInitialized += ActivationGate.AddEventHandler(value);
      remove => _app.ApplicationInitialized -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs> DocumentChanged
    {
      add    => _app.DocumentChanged += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentChanged -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentClosingEventArgs> DocumentClosing
    {
      add    => _app.DocumentClosing += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentClosing -= ActivationGate.RemoveEventHandler(value);
    }
    #endregion
}

class HostServicesU : HostServices
  {
    readonly Application _app;
    public HostServicesU(Application app) => _app = app;
    public override void Dispose() => _app.Dispose();
    public override object Value => _app;

    #region Version
    public override string VersionName => _app.VersionName;
    public override string VersionNumber => _app.VersionNumber;
    public override string VersionBuild => _app.VersionBuild;
    public override string SubVersionNumber => _app.GetSubVersionNumber();

    public override ProductType Product => _app.Product;
    public override LanguageType Language => _app.Language;
    #endregion

    #region Journaling
    public override string RecordingJournalFilename => _app.RecordingJournalFilename;
    public override void WriteJournalComment(string comment, bool timeStamp) => _app.WriteJournalComment(comment, timeStamp);
    #endregion

    #region Folders
    public override string CurrentUsersDataFolderPath =>
#if REVIT_2019
      _app.CurrentUsersDataFolderPath;
#else
      System.IO.Path.Combine
      (
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
        "Autodesk",
        "Revit",
        _app.VersionName
      );
#endif
    public override string CurrentUserAddinsLocation => _app.CurrentUserAddinsLocation;
    #endregion

    #region Events
    public override event EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> ApplicationInitialized
    {
      add    => _app.ApplicationInitialized += ActivationGate.AddEventHandler(value);
      remove => _app.ApplicationInitialized -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs> DocumentChanged
    {
      add    => _app.DocumentChanged += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentChanged -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentClosingEventArgs> DocumentClosing
    {
      add    => _app.DocumentClosing += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentClosing -= ActivationGate.RemoveEventHandler(value);
    }
    #endregion
  }
  #endregion
}
