using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIApplicationExtension
  {
    internal static bool CatchException(this UIApplication app, Exception e, object sender)
    {
      var addinId = app.ActiveAddInId?.GetGUID() ?? Guid.Empty;
      if (addinId != Guid.Empty)
      {
        if (app.LoadedApplications.OfType<UI.ExternalApplication>().Where(x => x.GetGUID() == addinId).FirstOrDefault() is UI.ExternalApplication addin)
          return addin.CatchException(e, app, sender);
      }

      return false;
    }

    internal static void ReportException(this UIApplication app, Exception e, object sender)
    {
      var comment = $@"Managed exception caught from external API application '{e.Source}' in method '{e.TargetSite}' Exception type: '<{e.GetType().FullName}>,' Exception method: '<{e.Message}>,' Stack trace '   {e.StackTrace}";
      comment = comment.Replace(Environment.NewLine, $"{Environment.NewLine}'");
      app.Application.WriteJournalComment(comment, true);

      foreach (var hWnd in ActivationGate.GateWindows)
      {
        using (var window = new WindowHandle(hWnd))
        {
          window.HideOwnedPopups();
          window.Hide();
        }
      }

      var addinId = app.ActiveAddInId?.GetGUID() ?? Guid.Empty;
      if (addinId != Guid.Empty)
      {
        if (app.LoadedApplications.OfType<UI.ExternalApplication>().Where(x => x.GetGUID() == addinId).FirstOrDefault() is UI.ExternalApplication addin)
          addin.ReportException(e, app, sender);
      }
    }

    internal static IList<UIDocument> GetOpenUIDocuments(this UIApplication app)
    {
      return Rhinoceros.InvokeInHostContext
      (
        () =>
        app.Application.Documents.Cast<Document>().
        Where(x => !x.IsLinked).
        Select(x => new UIDocument(x)).
        Where(x => x.GetOpenUIViews().Count > 0).
        ToArray()
      );
    }

    public static bool TryGetDocument(this UIApplication app, Guid guid, out Document document) =>
      app.Application.Documents.Cast<Document>().TryGetDocument(guid, out document, app.ActiveUIDocument?.Document);

    /// <summary>
    /// Get Revit screen that includes center of Revit window.
    /// </summary>
    public static Screen GetMainScreen(this UIApplication uiapp)
    {
      // find the screen that contains the center of Revit window
      var rect = uiapp.MainWindowExtents;
      return Screen.FromPoint
      (
        new System.Drawing.Point
        (
          Math.Abs(rect.Right - rect.Left) / 2 + rect.Left,
          Math.Abs(rect.Bottom - rect.Top) / 2 + rect.Top
        )
      );
    }

    /// <summary>
    /// Center given rectangle on main window and return new rectangle
    /// </summary>
    /// <param name="width">Width of rectangle to be centered</param>
    /// <param name="height">Height of rectangle to be centered</param>
    /// <returns></returns>
    public static System.Drawing.Rectangle CenterRectangleOnExtents(this UIApplication uiApp, int width, int height)
    {
      var revitHeight = Math.Abs(uiApp.MainWindowExtents.Bottom - uiApp.MainWindowExtents.Top);
      var revitWidth = Math.Abs(uiApp.MainWindowExtents.Right - uiApp.MainWindowExtents.Left);
      return new System.Drawing.Rectangle(
          x: Math.Abs(revitWidth - width) / 2 + uiApp.MainWindowExtents.Left,
          y: Math.Abs(revitHeight - height) / 2 + uiApp.MainWindowExtents.Top,
          width: width,
          height: height
        );
    }
  }
}
