using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static RhinoInside.Revit.Diagnostics;

namespace RhinoInside.Revit
{
  static class AssemblyResolver
  {
    static readonly string InstallPath = Core.Distribution.InstallPath;

    #region AssemblyReference

    delegate bool InitAssembly();
    static readonly Dictionary<string, InitAssembly> InternalAssemblies = new Dictionary<string, InitAssembly>()
    {
      { "Eto",          Rhinoceros.InitEto },
      { "RhinoCommon",  Rhinoceros.InitRhinoCommon },
      { "Grasshopper",  Rhinoceros.InitGrasshopper },
    };

    public sealed class AssemblyReference
    {
      internal AssemblyReference(AssemblyName name)
      {
        assemblyName = name;
        Assembly = default;

        activated = default;
      }

      internal readonly AssemblyName assemblyName;
      public AssemblyName Name => assemblyName.Clone() as AssemblyName;
      public Assembly Assembly { get; private set; }

      event AssemblyLoadEventHandler activated;
      public event AssemblyLoadEventHandler Activated
      {
        add
        {
          if (Assembly is object) value?.Invoke(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(Assembly));
          else activated += value;
        }

        remove => activated -= value;
      }

      internal void Activate(Assembly assembly)
      {
        if (!assembly.CodeBase.Equals(assemblyName.CodeBase, StringComparison.OrdinalIgnoreCase))
          return;

        Assembly = assembly;

        bool failed = false;
        if (InternalAssemblies.TryGetValue(assemblyName.Name, out var InitAssembly))
          failed = !InitAssembly();

        if (failed)
          throw new InvalidOperationException($"Failed to activate {assembly.FullName}");

        NotifyActivatedAsync();
      }

      async void NotifyActivatedAsync()
      {
        if (activated is object)
        {
          await System.Threading.Tasks.Task.Yield();

          try { activated(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(Assembly)); }
          finally { activated = default; }
        }
      }

      public override string ToString()
      {
        return $"Activated={Assembly is object}, CodeBase={assemblyName.CodeBase}";
      }
    }

    static readonly Dictionary<string, AssemblyReference> references = new Dictionary<string, AssemblyReference>();
    public static IReadOnlyDictionary<string, AssemblyReference> References => references;
    #endregion

    #region Resolving event
    static readonly FieldInfo _AssemblyResolve = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);

    static Assembly AssemblyResolving(object sender, ResolveEventArgs args)
    {
      if (Resolving?.GetInvocationList() is Delegate[] invocationList)
      {
        foreach (ResolveEventHandler resolver in invocationList)
        {
          try
          {
            var resolved = resolver(sender, args);
            if (resolved is object) return resolved;
          }
          catch { }
        }
      }

      return default;
    }

    public static event ResolveEventHandler Resolving;
    #endregion

