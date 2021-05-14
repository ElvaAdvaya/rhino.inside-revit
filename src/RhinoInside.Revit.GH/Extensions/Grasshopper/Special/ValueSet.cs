using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.HTML;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

///////////////////////////////////////////////////////////////////////////////
//                                                                           //
// NOTE: At some point this code may end up in the Grasshopper code base.    //
//                                                                           //
///////////////////////////////////////////////////////////////////////////////

namespace Grasshopper.Special
{
  /// <summary>
  /// <see cref="IEqualityComparer{T}"/> implementation for <see cref="IGH_Goo"/> references.
  /// </summary>
  /// <remarks>
  /// Support most of the Grasshopper built-in types, but some types are not comparable, see code below.
  /// </remarks>
  struct GooEqualityComparer : IEqualityComparer<IGH_Goo>
  {
    static bool IsEquatable(Type value) => value?.GetInterfaces().Any
    (
      i =>
      i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>)
    ) == true;

    public static bool IsEquatable(IGH_Goo goo)
    {
      return IsEquatable(goo?.GetType()) ||
      goo is IGH_GeometricGoo ||
      goo is IGH_QuickCast ||
      goo is GH_StructurePath ||
      goo is GH_Culture ||
      goo is IComparable ||
      (
        goo?.ScriptVariable() is object obj &&
        (
          IsEquatable(obj.GetType()) ||
          obj is ValueType ||
          obj is IComparable
        )
      );
    }

    public bool Equals(IGH_Goo x, IGH_Goo y)
    {
      if (ReferenceEquals(x, y)) return true;
      if (x is null) return false;
      if (y is null) return false;

      // Compare at Goo level
      if (x.GetType() is Type typeX && y.GetType() is Type typeY && typeX == typeY)
      {
        if (IsEquatable(typeX))
        {
          dynamic dynX = x, dynY = y;
          return dynX.Equals(dynY);
        }

        if (x is IGH_QuickCast qcX && y is IGH_QuickCast qcY)
          return qcX.QC_CompareTo(qcY) == 0;

        if (x is IGH_GeometricGoo geoX && y is IGH_GeometricGoo geoY)
        {
          if (geoX.IsReferencedGeometry || geoY.IsReferencedGeometry)
            return geoX.ReferenceID == geoY.ReferenceID;

          if (geoX.ScriptVariable() is Rhino.Geometry.GeometryBase geometryX && geoY.ScriptVariable() is Rhino.Geometry.GeometryBase geometryY)
            return Rhino.Geometry.GeometryBase.GeometryEquals(geometryX, geometryY);
        }

        if (x is GH_StructurePath pathX && y is GH_StructurePath pathY)
          return pathX.Value == pathY.Value;

        if (x is GH_Culture cultureX && y is GH_Culture cultureY)
          return cultureX.Value == cultureY.Value;

        if (x is IComparable cX && y is IComparable cY)
          return cX.CompareTo(cY) == 0;

        // Compare at ScriptVariable level
        if (x.ScriptVariable() is object objX && y.ScriptVariable() is object objY)
          return ScriptVariableEquals(objX, objY);
      }

      return false;
    }

    static bool ScriptVariableEquals(object x, object y)
    {
      if (x.GetType() is Type typeX && y.GetType() is Type typeY && typeX == typeY)
      {
        if (IsEquatable(typeX))
        {
          dynamic dynX = x, dynY = y;
          return dynX.Equals(dynY);
        }

        if (x is IComparable comparableX && y is IComparable comparableY)
          return comparableX.CompareTo(comparableY) == 0;

        if (x is ValueType valueX && y is ValueType valueY)
          return valueX.Equals(valueY);
      }

      return false;
    }

