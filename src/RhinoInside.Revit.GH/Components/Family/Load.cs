using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyLoad : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("0E244846-95AE-4B0E-8218-CB24FD4D34D1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "L";

    public FamilyLoad() : base
    (
      name: "Load Component Family",
      nickname: "Load",
      description: "Loads a family into the document",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_FilePath()
        {
          Name = "Path",
          NickName = "P",
          FileFilter = "Family File (*.rfa)|*.rfa"
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Override Family",
          NickName = "OF",
          Description = "Override Family",
        }.
        SetDefaultVale(false)
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Override Parameters",
          NickName = "OP",
          Description = "Override Parameters",
        }.
        SetDefaultVale(false)
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Family()
        {
          Name = "Family",
          NickName = "F",
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var filePath = string.Empty;
      if (!DA.GetData("Path", ref filePath))
        return;

      var overrideFamily = false;
      if (!DA.GetData("Override Family", ref overrideFamily))
        return;

      var overrideParameters = false;
      if (!DA.GetData("Override Parameters", ref overrideParameters))
        return;

      using (var transaction = NewTransaction(doc))
      {
        transaction.Start(Name);

        if (doc.LoadFamily(filePath, new FamilyLoadOptions(overrideFamily, overrideParameters), out var family))
        {
          CommitTransaction(doc, transaction);
        }
        else
        {
          var name = Path.GetFileNameWithoutExtension(filePath);
          doc.TryGetFamily(name, out family);

          if (family is object && overrideFamily == false)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Family '{name}' already loaded!");
        }

        DA.SetData("Family", family);
      }
    }
  }
}