    static AssemblyResolver()
    {
      // Setup Resolving event
      {
        var domain = AppDomain.CurrentDomain;
        var assemblyResolve = _AssemblyResolve.GetValue(domain) as ResolveEventHandler;
        var invocationList = assemblyResolve.GetInvocationList();

        foreach (var invocation in invocationList)
          domain.AssemblyResolve -= invocation as ResolveEventHandler;

        domain.AssemblyResolve += AssemblyResolving;

        foreach (var invocation in invocationList)
          domain.AssemblyResolve += invocation as ResolveEventHandler;
      }

      // Search Rhino stuff
      System.Threading.Tasks.Task.Run(() =>
      {
        lock (references)
        {
          try
          {
            var installFolders = new DirectoryInfo[]
            {
              new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)),
              new DirectoryInfo(InstallPath ?? string.Empty)
            };

            foreach (var installFolder in installFolders.Where(x => x.Exists))
            {
              foreach (var dll in installFolder.EnumerateFiles("*.dll", SearchOption.AllDirectories))
              {
                try
                {
                  // https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.8
                  // If the specified extension is exactly three characters long,
                  // the method returns files with extensions that begin with the specified extension.
                  // For example, "*.xls" returns both "book.xls" and "book.xlsx"
                  if (dll.Extension.ToLower() != ".dll") continue;

                  var assemblyName = AssemblyName.GetAssemblyName(dll.FullName);
                  var assemblyReference = new AssemblyReference(assemblyName);

                  if (references.TryGetValue(assemblyName.Name, out var location))
                  {
                    if (location.assemblyName.Version >= assemblyName.Version) continue;
                    references.Remove(assemblyName.Name);
                  }

                  references.Add(assemblyName.Name, assemblyReference);
                }
                catch { }
              }
            }
          }
          catch { }
        }
      });

      Resolving += AssemblyResolve;
    }

    static bool enabled;
    public static bool Enabled
    {
      get => enabled;
      set
      {
        if (enabled != value)
        {
          if (value)
          {
            // Report if opennurbs.dll is loaded
            NativeLoader.SetReportOnLoad("opennurbs.dll", enable: true);

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
            if (_AssemblyResolve is null) AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            else
            {
              if (External.ActivationGate.IsOpen)
                ActivationGate_Enter(default, EventArgs.Empty);

              External.ActivationGate.Enter += ActivationGate_Enter;
              External.ActivationGate.Exit += ActivationGate_Exit;
            }
          }
          else
          {
            if (_AssemblyResolve is null) AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            else
            {
              External.ActivationGate.Exit -= ActivationGate_Exit;
              External.ActivationGate.Enter -= ActivationGate_Enter;

              if (External.ActivationGate.IsOpen)
                ActivationGate_Exit(default, EventArgs.Empty);
            }
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoaded;

            // Disable report opennurbs.dll is loaded 
            NativeLoader.SetReportOnLoad("opennurbs.dll", enable: false);
          }
          enabled = value;
        }
      }
    }

    static Delegate[] InvocationList
    {
      get
      {
        var domain = AppDomain.CurrentDomain;
        var assemblyResolve = _AssemblyResolve.GetValue(domain) as ResolveEventHandler;
        var invocationList = assemblyResolve.GetInvocationList();
        return invocationList;
      }
    }

    static readonly uint InternalThreadId = ThreadHandle.CurrentThreadId;
    static bool InternalAssembliesOnly = true;
    static void ActivationGate_Enter(object sender, EventArgs e) => InternalAssembliesOnly = false;
    static void ActivationGate_Exit(object sender, EventArgs e) => InternalAssembliesOnly = true;

    static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
    {
      var assemblyName = new AssemblyName(args.Name);
      if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        return default;

      var internalAssembliesOnly = InternalAssembliesOnly;
      try
      {
        if (InternalAssembliesOnly && !IsInternalReference(args))
          return default;

        internalAssembliesOnly = false;

        if (Logger.Active)
        {
          using (var scope = Logger.LogScope
          (
            "AppDomain.AssemblyResolve",
            $"Requesting Assembly = {GetRequestingAssembly(args).FullName}",
            $"Requires = '{args.Name}'"
          ))
          {
            var resolved = default(Assembly);
            foreach (ResolveEventHandler resolver in InvocationList)
            {
              var type = MethodInfo.GetCurrentMethod().DeclaringType;
              var info = type.GetMethod(nameof(AssemblyResolving), BindingFlags.NonPublic | BindingFlags.Static);
              if (resolver.Method == info)
              {
                resolved = ResolveAssembly(args.RequestingAssembly, new AssemblyName(args.Name));
              }
              else
              {
                try { resolved = resolver(sender, args); }
                catch (Exception) { }
              }

              if (resolved is object)
              {
                scope.Log
                (
                  resolved.FullName == args.Name ?
                  Logger.Severity.Succeded :
                  Logger.Severity.Information,
                  $"Resolved",
                  $"Resolver = '{resolver.Method.DeclaringType.Assembly}'",
                  $"Got = '{resolved.FullName}'",
                  $"Location = '{resolved.Location}'"
                );
                break;
              }
            }

            if (resolved is null)
              scope.LogWarning("Not Resolved");

            return resolved;
          }
        }
        else
        {
          return ResolveAssembly(args.RequestingAssembly, new AssemblyName(args.Name));
        }
      }
      finally
      {
        InternalAssembliesOnly = internalAssembliesOnly;
      }
    }

    static Assembly ResolveAssembly(Assembly requestingAssembly, AssemblyName requested)
    {
      if (Core.CurrentStatus < Core.Status.Available)
        return default;

      // Resolve this Assembly
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var executingAssemblyName = executingAssembly.GetName();
        if (requested.Name == executingAssemblyName.Name)
          return requested.Version > executingAssemblyName.Version ? default : executingAssembly;
      }

      // AppDomain.AssemblyResolve may be called from any thread.
      lock (references)
      {
        // Look up if Rhino deplois something for us…
        if (!references.TryGetValue(requested.Name, out var location))
        {
          // Probe with loaded Assemblies if full name coincides.
          var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
          foreach (var assembly in domainAssemblies)
          {
            if (assembly.FullName == requested.FullName)
              return assembly;
          }

          return default;
        }

        // Never return an older assembly.
        if (location.assemblyName.Version < requested.Version)
          return default;

        if (location.Assembly is null)
        {
          // Never load an Internal assembly from an other thread than the UI thread.
          if (ThreadHandle.CurrentThreadId != InternalThreadId && InternalAssemblies.ContainsKey(location.assemblyName.Name))
            return default;

          // Remove it to not try again if it fails and avoid recursion.
          references.Remove(requested.Name);

          if (!AssemblyCanLoad(requested))
            return default;

          // Load Assembly
          //var assembly =  Assembly.Load(location.assemblyName);
          var assembly = Assembly.LoadFrom(location.assemblyName.CodeBase);
          //var assembly = Assembly.LoadFile(new Uri(location.assemblyName.CodeBase).LocalPath);

          Debug.Assert
          (
            assembly.CodeBase.ToLowerInvariant() == location.assemblyName.CodeBase.ToLowerInvariant(),
            $"Expected = {location.assemblyName.CodeBase}" + Environment.NewLine +
            $"Loaded = {assembly.CodeBase}"
          );

          // Add again loaded assembly
          references.Add(requested.Name, location);

          try { location.Activate(assembly); }
          catch { }
        }

        return location.Assembly;
      }
    }

    static bool AssemblyCanLoad(AssemblyName assemblyName)
    {
      if (assemblyName.Name == "RhinoCommon")
      {
        if (NativeLoader.GetReportOnLoad("opennurbs.dll"))
        {
          // Disable report opennurbs.dll is loaded 
          NativeLoader.SetReportOnLoad("opennurbs.dll", enable: false);

          // Check if 'opennurbs.dll' is already loaded
          var openNURBS = LibraryHandle.GetLoadedModule("opennurbs.dll");
          if (openNURBS != LibraryHandle.Zero)
          {
            var openNURBSVersion = FileVersionInfo.GetVersionInfo(openNURBS.ModuleFileName);

            using
            (
              var taskDialog = new TaskDialog($"Rhino.Inside {Core.Version} - openNURBS Conflict")
              {
                Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.OpenNURBSConflict",
                MainIcon = External.UI.TaskDialogIcons.IconError,
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
              switch (taskDialog.Show())
              {
                case TaskDialogResult.CommandLink1:
                  Browser.Start($@"{Core.WebSite}reference/known-issues");
                  break;
                case TaskDialogResult.CommandLink2:

                  var RhinoInside_dmp = Path.Combine
                  (
                    Path.GetDirectoryName(Core.Host.Services.RecordingJournalFilename),
                    Path.GetFileNameWithoutExtension(Core.Host.Services.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
                  );

                  MiniDumper.Write(RhinoInside_dmp);

                  ErrorReport.SendEmail
                  (
                    app: Core.Host,
                    subject: $"{Core.Product}.{Core.Platform} - openNURBS Conflict",
                    body: null,
                    includeAddinsList: true,
                    attachments: new string[]
                    {
                      Core.Host.Services.RecordingJournalFilename,
                      RhinoInside_dmp
                    }
                  );

                  Core.CurrentStatus = Core.Status.Failed;
                  break;
              }
            }

            return false;
          }
        }
      }

      return true;
    }

    static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
    {
      if (args.LoadedAssembly.ReflectionOnly) return;

      var assemblyName = args.LoadedAssembly.GetName();
      if (references.TryGetValue(assemblyName.Name, out var location))
        location.Activate(args.LoadedAssembly);
    }

    #region Utils
    static bool IsInternalReference(ResolveEventArgs args)
    {
      var assembly = new AssemblyName(args.Name);
      if
      (
        InternalAssemblies.ContainsKey(assembly.Name) &&
        GetRequestingAssembly(args) is Assembly requestingAssembly
      )
        return requestingAssembly.GetReferencedAssemblies().Any(x => InternalAssemblies.ContainsKey(x.Name));

      return false;
    }

    static Assembly GetRequestingAssembly(ResolveEventArgs args)
    {
      var requestingAssembly = args.RequestingAssembly;
      if (requestingAssembly is null)
      {
        var trace = new StackTrace(1);
        var frames = trace.GetFrames();

        var callingAssembly = Assembly.GetCallingAssembly();

        // Skip Calling Assembly
        int f = 0;
        for (; f < frames.Length; ++f)
        {
          var frameAssembly = frames[f].GetMethod().DeclaringType.Assembly;
          if (frameAssembly != callingAssembly)
            break;
        }

        // Skip mscorlib
        for (; f < frames.Length; ++f)
        {
          var frameAssembly = frames[f].GetMethod().DeclaringType.Assembly;
          if (frameAssembly != typeof(object).Assembly)
          {
            requestingAssembly = frameAssembly;
            break;
          }
        }
      }

      return requestingAssembly;
    }
    #endregion
  }
}
