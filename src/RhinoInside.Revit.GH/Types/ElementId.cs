using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_ElementId : IGH_Goo
  {
    DB.Reference Reference { get; }
    DB.Document Document { get; }
    DB.ElementId Id { get; }

    Guid DocumentGUID { get; }
    string UniqueID { get; }

    bool IsReferencedElement { get; }
    bool IsElementLoaded { get; }
    bool LoadElement();
    void UnloadElement();
  }

  public abstract class ElementId : IGH_ElementId, IEquatable<ElementId>
  {
    #region System.Object
    public bool Equals(ElementId id) => id?.DocumentGUID == DocumentGUID && id?.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is ElementId id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => DocumentGUID.GetHashCode() ^ UniqueID.GetHashCode();

    public override sealed string ToString()
    {
      var TypeName = $"Revit {((IGH_Goo) this).TypeName}";

      if (!IsReferencedElement)
        return $"{TypeName} : {DisplayName}";

      var tip = IsValid ?
      (
        IsElementLoaded ?
        $"{TypeName} : {DisplayName}" :
        $"Unresolved {TypeName} : {UniqueID}"
      ) :
      $"Invalid {TypeName}";

      using (var Documents = Revit.ActiveDBApplication.Documents)
      {
        return
        (
          Documents.Size > 1 ?
          $"{tip} @ {Document?.Title ?? DocumentGUID.ToString()}" :
          tip
        );
      }
    }
    #endregion

    #region GH_ISerializable
    public virtual bool Read(GH_IReader reader)
    {
      Id = DB.ElementId.InvalidElementId;
      Document = null;

      var documentGUID = Guid.Empty;
      reader.TryGetGuid("DocumentGUID", ref documentGUID);
      DocumentGUID = documentGUID;

      string uniqueID = string.Empty;
      reader.TryGetString("UniqueID", ref uniqueID);
      UniqueID = uniqueID;

      return true;
    }

    public virtual bool Write(GH_IWriter writer)
    {
      if (DocumentGUID != Guid.Empty)
        writer.SetGuid("DocumentGUID", DocumentGUID);

      if (!string.IsNullOrEmpty(UniqueID))
        writer.SetString("UniqueID", UniqueID);

      return true;
    }
    #endregion

    #region IGH_Goo
    string IGH_Goo.TypeName
    {
      get
      {
        var type = GetType();
        var name = type.GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as Kernel.Attributes.NameAttribute;
        return name?.Name ?? type.Name;
      }
    }

    string IGH_Goo.TypeDescription => $"Represents a Revit {((IGH_Goo) this).TypeName.ToLowerInvariant()}";
    public virtual bool IsValid => Document.IsValid() && (Id.IsBuiltInId() || Value is object);
    public virtual string IsValidWhyNot => IsValid ? string.Empty : "Not Valid";
    IGH_Goo IGH_Goo.Duplicate() => (IGH_Goo) MemberwiseClone();
    public virtual object ScriptVariable() => Id;
    protected virtual Type ScriptVariableType => typeof(DB.ElementId);
    public static explicit operator DB.ElementId(ElementId self) { return self.Id; }

    public virtual bool CastFrom(object source)
    {
      if (source is GH_Integer integer)
      {
        Id = new DB.ElementId(integer.Value);
        UniqueID = string.Empty;
        return true;
      }
      if (source is DB.ElementId id)
      {
        Id = id;
        UniqueID = string.Empty;
        return true;
      }

      return false;
    }

    public virtual bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.ElementId)))
      {
        target = (Q) (object) Id;
        return true;
      }
      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(Id.IntegerValue);
        return true;
      }
      if (typeof(Q).IsAssignableFrom(typeof(GH_String)))
      {
        target = (Q) (object) new GH_String(UniqueID);
        return true;
      }

      target = default;
      return false;
    }

    [TypeConverter(typeof(Proxy.ObjectConverter))]
    protected class Proxy : IGH_GooProxy
    {
      protected readonly ElementId owner;
      public Proxy(ElementId o) { owner = o; ((IGH_GooProxy) this).UserString = FormatInstance(); }
      public override string ToString() => owner.DisplayName;

      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      string IGH_GooProxy.UserString { get; set; }
      bool IGH_GooProxy.IsParsable => IsParsable();
      string IGH_GooProxy.MutateString(string str) => str.Trim();

      public virtual void Construct() { }
      public virtual bool IsParsable() => false;
      public virtual string FormatInstance() => owner.DisplayName;
      public virtual bool FromString(string str) => throw new NotImplementedException();

      public bool Valid => owner.IsValid;

      [System.ComponentModel.Description("The document this element belongs to.")]
      public string Document => owner.Document.GetFilePath();
      [System.ComponentModel.Description("The Guid of document this element belongs to.")]
      public Guid DocumentGUID => owner.DocumentGUID;
      [System.ComponentModel.Description("The element identifier in this session.")]
      [System.ComponentModel.RefreshProperties(RefreshProperties.All)]
      public virtual int? Id
      {
        get => owner.Id?.IntegerValue;
        set
        {
          if (!value.HasValue || value == DB.ElementId.InvalidElementId.IntegerValue)
            owner.SetValue(default, DB.ElementId.InvalidElementId);
          else
          {
            var doc = owner.Document ?? Revit.ActiveDBDocument;
            var id = new DB.ElementId(value.Value);

            if(IsValidId(doc, id)) owner.SetValue(doc, id);
          }
        }
      }
      protected virtual bool IsValidId(DB.Document doc, DB.ElementId id) => true;

      [System.ComponentModel.Description("A stable unique identifier for an element within the document.")]
      public string UniqueID => owner.UniqueID;
      [System.ComponentModel.Description("API Object Type.")]
      public virtual Type ObjectType => owner.Value?.GetType();
      [System.ComponentModel.Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.Id.IsBuiltInId();

      class ObjectConverter : ExpandableObjectConverter
      {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
          var properties = base.GetProperties(context, value, attributes);
          if (value is Proxy proxy && proxy.Valid)
          {
            var element = proxy.owner.Document?.GetElement(proxy.owner.Id);
            if (element is object)
            {
              var parameters = element.GetParameters(DBX.ParameterClass.Any).
                Select(p => new ParameterPropertyDescriptor(p)).
                ToArray();

              var descriptors = new PropertyDescriptor[properties.Count + parameters.Length];
              properties.CopyTo(descriptors, 0);
              parameters.CopyTo(descriptors, properties.Count);

              return new PropertyDescriptorCollection(descriptors, true);
            }
          }

          return properties;
        }
      }

      private class ParameterPropertyDescriptor : PropertyDescriptor
      {
        readonly DB.Parameter parameter;
        public ParameterPropertyDescriptor(DB.Parameter p) : base(p.Definition?.Name ?? p.Id.IntegerValue.ToString(), null) { parameter = p; }
        public override Type ComponentType => typeof(Proxy);
        public override bool IsReadOnly => true;
        public override string Name => parameter.Definition?.Name ?? string.Empty;
        public override string Category => parameter.Definition is null ? string.Empty : DB.LabelUtils.GetLabelFor(parameter.Definition.ParameterGroup);
        public override string Description
        {
          get
          {
            var description = string.Empty;
            if (parameter.Element is object && parameter.Definition is object)
            {
              try { description = parameter.StorageType == DB.StorageType.ElementId ? "ElementId" : DB.LabelUtils.GetLabelFor(parameter.Definition.ParameterType); }
              catch (Autodesk.Revit.Exceptions.InvalidOperationException)
              { description = parameter.Definition.UnitType == DB.UnitType.UT_Number ? "Enumerate" : DB.LabelUtils.GetLabelFor(parameter.Definition.UnitType); }
            }

            if (parameter.IsReadOnly)
              description = "Read only " + description;

            description += "\nParameterId : " + ((DB.BuiltInParameter)parameter.Id.IntegerValue).ToStringGeneric();
            return description;
          }
        }
        public override bool Equals(object obj)
        {
          if (obj is ParameterPropertyDescriptor other)
            return other.parameter.Id == parameter.Id;

          return false;
        }
        public override int GetHashCode() => parameter.Id.IntegerValue;
        public override bool ShouldSerializeValue(object component) { return false; }
        public override void ResetValue(object component) { }
        public override bool CanResetValue(object component) { return false; }
        public override void SetValue(object component, object value) { }
        public override Type PropertyType => typeof(string);
        public override object GetValue(object component) =>
          parameter.Element is object && parameter.Definition is object ?
          (parameter.StorageType == DB.StorageType.String ? parameter.AsString() :
          parameter.AsValueString()) : null;
      }
    }

    public virtual IGH_GooProxy EmitProxy() => new Proxy(this);

    string IGH_Goo.ToString() => DisplayName;
    #endregion

    #region IGH_ElementId
    DB.Element value = default;
    public object Value
    {
      get
      {
        if (value?.IsValidObject == false)
          ResetValue();

        if (value is null)
        {
          if (IsElementLoaded)
            value = document.GetElement(id);
        }

        return value;
      }
    }

    protected internal void SetValue(DB.Document doc, DB.ElementId id)
    {
      if (id == DB.ElementId.InvalidElementId)
        doc = null;

      Document = doc;
      DocumentGUID = doc.GetFingerprintGUID();

      Id = id;
      UniqueID = doc?.GetElement(id)?.UniqueId ??
                  (
                    id.IntegerValue < DB.ElementId.InvalidElementId.IntegerValue ?
                      DBX.UniqueId.Format(Guid.Empty, id.IntegerValue) :
                      string.Empty
                  );
    }

    protected virtual void ResetValue()
    {
      value = default;
    }

    public DB.Reference Reference
    {
      get
      {
        try { return DB.Reference.ParseFromStableRepresentation(Document, UniqueID); }
        catch (Autodesk.Revit.Exceptions.ArgumentNullException) { return null; }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { return null; }
      }
    }

    DB.Document document = default;
    public DB.Document Document
    {
      get => document?.IsValidObject != true ? null : document;
      protected set { document = value; ResetValue(); }
    }

    DB.ElementId id = DB.ElementId.InvalidElementId;
    public DB.ElementId Id
    {
      get => id;
      protected set { id = value; ResetValue(); }
    }

    public Guid DocumentGUID { get; protected set; } = Guid.Empty;
    public string UniqueID { get; protected set; } = string.Empty;
    public bool IsReferencedElement => DocumentGUID != Guid.Empty;
    public bool IsElementLoaded => Document is object && Id is object;

    public virtual bool LoadElement()
    {
      if (IsReferencedElement && !IsElementLoaded)
      {
        Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc);
        Document = doc;

        Document.TryGetElementId(UniqueID, out var id);
        Id = id;
      }

      return IsElementLoaded;
    }

    public void UnloadElement()
    {
      ResetValue();

      if (IsReferencedElement)
      {
        Document = default;
        Id = default;
      }
    }
    #endregion

    public ElementId() { }

    protected ElementId(DB.Document doc, DB.ElementId id) => SetValue(doc, id);

    protected ElementId(DB.Element element)
    {
      SetValue(element?.Document, element?.Id ?? DB.ElementId.InvalidElementId);
      value = element;
    }

    #region Properties
    public virtual string DisplayName => IsReferencedElement ?
      Id is null ? "INVALID" : Id.IntegerValue.ToString() :
      "<None>";
    #endregion
  }
}
