﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotViewGtk3.cs" company="OxyPlot">
//   Copyright (c) 2015 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OxyPlot;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Gdk;
using Gtk;

namespace OxyPlot.GtkSharp
{
    public class PlotView : Layout, IPlotView
    {
        /// <summary>
        /// The category for the properties of this control.
        /// </summary>
        private const string OxyPlotCategory = "OxyPlot";

        /// <summary>
        /// The invalidate lock.
        /// </summary>
        private readonly object invalidateLock = new object();

        /// <summary>
        /// The model lock.
        /// </summary>
        private readonly object modelLock = new object();

        /// <summary>
        /// The rendering lock.
        /// </summary>
        private readonly object renderingLock = new object();

        /// <summary>
        /// The render context.
        /// </summary>
        private readonly GraphicsRenderContext renderContext;

        /// <summary>
        /// The tracker label
        /// </summary>
        [NonSerialized]
        private Gtk.Label trackerLabel = null;

        /// <summary>
        /// The current model (holding a reference to this plot view).
        /// </summary>
        [NonSerialized]
        private PlotModel currentModel;

        /// <summary>
        /// The is model invalidated.
        /// </summary>
        private bool isModelInvalidated;

        /// <summary>
        /// The model.
        /// </summary>
        private PlotModel model;

        /// <summary>
        /// The update data flag.
        /// </summary>
        private bool updateDataFlag = true;

        /// <summary>
        /// The zoom rectangle.
        /// </summary>
        private OxyRect? zoomRectangle;