    public int GetHashCode(IGH_Goo obj)
    {
      if (obj is null)
        return 0;

      if (IsEquatable(obj.GetType()))
        return obj.GetHashCode();

      if (obj is IGH_GeometricGoo geo && geo.IsReferencedGeometry)
        return geo.ReferenceID.GetHashCode();

      if (obj is IGH_QuickCast qc)
        return qc.QC_Hash();

      if (obj is GH_StructurePath path)
        return path.Value.GetHashCode();

      if (obj is GH_Culture culture)
        return culture.Value.LCID;

      if (obj is IComparable comparableGoo)
        return comparableGoo.GetHashCode();

      if (obj.ScriptVariable() is object o)
      {
        if (o is IComparable comparable)
          return comparable.GetHashCode();

        if (IsEquatable(o.GetType()))
          return o.GetHashCode();

        if (o is ValueType value)
          return value.GetHashCode();
      }

      return 0;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Never)]
  public abstract class ValueSet : GH_PersistentParam<IGH_Goo>, IGH_InitCodeAware, IGH_StateAwareObject
  {
    public override string TypeName => "Data";
    public override GH_ParamKind Kind => GH_ParamKind.floating;

    protected override Bitmap Icon => ClassIcon;
    static readonly Bitmap ClassIcon = BuildIcon();
    static Bitmap BuildIcon()
    {
      var bitmap = new Bitmap(24, 24);
      using (var graphics = Graphics.FromImage(bitmap))
      {
        var iconBounds = new RectangleF(0.0f, 0.0f, 24.0f, 24.0f);
        iconBounds.Inflate(-0.5f, -0.5f);

        using (var capsule = GH_Capsule.CreateCapsule(iconBounds, GH_Palette.Grey))
        {
          capsule.Render(graphics, false, false, false);
          ComponentAttributes.RenderCheckMark(graphics, iconBounds, Color.Black);
        }
      }

      return bitmap;
    }

    void IGH_InitCodeAware.SetInitCode(string code) => NickName = code;
    protected override IGH_Goo InstantiateT() => new GH_ObjectWrapper();

    protected ValueSet(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    {
      ObjectChanged += OnObjectChanged;

      // This makes the parameter not turn orange when there is nothing selected.
      Optional = true;
    }

    [Flags]
    public enum DataCulling
    {
      None = 0,
      Nulls = 1 << 0,
      Invalids = 1 << 1,
      Duplicates = 1 << 2,
      Empty = 1 << 31
    };

    /// <summary>
    /// Culling nulls by default to make it work as a <see cref="CheckBox"/>
    /// </summary>
    const DataCulling DefaultCulling = DataCulling.Nulls;
    DataCulling culling = DefaultCulling;
    public DataCulling Culling
    {
      get => culling & CullingMask;
      set => culling = value;
    }

    public virtual DataCulling CullingMask => DataCulling.Nulls | DataCulling.Invalids | DataCulling.Duplicates | DataCulling.Empty;

    public class ListItem
    {
      public ListItem(IGH_Goo goo, bool selected = false)
      {
        Value = goo;
        Name = goo.ToString();
        Selected = selected;
      }

      public readonly IGH_Goo Value;
      public readonly string Name;
      public bool Selected;
      public RectangleF BoxName;
    }

    public List<ListItem> ListItems = new List<ListItem>();
    public IEnumerable<ListItem> SelectedItems => ListItems.Where(x => x.Selected);

    private void OnObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
    {
      if (e.Type == GH_ObjectEventType.NickName)
        ExpireSolution(true);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendWireDisplay(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendPreProcessParameter(menu);
      Menu_AppendPrincipalParameter(menu);
      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);
      Menu_AppendPostProcessParameter(menu);

      if (Kind == GH_ParamKind.floating || Kind == GH_ParamKind.input)
      {
        Menu_AppendSeparator(menu);
        if (Menu_CustomSingleValueItem() is ToolStripMenuItem single)
        {
          single.Enabled &= SourceCount == 0;
          menu.Items.Add(single);
        }
        else Menu_AppendPromptOne(menu);

        if (Menu_CustomMultiValueItem() is ToolStripMenuItem more)
        {
          more.Enabled &= SourceCount == 0;
          menu.Items.Add(more);
        }
        else Menu_AppendPromptMore(menu);
        Menu_AppendManageCollection(menu);

        Menu_AppendSeparator(menu);
        Menu_AppendDestroyPersistent(menu);
        Menu_AppendInternaliseData(menu);

        if (Exposure != GH_Exposure.hidden)
          Menu_AppendExtractParameter(menu);
      }
    }

    protected virtual void Menu_AppendPreProcessParameter(ToolStripDropDown menu) { }

    private void Menu_Culling(DataCulling value)
    {
      RecordUndoEvent("Set: Culling");

      if (Culling.HasFlag(value))
        Culling &= ~value;
      else
        Culling |= value;

      OnObjectChanged(GH_ObjectEventType.Options);

      if (Kind == GH_ParamKind.output)
        ExpireOwner();

      ExpireSolution(true);
    }

    protected virtual void Menu_AppendPostProcessParameter(ToolStripDropDown menu)
    {
      var Cull = Menu_AppendItem(menu, "Cull");

      Cull.Checked = Culling != DataCulling.None;
      if (CullingMask.HasFlag(DataCulling.Nulls))
        Menu_AppendItem(Cull.DropDown, "Nulls", (s, a) => Menu_Culling(DataCulling.Nulls), true, Culling.HasFlag(DataCulling.Nulls));

      if (CullingMask.HasFlag(DataCulling.Invalids))
        Menu_AppendItem(Cull.DropDown, "Invalids", (s, a) => Menu_Culling(DataCulling.Invalids), true, Culling.HasFlag(DataCulling.Invalids));

      if (CullingMask.HasFlag(DataCulling.Duplicates))
        Menu_AppendItem(Cull.DropDown, "Duplicates", (s, a) => Menu_Culling(DataCulling.Duplicates), true, Culling.HasFlag(DataCulling.Duplicates));

      if (CullingMask.HasFlag(DataCulling.Empty))
        Menu_AppendItem(Cull.DropDown, "Empty", (s, a) => Menu_Culling(DataCulling.Empty), true, Culling.HasFlag(DataCulling.Empty));
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }

    protected override GH_GetterResult Prompt_Plural(ref List<IGH_Goo> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref IGH_Goo value) => GH_GetterResult.cancel;

    protected override void Menu_AppendDestroyPersistent(ToolStripDropDown menu) =>
      Menu_AppendItem(menu, "Clear selection", Menu_DestroyPersistentData, PersistentDataCount > 0);

    private void Menu_DestroyPersistentData(object sender, EventArgs e)
    {
      if (PersistentDataCount == 0) return;

      foreach (var item in ListItems)
        item.Selected = false;

      ResetPersistentData(null, "Clear selection");
    }

    protected override void Menu_AppendInternaliseData(ToolStripDropDown menu)
    {
      Menu_AppendItem(menu, "Internalise selection", Menu_InternaliseDataClicked, SourceCount > 0);
      Menu_AppendItem(menu, "Invert selection", Menu_InvertSelectionClicked, SourceCount > 0);
      Menu_AppendItem(menu, "Select all", Menu_SelectAllClicked, SourceCount > 0);
    }

    private void Menu_InternaliseDataClicked(object sender, EventArgs e)
    {
      if (SourceCount == 0) return;

      RecordUndoEvent("Internalise selection");

      ListItems = SelectedItems.ToList();

      foreach (var param in Sources)
        param.Recipients.Remove(this);

      Sources.Clear();
      OnObjectChanged(GH_ObjectEventType.Sources);

      OnDisplayExpired(false);
    }

    protected override void Menu_AppendExtractParameter(ToolStripDropDown menu) { }

    protected void Menu_InvertSelectionClicked(object sender, EventArgs e)
    {
      foreach (var item in ListItems)
        item.Selected = !item.Selected;

      ResetPersistentData(SelectedItems.Select(x => x.Value), "Invert selection");
    }

    protected void Menu_SelectAllClicked(object sender, EventArgs e)
    {
      foreach (var item in ListItems)
        item.Selected = true;

      ResetPersistentData(ListItems.Select(x => x.Value), "Select all");
    }

    class ComponentAttributes : GH_ResizableAttributes<ValueSet>
    {
      public override bool HasInputGrip => true;
      public override bool HasOutputGrip => true;
      public override bool AllowMessageBalloon => true;
      protected override Padding SizingBorders => new Padding(4, 6, 4, 6);
      protected override Size MinimumSize => new Size(50, 25 + 18 * 5);

      public ComponentAttributes(ValueSet owner) : base(owner)
      {
        Bounds = new RectangleF
        (
          Bounds.Location,
          new SizeF(Math.Max(Bounds.Width, MinimumSize.Width), Math.Max(Bounds.Height, MinimumSize.Height))
        );
      }
      protected override void Layout()
      {
        if (MaximumSize.Width < Bounds.Width || Bounds.Width < MinimumSize.Width)
          Bounds = new RectangleF(Bounds.Location, new SizeF(Bounds.Width < MinimumSize.Width ? MinimumSize.Width : MaximumSize.Width, Bounds.Height));
        if (MaximumSize.Height < Bounds.Height || Bounds.Height < MinimumSize.Height)
          Bounds = new RectangleF(Bounds.Location, new SizeF(Bounds.Width, Bounds.Height < MinimumSize.Height ? MinimumSize.Height : MaximumSize.Height));

        var itemBounds = new RectangleF(Bounds.X + 2, Bounds.Y + 20, Bounds.Width - 4, 18);

        for (int i = 0; i < Owner.ListItems.Count; i++)
        {
          Owner.ListItems[i].BoxName = itemBounds;
          itemBounds = new RectangleF(itemBounds.X, itemBounds.Y + itemBounds.Height, itemBounds.Width, itemBounds.Height);
        }

        base.Layout();
      }

      const int CaptionHeight = 20;
      const int ItemHeight = 18;
      const int FootnoteHeight = 18;
      const int ScrollerWidth = 8;

      float ScrollRatio = 0.0f;

      float Scrolling = float.NaN;
      float ScrollingY = float.NaN;

      int LastItemIndex = 0;

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        switch (channel)
        {
          case GH_CanvasChannel.Wires:
          {
            if (Owner.SourceCount > 0)
              RenderIncomingWires(canvas.Painter, Owner.Sources, Owner.WireDisplay);
            break;
          }
          case GH_CanvasChannel.Objects:
          {
            var bounds = Bounds;
            if (!canvas.Viewport.IsVisible(ref bounds, 10))
              return;

            var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Owner);
            using (var capsule = GH_Capsule.CreateCapsule(bounds, palette))
            {
              capsule.AddInputGrip(InputGrip.Y);
              capsule.AddOutputGrip(OutputGrip.Y);
              capsule.Render(canvas.Graphics, Selected, Owner.Locked, false);
            }

            var alpha = GH_Canvas.ZoomFadeLow;
            if (alpha > 0)
            {
              canvas.SetSmartTextRenderingHint();
              var style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, this);
              var textColor = Color.FromArgb(alpha, style.Text);

              var captionColor = string.IsNullOrEmpty(Owner.NickName) || !Owner.MutableNickName ?
                                 Color.FromArgb(alpha / 2, style.Text) : textColor;

              using (var nameFill = new SolidBrush(captionColor))
                graphics.DrawString(string.IsNullOrEmpty(Owner.NickName) ? "Filter mask…" : Owner.NickName, GH_FontServer.LargeAdjusted, nameFill, Bounds, GH_TextRenderingConstants.StringFormat(StringAlignment.Center, StringAlignment.Near));

              {
                var clip = ListBounds;
                clip.Inflate(-2, 0);

                Brush alternateBrush = null;
                if (GH_Canvas.ZoomFadeMedium > 0 && Owner.DataType == GH_ParamData.remote)
                {
                  graphics.FillRectangle(Brushes.White, clip);
                  alternateBrush = Brushes.WhiteSmoke;
                }
                else
                {
                  alternateBrush = new SolidBrush(Color.FromArgb(70, style.Fill));
                }

                graphics.SetClip(clip);

                var transform = graphics.Transform;
                if (!ScrollerBounds.IsEmpty)
                  graphics.TranslateTransform(0.0f, -((Owner.ListItems.Count * ItemHeight) - clip.Height) * ScrollRatio);

                using (var format = new StringFormat(StringFormatFlags.NoWrap) { LineAlignment = StringAlignment.Center })
                {
                  var itemBounds = new Rectangle((int) clip.X, (int) clip.Y, (int) clip.Width, (int) 18);
                  int index = 0;
                  foreach (var item in Owner.ListItems)
                  {
                    if (index++ % 2 != 0)
                      graphics.FillRectangle(alternateBrush, itemBounds);

                    if (item.Selected)
                    {
                      if (Owner.DataType == GH_ParamData.remote && GH_Canvas.ZoomFadeMedium > 0)
                      {
                        var highlightBounds = itemBounds;
                        highlightBounds.Inflate(-1, -1);
                        GH_GraphicsUtil.RenderHighlightBox(graphics, highlightBounds, 2, true, true);
                      }

                      var markBounds = new RectangleF(itemBounds.X, itemBounds.Y, 22, itemBounds.Height);
                      RenderCheckMark(graphics, markBounds, textColor);
                    }

                    var nameBounds = new RectangleF(itemBounds.X + 22, itemBounds.Y, itemBounds.Width - 22, itemBounds.Height);
                    graphics.DrawString(item.Name, GH_FontServer.StandardAdjusted, Brushes.Black, nameBounds, format);
                    itemBounds.Y += itemBounds.Height;
                  }
                }

                graphics.Transform = transform;

                RenderScrollBar(canvas, graphics, style.Text);

                graphics.ResetClip();

                if (GH_Canvas.ZoomFadeMedium > 0 && Owner.DataType == GH_ParamData.remote)
                {
                  graphics.DrawRectangle(Pens.Black, clip);
                  GH_GraphicsUtil.ShadowHorizontal(graphics, clip.Left, clip.Right, clip.Top);
                }
                else
                {
                  GH_GraphicsUtil.EtchFadingHorizontal(graphics, (int) bounds.Left, (int) bounds.Right, (int) (bounds.Top + 20), (int) (0.8 * alpha), (int) (0.3 * alpha));
                  GH_GraphicsUtil.EtchFadingHorizontal(graphics, (int) bounds.Left, (int) bounds.Right, (int) (bounds.Bottom - 16), (int) (0.8 * alpha), (int) (0.3 * alpha));
                }

                var footnoteBounds = new RectangleF(bounds.Left, bounds.Bottom - 17, bounds.Width - 3, 17);
                graphics.DrawString($"{Owner.ListItems.Count} items", GH_FontServer.StandardAdjusted, Brushes.Gray, footnoteBounds, GH_TextRenderingConstants.FarCenter);
              }
            }

            return;
          }
        }

        base.Render(canvas, graphics, channel);
      }

      public static void RenderCheckMark(Graphics graphics, RectangleF bounds, Color color)
      {
        var x = (int) (bounds.X + 0.5F * bounds.Width) - 2;
        var y = (int) (bounds.Y + 0.5F * bounds.Height);
        var corners = new PointF[]
        {
          new PointF(x, y),
          new PointF(x - 3.5F, y - 3.5F),
          new PointF(x - 6.5F, y - 0.5F),
          new PointF(x, y + 6.0F),
          new PointF(x + 9.5F, y - 3.5F),
          new PointF(x + 6.5F, y - 6.5F)
        };

        using (var edge = new Pen(color, 1.0F))
        {
          edge.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
          graphics.FillPolygon(new SolidBrush(Color.FromArgb(150, color)), corners);
          graphics.DrawPolygon(edge, corners);
        }
      }

      Rectangle ListBounds => new Rectangle
        (
          (int) Bounds.X + 2, (int) Bounds.Y + CaptionHeight,
          (int) Bounds.Width - 4, (int) Bounds.Height - CaptionHeight - FootnoteHeight
        );

      Rectangle ScrollerBounds
      {
        get
        {
          var total = Owner.ListItems.Count * ItemHeight;
          if (total > 0)
          {
            var scrollerBounds = ListBounds;
            var factor = (double) scrollerBounds.Height / total;
            if (factor < 1.0)
            {
              var scrollSize = Math.Max((scrollerBounds.Height) * factor, ItemHeight);
              var position = ((scrollerBounds.Height - scrollSize) * ScrollRatio);
              return new Rectangle
              (
                scrollerBounds.Right - ScrollerWidth - 2,
                scrollerBounds.Top + (int) Math.Round(position),
                ScrollerWidth,
                (int) Math.Round(scrollSize)
              );
            }
          }

          return Rectangle.Empty;
        }
      }

      void RenderScrollBar(GH_Canvas canvas, Graphics graphics, Color color)
      {
        var total = Owner.ListItems.Count * ItemHeight;
        if (total > 0)
        {
          var scrollerBounds = ScrollerBounds;
          if (!scrollerBounds.IsEmpty)
          {
            using (var pen = new Pen(Color.FromArgb(100, color), ScrollerWidth)
            {
              StartCap = System.Drawing.Drawing2D.LineCap.Round,
              EndCap   = System.Drawing.Drawing2D.LineCap.Round
            })
            {
              var startPoint = new Point(scrollerBounds.X + (scrollerBounds.Width / 2), scrollerBounds.Top + 5);
              var endPoint = new Point(scrollerBounds.X + (scrollerBounds.Width / 2), scrollerBounds.Bottom - 5);

              graphics.DrawLine(pen, startPoint, endPoint);
            }
          }
        }
      }

      public override GH_ObjectResponse RespondToMouseDown(GH_Canvas canvas, GH_CanvasMouseEvent e)
      {
        if (canvas.Viewport.Zoom >= GH_Viewport.ZoomDefault * 0.6f)
        {
          if (e.Button == MouseButtons.Left)
          {
            var clientBounds = new RectangleF(Bounds.X + SizingBorders.Left, Bounds.Y + SizingBorders.Top, Bounds.Width - SizingBorders.Horizontal, Bounds.Height - SizingBorders.Vertical);
            if (clientBounds.Contains(e.CanvasLocation))
            {
              var listBounds = new RectangleF(Bounds.X + 2, Bounds.Y + 20, Bounds.Width - 4, Bounds.Height - 38);
              if (listBounds.Contains(e.CanvasLocation))
              {
                var scrollerBounds = ScrollerBounds;
                var canvasLocation = new Point((int) e.CanvasLocation.X, (int) e.CanvasLocation.Y);
                if (scrollerBounds.Contains(canvasLocation))
                {
                  ScrollingY = e.CanvasY;
                  Scrolling = ScrollRatio;
                  return GH_ObjectResponse.Handled;
                }
                else if (Owner.DataType == GH_ParamData.remote && canvas.Viewport.Zoom >= GH_Viewport.ZoomDefault * 0.8f)
                {
                  var scrolledCanvasLocation = e.CanvasLocation;
                  if (!ScrollerBounds.IsEmpty)
                    scrolledCanvasLocation.Y += ((Owner.ListItems.Count * ItemHeight) - ListBounds.Height) * ScrollRatio;

                  
                  bool keepSelection  = (Control.ModifierKeys & Keys.Control) != Keys.None;
                  bool rangeSelection = (Control.ModifierKeys & Keys.Shift)   != Keys.None;
                  int lastItemIndex = 0;

                  bool sel = LastItemIndex < Owner.ListItems.Count ? Owner.ListItems[LastItemIndex].Selected : false;
                  for (int i = 0; i < Owner.ListItems.Count; i++)
                  {
                    if (Owner.ListItems[i].BoxName.Contains(scrolledCanvasLocation))
                    {
                      Owner.ListItems[i].Selected ^= true;
                      lastItemIndex = i;
                    }
                    else if (!keepSelection)
                    {
                      Owner.ListItems[i].Selected = false;
                    }
                  }

                  if (rangeSelection)
                  {
                    int min = Math.Min(lastItemIndex, LastItemIndex);
                    int max = Math.Max(lastItemIndex, LastItemIndex);

                    for (int i = min; i <= max; i++)
                      Owner.ListItems[i].Selected = sel;
                  }

                  LastItemIndex = lastItemIndex;
                  Owner.ResetPersistentData(Owner.SelectedItems.Select(x => x.Value), "Change selection");

                  return GH_ObjectResponse.Handled;
                }
              }
            }
          }
        }
        return base.RespondToMouseDown(canvas, e);
      }

      public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        Scrolling = float.NaN;
        ScrollingY = float.NaN;

        return base.RespondToMouseUp(sender, e);
      }

      public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (e.Button == MouseButtons.Left && !float.IsNaN(Scrolling))
        {
          var dy = e.CanvasY - ScrollingY;
          var ty = ListBounds.Height - ScrollerBounds.Height;
          var f = dy / ty;

          ScrollRatio = Math.Max(0.0f, Math.Min(Scrolling + f, 1.0f));

          ExpireLayout();
          sender.Refresh();
          return GH_ObjectResponse.Handled;
        }

        return base.RespondToMouseMove(sender, e);
      }

      public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas canvas, GH_CanvasMouseEvent e)
      {
        if (canvas.Viewport.Zoom >= GH_Viewport.ZoomDefault * 0.6f)
        {
          if (e.Button == MouseButtons.Left)
          {
            if (Owner.MutableNickName && e.CanvasLocation.Y < Bounds.Top + 20.0f)
            {
              var objectMenu = new ContextMenuStrip();

              Owner.AppendMenuItems(objectMenu);
              if (objectMenu.Items.Count > 0)
              {
                canvas.ActiveInteraction = null;
                objectMenu.Show(canvas, e.ControlLocation);
              }

              return GH_ObjectResponse.Handled;
            }

            if (Owner.DataType == GH_ParamData.remote && canvas.Viewport.Zoom >= GH_Viewport.ZoomDefault * 0.8f)
            {
              var listBounds = new RectangleF(ListBounds.X, ListBounds.Y, ListBounds.Width, ListBounds.Height);
              if (listBounds.Contains(e.CanvasLocation))
              {
                foreach (var item in Owner.ListItems)
                  item.Selected = true;

                Owner.ResetPersistentData(Owner.ListItems.Select(x => x.Value), "Select all");

                return GH_ObjectResponse.Handled;
              }
            }
          }
        }

        return GH_ObjectResponse.Ignore;
      }
    }

    public override void CreateAttributes() => m_attributes = new ComponentAttributes(this);
    public override void AddedToDocument(GH_Document document)
    {
      if (NickName == Name)
        NickName = string.Empty;

      base.AddedToDocument(document);
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int culling = (int) DefaultCulling;
      reader.TryGetInt32("Culling", ref culling);
      Culling = (DataCulling) culling;

      return true;
    }
    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Culling != DefaultCulling)
        writer.SetInt32("Culling", (int) Culling);

      return true;
    }

    public override void ClearData()
    {
      base.ClearData();

      foreach (var goo in PersistentData)
      {
        if (goo is RhinoInside.Revit.GH.Types.IGH_ReferenceData id) id.UnloadReferencedData();
        else if (goo is IGH_GeometricGoo geo) geo.ClearCaches();
      }
    }

    protected void ResetPersistentData(IEnumerable<IGH_Goo> list, string name)
    {
      RecordPersistentDataEvent(name);

      PersistentData.Clear();
      if(list is object)
        PersistentData.AppendRange(list, new GH_Path(0));

      OnObjectChanged(GH_ObjectEventType.PersistentData);

      ExpireSolution(true);
    }

    protected virtual void LoadVolatileData()
    {
      foreach (var branch in m_data.Branches)
      {
        for (int i = 0; i < branch.Count; i++)
        {
          var goo = branch[i];

          if (goo is RhinoInside.Revit.GH.Types.IGH_ReferenceData id && id.IsReferencedData && !id.IsReferencedDataLoaded && !id.LoadReferencedData())
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"A referenced {goo.TypeName} could not be found.");
          }
          else if (goo is IGH_GeometricGoo geo && geo.IsReferencedGeometry && !geo.IsGeometryLoaded && !geo.LoadGeometry())
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"A referenced {geo.TypeName} could not be found in the Rhino document.");
          }
        }
      }
    }

    protected virtual void PreProcessVolatileData() { }

    protected abstract void ProcessVolatileData();

    protected virtual void PostProcessVolatileData() => base.PostProcessData();

    protected override void OnVolatileDataCollected()
    {
      base.OnVolatileDataCollected();

      if (Culling != DataCulling.None)
      {
        var data = new GH_Structure<IGH_Goo>();
        var pathCount = m_data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var path = m_data.Paths[p];
          var branch = m_data.get_Branch(path);

          var items = branch.Cast<IGH_Goo>();
          if (Culling.HasFlag(DataCulling.Nulls))
            items = items.Where(x => x is object);

          if (Culling.HasFlag(DataCulling.Invalids))
            items = items.Where(x => x?.IsValid != false);

          if (Culling.HasFlag(DataCulling.Duplicates))
            items = items.Distinct(new GooEqualityComparer());

          if (!Culling.HasFlag(DataCulling.Empty) || items.Any())
            data.AppendRange(items, path);
        }

        m_data = data;
      }
    }

    public sealed override void PostProcessData()
    {
      LoadVolatileData();

      PreProcessVolatileData();

      ProcessVolatileData();

      // Show elements sorted Alphabetically
      ListItems.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));

      PostProcessVolatileData();
    }

    public override void RegisterRemoteIDs(GH_GuidTable id_list)
    {
      foreach (var item in SelectedItems)
      {
        if (item.Value is IGH_GeometricGoo geo)
          id_list.Add(geo.ReferenceID, this);
      }
    }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new GH_HtmlFormatter(this)
      {
        Title = Name,
        Description =
        @"<p>This component is a special interface object that allows for quick picking an item from a list.</p>" +
        @"<p>Double click on it and use the name input box to enter an exact name, alternativelly you can enter a name patter. " +
        @"If a pattern is used, this param list will be filled up with all the items that match it.</p>" +
        @"<p>Several kind of patterns are supported, the method used depends on the first pattern character:</p>" +
        @"<dl>" +
        @"<dt><b><</b></dt><dd>Starts with</dd>" +
        @"<dt><b>></b></dt><dd>Ends with</dd>" +
        @"<dt><b>?</b></dt><dd>Contains, same as a regular search</dd>" +
        @"<dt><b>:</b></dt><dd>Wildcards, see Microsoft.VisualBasic " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/operators/like-operator#pattern-options\">LikeOperator</a></dd>" +
        @"<dt><b>;</b></dt><dd>Regular expresion, see " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference\">here</a> as reference</dd>" +
        @"</dl>"
      };

      return nTopic.HtmlFormat();
    }

    #region IGH_StateAwareObject
    string IGH_StateAwareObject.SaveState()
    {
      if (string.IsNullOrEmpty(NickName) && PersistentData.IsEmpty)
        return string.Empty;

      var chunk = new GH_LooseChunk("ValueSet");

      chunk.SetString(nameof(NickName), NickName);
      PersistentData.Write(chunk.CreateChunk(nameof(PersistentData)));

      return chunk.Serialize_Xml();
    }

    void IGH_StateAwareObject.LoadState(string state)
    {
      if (!string.IsNullOrEmpty(state))
      {
        try
        {
          var chunk = new GH_LooseChunk("ValueSet");
          chunk.Deserialize_Xml(state);

          NickName = chunk.GetString(nameof(NickName));
          PersistentData.Read(chunk.FindChunk(nameof(PersistentData)));

          ExpireSolution(false);
          return;
        }
        catch { }
      }

      NickName = string.Empty;
      PersistentData.Clear();
    }
    #endregion
  }

  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ValuePicker : ValueSet
  {
    static readonly Guid ComponentClassGuid = new Guid("AFB12752-3ACB-4ACF-8102-16982A69CDAE");
    public override Guid ComponentGuid => ComponentClassGuid;
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ValuePicker() : base
    (
      name: "Value Picker",
      nickname: string.Empty,
      description: "A value picker for comparable values",
      category: "Params",
      subcategory: "Input"
    )
    { }

    protected override void ProcessVolatileData()
    {
      int nonComparableCount = 0;
      var goosSet = new HashSet<IGH_Goo>
      (
        VolatileData.AllData(true).
        Where(x =>
        {
          if (GooEqualityComparer.IsEquatable(x))
            return true;

          nonComparableCount++;
          return false;
        }),
        new GooEqualityComparer()
      );

      if (nonComparableCount > 0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{nonComparableCount} non comparable elements filtered.");

      if (DataType == GH_ParamData.local)
      {
        ListItems = goosSet.Select(goo => new ListItem(goo, true)).
                    Where(x => string.IsNullOrEmpty(NickName) || RhinoInside.Revit.Operator.IsSymbolNameLike(x.Name, NickName)).
                    ToList();
      }
      else if (DataType == GH_ParamData.remote)
      {
        var selectSet = new HashSet<IGH_Goo>(PersistentData.Where(x => GooEqualityComparer.IsEquatable(x)), new GooEqualityComparer());
        ListItems = goosSet.Select(goo => new ListItem(goo, selectSet.Contains(goo))).
                    Where(x => string.IsNullOrEmpty(NickName) || RhinoInside.Revit.Operator.IsSymbolNameLike(x.Name, NickName)).
                    ToList();
      }
      else
      {
        ListItems.Clear();
      }

      // Cull items that are not selected
      var selectedItems = new HashSet<IGH_Goo>(SelectedItems.Select(x=> x.Value), new GooEqualityComparer());

      var pathCount = m_data.PathCount;
      for (int p = 0; p < pathCount; ++p)
      {
        var path = m_data.get_Path(p);
        var srcBranch = m_data.get_Branch(path);

        var itemCount = srcBranch.Count;
        for (int i = 0; i < itemCount; ++i)
        {
          if (!selectedItems.Contains((IGH_Goo) srcBranch[i]))
            srcBranch[i] = default;
        }
      }
    }
  }
}
