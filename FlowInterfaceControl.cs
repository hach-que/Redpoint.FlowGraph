using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Redpoint.FlowGraph
{
    /// <summary>
    /// A general control for flow graphs.
    /// </summary>
    public class FlowInterfaceControl : UserControl
    {
        /// <summary>
        /// The list of all flow graph elements that the control owns.
        /// </summary>
        private ListOfFlowElements m_Elements = new ListOfFlowElements();

        /// <summary>
        /// The currently selected internal element.
        /// </summary>
        private FlowElement m_SelectedElement = null;

        /// <summary>
        /// Whether the current selected element is still being held down
        /// by the mouse.
        /// </summary>
        private bool m_SelectedElementStillHeldDown = false;

        /// <summary>
        /// Whether the user is still panning the control.
        /// </summary>
        private bool m_PanningStillHeldDown = false;
        private int m_SelectedElementDragX = 0;
        private int m_SelectedElementDragY = 0;
        private int m_AllElementPanX = 0;
        private int m_AllElementPanY = 0;
        private int m_AllElementPanOldX = 0;
        private int m_AllElementPanOldY = 0;
        private FlowConnector m_ActiveConnection = null;

        /// <summary>
        /// Occurs when selected flow element changes.
        /// </summary>
        public event EventHandler SelectedElementChanged;

        /// <summary>
        /// The currently selected flow element in the control.
        /// </summary>
        public FlowElement SelectedElement
        {
            get
            {
                return this.m_SelectedElement;
            }
            set
            {
                if (this.m_SelectedElement != null)
                    this.Invalidate(this.m_SelectedElement.InvalidatingRegion.Apply(this.Zoom).Fix());
                this.m_SelectedElement = value;
                if (this.m_SelectedElement != null)
                    this.Invalidate(this.m_SelectedElement.InvalidatingRegion.Apply(this.Zoom).Fix());
                if (this.SelectedElementChanged != null)
                    this.SelectedElementChanged(this, new EventArgs());
            }
        }

        public ListOfFlowElements Elements
        {
            get
            {
                return this.m_Elements;
            }
        }

        private float m_Zoom = 1.0f;
        public float Zoom
        {
            get
            {
                return this.m_Zoom;
            }
            set
            {
                if (value >= 0.1 && value <= 10)
                {
                    this.m_Zoom = value;
                    this.Invalidate();
                }
            }
        }

        public FlowInterfaceControl()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Name = "FlowInterfaceControl";
            this.Size = new Size(543, 376);
            this.ResumeLayout(false);
        }

        private Point m_MouseActiveConnectionLocation;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(SystemBrushes.ControlDark, e.ClipRectangle);

            // Render each flow element.
            var re = new RenderAttributes
            {
                Graphics = e.Graphics,
                Zoom = this.Zoom,
                Font = new Font(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.Size * this.Zoom)
            };
            foreach (FlowElement el in this.m_Elements)
                FlowElement.RenderTo(el, re, this.m_SelectedElement == el);

            // Render the active connection line.
            if (this.m_ActiveConnection != null)
            {
                e.Graphics.DrawLine(new Pen(Color.Red, 3),
                    this.m_ActiveConnection.Center,
                    this.m_MouseActiveConnectionLocation
                );
            }
        }

        private Point m_LastContextMenuOpenLocation;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Don't do anything on right-click.
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (this.m_ActiveConnection != null)
                {
                    this.m_ActiveConnection = null;
                    this.Invalidate();
                }
                else 
                {
                    this.SelectedElement = null;
                    foreach (FlowElement el in this.m_Elements.Reverse<FlowElement>())
                    {
                        if (el.Region.Contains(e.Location.Apply(1 / this.Zoom)))
                        {
                            this.SelectedElement = el;
                            this.m_SelectedElementStillHeldDown = true;
                            this.m_SelectedElementDragX = (int)(e.X / this.Zoom) - el.X;
                            this.m_SelectedElementDragY = (int)(e.Y / this.Zoom) - el.Y;
                            break;
                        }
                    }
                    
                    this.m_LastContextMenuOpenLocation = e.Location;
                }
                return;
            }

            // If we are currently dragging, ignore this logic.
            if (this.m_SelectedElementStillHeldDown || this.m_PanningStillHeldDown)
                return;

            // Check to see if the mouse is over an element during left press.
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.SelectedElement = null;
                foreach (FlowElement el in this.m_Elements.Reverse<FlowElement>())
                {
                    if (el.Region.Contains(e.Location.Apply(1 / this.Zoom)))
                    {
                        this.SelectedElement = el;
                        this.m_SelectedElementStillHeldDown = true;
                        this.m_SelectedElementDragX = (int)(e.X / this.Zoom) - el.X;
                        this.m_SelectedElementDragY = (int)(e.Y / this.Zoom) - el.Y;
                        break;
                    }
                }
                
                // If we didn't select an element, see if we clicked on a flow
                // connector.
                if (this.SelectedElement == null && this.m_ActiveConnection == null)
                {
                    var range = new Rectangle(
                        e.X - FlowConnector.CONNECTOR_SIZE / 2,
                        e.Y - FlowConnector.CONNECTOR_SIZE / 2,
                        FlowConnector.CONNECTOR_SIZE * 2,
                        FlowConnector.CONNECTOR_SIZE * 2);
                    foreach (FlowElement el in this.m_Elements.Reverse<FlowElement>())
                    {
                        // Check input connectors.
                        foreach (FlowConnector ic in el.InputConnectors)
                        {
                            if (range.Contains(ic.Center))
                            {
                                this.m_ActiveConnection = ic;
                                return;
                            }
                        }

                        // Check output connectors.
                        foreach (FlowConnector ic in el.OutputConnectors)
                        {
                            if (range.Contains(ic.Center))
                            {
                                this.m_ActiveConnection = ic;
                                return;
                            }
                        }
                    }
                }
                // Otherwise if we're clicking on a flow connector and there is an
                // active connection, we probably want to make the connection.
                else if (this.m_ActiveConnection != null)
                {
                    var range = new Rectangle(
                        e.X - FlowConnector.CONNECTOR_SIZE / 2,
                        e.Y - FlowConnector.CONNECTOR_SIZE / 2,
                        FlowConnector.CONNECTOR_SIZE * 2,
                        FlowConnector.CONNECTOR_SIZE * 2);
                    foreach (FlowElement el in this.m_Elements.Reverse<FlowElement>())
                    {
                        // Check input connectors.
                        foreach (FlowConnector ic in el.InputConnectors)
                        {
                            if (range.Contains(ic.Center))
                            {
                                // The user wants to connect this up.  Is it possible?
                                if (this.m_ActiveConnection.IsInput ||
                                    !this.m_ActiveConnection.CanConnectTo(ic) ||
                                    !ic.CanConnectTo(this.m_ActiveConnection))
                                {
                                    // Can't connect an input to an input..
                                    this.m_ActiveConnection = null;
                                    this.Invalidate();
                                    return;
                                }

                                // Set the connection.
                                var old = this.m_ActiveConnection.ConnectedTo;
                                var newFC = new List<FlowConnector>();
                                foreach (FlowConnector fc in old)
                                    if (!newFC.Contains(fc))
                                        newFC.Add(fc);
                                if (!newFC.Contains(ic))
                                    newFC.Add(ic);
                                this.m_ActiveConnection.ConnectedTo = newFC.ToArray();

                                // Finish up by turning off connection mode.
                                this.m_ActiveConnection = null;
                                this.Invalidate();
                                return;
                            }
                        }

                        // Check output connectors.
                        foreach (FlowConnector ic in el.OutputConnectors)
                        {
                            if (range.Contains(ic.Center))
                            {
                                // The user wants to connect this up.  Is it possible?
                                if (this.m_ActiveConnection.IsOutput ||
                                    !this.m_ActiveConnection.CanConnectTo(ic) ||
                                    !ic.CanConnectTo(this.m_ActiveConnection))
                                {
                                    // Can't connect an output to an output..
                                    this.m_ActiveConnection = null;
                                    this.Invalidate();
                                    return;
                                }

                                // Set the connection.
                                var old = this.m_ActiveConnection.ConnectedTo;
                                foreach (var fc in old)
                                {
                                    var newFC = new List<FlowConnector>();
                                    foreach (var ffc in fc.ConnectedTo)
                                        if (ffc != this.m_ActiveConnection)
                                            newFC.Add(ffc);
                                    fc.ConnectedTo = newFC.ToArray();
                                }
                                this.m_ActiveConnection.ConnectedTo = new[] { ic };

                                // Finish up by turning off connection mode.
                                this.m_ActiveConnection = null;
                                this.Invalidate();
                                return;
                            }
                        }
                    }
                }
            }

            // Pan on middle mouse button.
            if (e.Button == System.Windows.Forms.MouseButtons.Middle ||
                e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.m_PanningStillHeldDown = true;
                this.m_AllElementPanX = this.m_AllElementPanOldX = e.X;
                this.m_AllElementPanY = this.m_AllElementPanOldY = e.Y;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            // We are no longer dragging.
            this.m_PanningStillHeldDown = false;
            this.m_SelectedElementStillHeldDown = false;
            this.m_SelectedElementDragX = 0;
            this.m_SelectedElementDragY = 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // If we are dragging an element....
            if (this.m_SelectedElementStillHeldDown && this.m_SelectedElement != null)
            {
                this.Invalidate(this.m_SelectedElement.InvalidatingRegion.Apply(this.Zoom).Fix());
                foreach (Rectangle r in this.m_SelectedElement.GetConnectorRegionsToInvalidate())
                    this.Invalidate(r.Apply(this.Zoom).Fix());
                this.m_SelectedElement.X = (int)(e.X / this.Zoom) - this.m_SelectedElementDragX;
                this.m_SelectedElement.Y = (int)(e.Y / this.Zoom) - this.m_SelectedElementDragY;
                this.Invalidate(this.m_SelectedElement.InvalidatingRegion.Apply(this.Zoom));
                foreach (Rectangle r in this.m_SelectedElement.GetConnectorRegionsToInvalidate())
                    this.Invalidate(r.Apply(this.Zoom).Fix());
            }
            else if (this.m_PanningStillHeldDown)
            {
                this.Invalidate();
                this.m_AllElementPanX = e.X;
                this.m_AllElementPanY = e.Y;
                foreach (FlowElement el in this.m_Elements)
                {
                    el.X += (int)((this.m_AllElementPanX - this.m_AllElementPanOldX) / this.Zoom);
                    el.Y += (int)((this.m_AllElementPanY - this.m_AllElementPanOldY) / this.Zoom);
                }
                this.m_AllElementPanOldX = this.m_AllElementPanX;
                this.m_AllElementPanOldY = this.m_AllElementPanY;
            }
            else if (this.m_ActiveConnection != null)
            {
                var r = new Rectangle(
                    this.m_ActiveConnection.Center.X,
                    this.m_ActiveConnection.Center.Y,
                    this.m_MouseActiveConnectionLocation.X - this.m_ActiveConnection.Center.X,
                    this.m_MouseActiveConnectionLocation.Y - this.m_ActiveConnection.Center.Y);
                r.Inflate(r.Width > 0 ? 10 : -10, r.Height > 0 ? 10 : -10);
                this.Invalidate(r.Fix());
                r = new Rectangle(
                    this.m_ActiveConnection.Center.X,
                    this.m_ActiveConnection.Center.Y,
                    e.X - this.m_ActiveConnection.Center.X,
                    e.Y - this.m_ActiveConnection.Center.Y);
                r.Inflate(r.Width > 0 ? 10 : -10, r.Height > 0 ? 10 : -10);
                this.Invalidate(r.Fix());
            }
            this.m_MouseActiveConnectionLocation = new Point(
                e.X,
                e.Y
            );
        }

        /// <summary>
        /// Invalidates the current control, forcing a
        /// redraw of all flow elements.
        /// </summary>
        public new void Invalidate()
        {
            base.Invalidate();
        }

        /// <summary>
        /// Pans the control by the specified amount.
        /// </summary>
        /// <param name="x">The X amount to pan by.</param>
        /// <param name="y">The Y amount to pan by.</param>
        public void Pan(int x, int y)
        {
            foreach (FlowElement el in this.m_Elements)
            {
                el.X += (int)(x / this.Zoom);
                el.Y += (int)(y / this.Zoom);
            }
            this.Invalidate();
        }

        /// <summary>
        /// Adds the element at the current mouse position.
        /// </summary>
        public void AddElementAtMouse(FlowElement flowElement)
        {
            flowElement.X = (int)(this.m_LastContextMenuOpenLocation.X / this.Zoom);
            flowElement.Y = (int)(this.m_LastContextMenuOpenLocation.Y / this.Zoom);
            this.Elements.Add(flowElement);
            this.Invalidate(flowElement.InvalidatingRegion.Fix());
        }

        /// <summary>
        /// Pushes an element on the queue for reprocessing.
        /// </summary>
        public void PushForReprocessing(FlowElement flowElement)
        {
            if (flowElement == null)
                throw new ArgumentNullException("flowElement");
            flowElement.ObjectReprocessRequested();
        }
    }

    public struct RenderAttributes
    {
        public Graphics Graphics;
        public float Zoom;
        public Font Font;
    }
}
