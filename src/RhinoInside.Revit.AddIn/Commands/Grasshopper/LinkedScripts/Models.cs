using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Interop;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Types of scripts that can be executed
  /// </summary>
  public enum ScriptType
  {
    GhFile = 0,
    GhxFile,
  }

  /// <summary>
  /// Generic linked item
  /// </summary>
  public abstract class LinkedItem
  {
    public string Text
    {
      get
      {
        string displayName = string.Empty;
        var nameLines = Name.Split('-');
        for (int l = 0; l < nameLines.Length; ++l)
        {
          var line = nameLines[l].
            Trim(' ').                    // Remove trailing spaces 
            Replace(' ', (char) 0x00A0);  // Replace spaces by non-breaking-spaces
          if (line == string.Empty) continue;

          displayName += line.TripleDot(12);
          if (l < nameLines.Length - 1)
            displayName += Environment.NewLine;
        }

        return displayName;
      }
    }

    public string Name { get; set; } = string.Empty;
    public string Tooltip { get; set; } = string.Empty;
  }

  /// <summary>
  /// Group of linked items
  /// </summary>
  public class LinkedItemGroup : LinkedItem
  {
    public string GroupPath { get; set; }
    public List<LinkedItem> Items { get; set; } = new List<LinkedItem>();
  }

  /// <summary>
  /// Linked script
  /// </summary>
  public class LinkedScript : LinkedItem
  {
    public ScriptType ScriptType { get; set; }
    public string ScriptPath { get; set; }
    public Type ScriptCommandType { get; set; }

    public string Description { get; set; } = null;

    static readonly string[] SupportedExtensions = new string[] { ".gh", ".ghx" };
    public static LinkedScript FromPath(string scriptPath)
    {
      var ext = Path.GetExtension(scriptPath).ToLower();
      if (SupportedExtensions.Contains(ext) && File.Exists(scriptPath))
      {
        var archive = new GH_IO.Serialization.GH_Archive();
        if (archive.ReadFromFile(scriptPath))
        {
          var description = default(string);
          var iconImageData = default(string);

          var definitionProperties = archive.GetRootNode.
            FindChunk("Definition")?.
            FindChunk("DefinitionProperties");

          if (definitionProperties is object)
          {
            definitionProperties.TryGetString("Description", ref description);
            definitionProperties.TryGetString("IconImageData", ref iconImageData);
          }

          return new LinkedScript
          {
            ScriptType = ext == ".gh" ? ScriptType.GhFile : ScriptType.GhxFile,
            ScriptPath = scriptPath,
            Name = Path.GetFileNameWithoutExtension(scriptPath),
            Description = description,
            IconImageData = iconImageData,
          };
        }
      }

      return default;
    }

    string IconImageData = null;
    public ImageSource GetScriptIcon(bool small)
    {
      if (!string.IsNullOrEmpty(IconImageData))
      {
        int width = small ? 16 : 32;
        int height = small ? 16 : 32;

        try
        {
          // if SVG
          if (IconImageData.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) > 0)
          {
            using (var bitmap = Rhino.UI.DrawingUtilities.BitmapFromSvg(IconImageData, width * 4, height * 4))
              return bitmap.ToBitmapImage(width, height);
          }
          // else assume bitmap
          else
          {
            using (var bitmap = new Bitmap(new MemoryStream(System.Convert.FromBase64String(IconImageData))))
              return bitmap.ToBitmapImage(width, height);
          }
        }
        catch { }
      }

      return default;
    }
  }

  /// <summary>
  /// Package of Scripts for this addin
  /// </summary>
  public class ScriptPkg
  {
    public string Name;
    public string Location;
    public List<LinkedItem> FindLinkedItems() => FindLinkedItemsRecursive(Location);

    public static bool operator ==(ScriptPkg lp, ScriptPkg rp) => lp.Location.Equals(rp.Location, StringComparison.InvariantCultureIgnoreCase);
    public static bool operator !=(ScriptPkg lp, ScriptPkg rp) => !lp.Location.Equals(rp.Location, StringComparison.InvariantCultureIgnoreCase);
    public override bool Equals(object obj) {
      if (obj is ScriptPkg pkg)
        return this == pkg;
      return false;
    }
    public override int GetHashCode() => Location.GetHashCode();

    /// <summary>
    /// Find all user script packages
    /// </summary>
    /// <returns></returns>
    public static List<ScriptPkg> GetUserScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      foreach (var location in Properties.AddInOptions.Current.ScriptLocations)
        if (Directory.Exists(location))
          pkgs.Add(
            new ScriptPkg { Name = Path.GetFileName(location), Location = location }
            );
      return pkgs;
    }

    /// <summary>
    /// Find user script package by name
    /// </summary>
    /// <returns></returns>
    public static ScriptPkg GetUserScriptPackageByName(string name)
    {
      foreach (var pkg in GetUserScriptPackages())
        if (pkg.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
          return pkg;
      return null;
    }

    /// <summary>
    /// Find user script package by location
    /// </summary>
    /// <returns></returns>
    public static ScriptPkg GetUserScriptPackageByLocation(string location)
    {
      foreach (var pkg in GetUserScriptPackages())
        if (pkg.Location.Equals(location, StringComparison.InvariantCultureIgnoreCase))
          return pkg;
      return null;
    }

    private static List<LinkedItem> FindLinkedItemsRecursive(string location)
    {
      var items = new List<LinkedItem>();

      foreach (var subDir in Directory.GetDirectories(location))
      {
        // only go one level deep
        items.Add(
          new LinkedItemGroup
          {
            GroupPath = subDir,
            Name = Path.GetFileName(subDir),
            Items = FindLinkedItemsRecursive(subDir),
          }
        );
      }

      foreach (var entry in Directory.GetFiles(location))
        if (LinkedScript.FromPath(entry) is LinkedScript script)
          items.Add(script);

      return items.OrderBy(x => x.Name).ToList();
    }
  }
}