        /// <summary>
        /// The default controller
        /// </summary>
        private IPlotController defaultController;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        public PlotView() : base(null, null)
        {
            this.renderContext = new GraphicsRenderContext();

            // ReSharper restore DoNotCallOverridableMethodsInConstructor
            this.PanCursor = new Cursor(Gdk.Display.Default, Gdk.CursorType.Sizing);
            this.ZoomRectangleCursor = new Cursor(Gdk.Display.Default, Gdk.CursorType.Sizing);
            this.ZoomHorizontalCursor = new Cursor(Gdk.Display.Default, Gdk.CursorType.SbHDoubleArrow);
            this.ZoomVerticalCursor = new Cursor(Gdk.Display.Default, Gdk.CursorType.SbVDoubleArrow);
            this.AddEvents((int)(EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask | EventMask.ScrollMask | EventMask.KeyPressMask | EventMask.PointerMotionMask));
            this.CanFocus = true;
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [Category(OxyPlotCategory)]
        public PlotModel Model
        {
            get
            {
                return this.model;
            }

            set
            {
                if (this.model != value)
                {
                    this.model = value;
                    this.OnModelChanged();
                }
            }
        }

        /// <summary>
        /// Gets the actual <see cref="OxyPlot.Model" /> of the control.
        /// </summary>
        Model IView.ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the actual <see cref="PlotModel" /> of the control.
        /// </summary>
        public PlotModel ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the actual controller.
        /// </summary>
        /// <value>
        /// The actual <see cref="IController" />.
        /// </value>
        IController IView.ActualController
        {
            get
            {
                return this.ActualController;
            }
        }

        /// <summary>
        /// Gets the coordinates of the client area of the view.
        /// </summary>
        public OxyRect ClientArea
        {
            get
            {
                return new OxyRect(0, 0, Allocation.Width, Allocation.Height);
            }
        }

        /// <summary>
        /// Gets the actual plot controller.
        /// </summary>
        /// <value>The actual plot controller.</value>
        public IPlotController ActualController
        {
            get
            {
                return this.Controller ?? (this.defaultController ?? (this.defaultController = new PlotController()));
            }
        }

        /// <summary>
        /// Gets or sets the plot controller.
        /// </summary>
        /// <value>The controller.</value>
        public IPlotController Controller { get; set; }

        /// <summary>
        /// Gets or sets the pan cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor PanCursor { get; set; }

        /// <summary>
        /// Gets or sets the horizontal zoom cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor ZoomHorizontalCursor { get; set; }

        /// <summary>
        /// Gets or sets the rectangle zoom cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor ZoomRectangleCursor { get; set; }

        /// <summary>
        /// Gets or sets the vertical zoom cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor ZoomVerticalCursor { get; set; }

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        public void HideTracker()
        {
            if (this.trackerLabel != null)
                this.trackerLabel.Parent.Visible = false;
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        public void HideZoomRectangle()
        {
            this.zoomRectangle = null;
            this.QueueDraw();
        }

        /// <summary>
        /// Invalidates the plot (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">if set to <c>true</c>, all data collections will be updated.</param>
        public void InvalidatePlot(bool updateData)
        {
            lock (this.invalidateLock)
            {
                this.isModelInvalidated = true;
                this.updateDataFlag = this.updateDataFlag || updateData;
            }

            this.QueueDraw();
        }

        /// <summary>
        /// Called when the Model property has been changed.
        /// </summary>
        public void OnModelChanged()
        {
            lock (this.modelLock)
            {
                if (this.currentModel != null)
                {
                    ((IPlotModel)this.currentModel).AttachPlotView(null);
                    this.currentModel = null;
                }

                if (this.Model != null)
                {
                    ((IPlotModel)this.Model).AttachPlotView(this);
                    this.currentModel = this.Model;
                }
            }

            this.InvalidatePlot(true);
        }

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="data">The data.</param>
        public void ShowTracker(TrackerHitResult data)
        {
            if (this.trackerLabel == null)
            {
                // Holding the tracker label inside an EventBox allows
                // us to set the background color
                Gtk.EventBox labelHolder = new Gtk.EventBox();
                this.trackerLabel = new Gtk.Label();
                this.trackerLabel.SetPadding(3, 3);
                OxyColor bgColor = OxyColors.LightSkyBlue;
                labelHolder.ModifyBg(StateType.Normal, new Gdk.Color(bgColor.R, bgColor.G, bgColor.B));
                labelHolder.Add(this.trackerLabel);
                this.Add(labelHolder);
                labelHolder.ShowAll();
            }
            this.trackerLabel.Parent.Visible = true;
            this.trackerLabel.Text = data.ToString();
            Gtk.Requisition req = this.trackerLabel.Parent.SizeRequest();
            int xPos = (int)data.Position.X - req.Width / 2;
            int yPos = (int)data.Position.Y - req.Height;
            xPos = Math.Max(0, Math.Min(xPos, this.Allocation.Width - req.Width));
            yPos = Math.Max(0, Math.Min(yPos, this.Allocation.Height - req.Height));
            this.Move(trackerLabel.Parent, xPos, yPos);
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        public void ShowZoomRectangle(OxyRect rectangle)
        {
            this.zoomRectangle = rectangle;
            this.QueueDraw();
        }

        /// <summary>
        /// Sets the clipboard text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetClipboardText(string text)
        {
            try
            {
                // todo: can't get the following solution to work
                // http://stackoverflow.com/questions/5707990/requested-clipboard-operation-did-not-succeed
                this.GetClipboard(Gdk.Selection.Clipboard).Text = text;
            }
            catch (ExternalException)
            {
                // Requested Clipboard operation did not succeed.
                // MessageBox.Show(this, ee.Message, "OxyPlot");
            }
        }

        /// <summary>
        /// Called when the mouse button is pressed.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        protected override bool OnButtonPressEvent(EventButton e)
        {
            this.GrabFocus();

            return this.ActualController.HandleMouseDown(this, e.ToMouseDownEventArgs());
        }

        /// <summary>
        /// Called on mouse move events.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        protected override bool OnMotionNotifyEvent(EventMotion e)
        {
            return this.ActualController.HandleMouseMove(this, e.ToMouseEventArgs());
        }

        /// <summary>
        /// Called when the mouse button is released.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        protected override bool OnButtonReleaseEvent(EventButton e)
        {
            return this.ActualController.HandleMouseUp(this, e.ToMouseUpEventArgs());
        }

        /// <summary>
        /// Called when the mouse wheel is scrolled.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        /// <remarks>
        /// The way that scroll direction is determined is different between
        /// gtk2 and gtk3, hence the need for version-specific imlementations of
        /// GetMouseWheelEventArgs(Gdk.EventScroll) function.
        /// </remarks>
        protected override bool OnScrollEvent(EventScroll e)
        {
            return this.ActualController.HandleMouseWheel(this, GetMouseWheelEventArgs(e));
        }

        /// <summary>
        /// Called when the mouse enters the widget.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        protected override bool OnEnterNotifyEvent(EventCrossing e)
        {
            // If mouse has entered from an inferior window (ie the tracker label),
            // further propagation of the event could be dangerous; e.g. if it results in
            // the label being moved, it will cause further LeaveNotify and MotionNotify
            // events being fired under X11.
            if (e.Detail == NotifyType.Inferior)
                return base.OnEnterNotifyEvent(e);
            return this.ActualController.HandleMouseEnter(this, e.ToMouseEventArgs());
        }

        /// <summary>
        /// Called when the mouse leaves the widget.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        protected override bool OnLeaveNotifyEvent(EventCrossing e)
        {
            // If mouse has left via an inferior window (ie the tracker label),
            // further propagation of the event could be dangerous; e.g. if it results in
            // the label being moved, it will cause further LeaveNotify and MotionNotify
            // events being fired under X11.
            if (e.Detail == NotifyType.Inferior)
                return base.OnLeaveNotifyEvent(e);
            return this.ActualController.HandleMouseLeave(this, e.ToMouseEventArgs());
        }

        /// <summary>
        /// Called on KeyPress event.
        /// </summary>
        /// <param name="e">An instance that contains the event data.</param>
        /// <returns>True if event was handled?</returns>
        protected override bool OnKeyPressEvent(EventKey e)
        {
            return this.ActualController.HandleKeyDown(this, e.ToKeyEventArgs());
        }

        /// <summary>
        /// Draws the plot to a cairo context within the specified bounds.
        /// </summary>
        /// <param name="cr">The cairo context to use for drawing.</param>
        public void DrawPlot(Cairo.Context cr)
        {
            try
            {
                lock (this.invalidateLock)
                {
                    if (this.isModelInvalidated)
                    {
                        if (this.model != null)
                        {
                            ((IPlotModel)this.model).Update(this.updateDataFlag);
                            this.updateDataFlag = false;
                        }

                        this.isModelInvalidated = false;
                    }
                }

                lock (this.renderingLock)
                {
                    this.renderContext.SetGraphicsTarget(cr);
                    if (this.model != null)
                    {
                        if (!this.model.Background.IsUndefined())
                        {
                            OxyRect rect = new OxyRect(0, 0, Allocation.Width, Allocation.Height);
                            this.renderContext.DrawRectangle(rect, this.model.Background, OxyColors.Undefined, 0);
                        }

                        ((IPlotModel)this.model).Render(this.renderContext, Allocation.Width, Allocation.Height);
                    }

                    if (this.zoomRectangle.HasValue)
                    {
                        this.renderContext.DrawRectangle(this.zoomRectangle.Value, OxyColor.FromArgb(0x40, 0xFF, 0xFF, 0x00), OxyColors.Transparent, 1.0);
                    }
                }
            }
            catch (Exception paintException)
            {
                var trace = new StackTrace(paintException);
                Debug.WriteLine(paintException);
                Debug.WriteLine(trace);

                // using (var font = new Font("Arial", 10))
                {
                    // int width; int height;
                    // this.GetSizeRequest(out width, out height);
                    Debug.Assert(false, "OxyPlot paint exception: " + paintException.Message);

                    // g.ResetTransform();
                    // g.DrawString(, font, Brushes.Red, width / 2, height / 2, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }
        }
        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        public void SetCursorType(OxyPlot.CursorType cursorType)
        {
            switch (cursorType)
            {
                case OxyPlot.CursorType.Pan:
                    this.Window.Cursor = this.PanCursor;
                    break;
                case OxyPlot.CursorType.ZoomRectangle:
                    this.Window.Cursor = this.ZoomRectangleCursor;
                    break;
                case OxyPlot.CursorType.ZoomHorizontal:
                    this.Window.Cursor = this.ZoomHorizontalCursor;
                    break;
                case OxyPlot.CursorType.ZoomVertical:
                    this.Window.Cursor = this.ZoomVerticalCursor;
                    break;
                default:
                    this.Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
                    break;
            }
        }

        protected override bool OnDrawn(Cairo.Context cr)
        {
            this.DrawPlot(cr);
            return base.OnDrawn(cr);
        }

        /// <summary>
        /// Creates the mouse wheel event arguments.
        /// </summary>
        /// <param name="e">The scroll event args.</param>
        /// <returns>Mouse event arguments.</returns>
        private static OxyMouseWheelEventArgs GetMouseWheelEventArgs(Gdk.EventScroll e)
        {
            int delta;
#if NETSTANDARD2_0
            if (e.Direction == Gdk.ScrollDirection.Smooth)
                delta = e.DeltaY < 0 ? 120 : -120;
            else
#endif
            delta = e.Direction == Gdk.ScrollDirection.Down ? -120 : 120;
            return new OxyMouseWheelEventArgs
            {
                Delta = delta,
                Position = new ScreenPoint(e.X, e.Y),
                ModifierKeys = ConverterExtensions.GetModifiers(e.State)
            };
        }

    }

}
