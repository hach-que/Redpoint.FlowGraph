using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.Serialization;

namespace Redpoint.FlowGraph
{
    [DataContract]
    public abstract class FlowElement
    {
        #region General Properties

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public int X
        {
            get;
            set;
        }

        [DataMember]
        public int Y
        {
            get;
            set;
        }

        [DataMember]
        public int Width
        {
            get;
            protected set;
        }

        [DataMember]
        public int Height
        {
            get;
            protected set;
        }

        [DataMember]
        public bool ProcessingDisabled
        {
            get;
            set;
        }
        
        public virtual Bitmap Image
        {
            get;
            protected set;
        }

        public int ImageWidth
        {
            get
            {
                return this.Width - 2;
            }
            set
            {
                this.Width = value + 2;
            }
        }

        public int ImageHeight
        {
            get
            {
                return this.Height - 22;
            }
            set
            {
                this.Height = value + 22;
            }
        }

        public Rectangle Region
        {
            get
            {
                return new Rectangle(
                    this.X,
                    this.Y,
                    this.Width,
                    this.Height
                );
            }
        }

        public Rectangle InvalidatingRegion
        {
            get
            {
                return new Rectangle(
                    this.X
                    - FlowConnector.CONNECTOR_PADDING * 2
                    - FlowConnector.CONNECTOR_SIZE
                    - (this.InputConnectors.Count == 0 ? 0 : this.InputConnectors.Max(v => v.InvalidationWidth)),
                    this.Y,
                    this.Width
                    + (FlowConnector.CONNECTOR_PADDING * 3 + FlowConnector.CONNECTOR_SIZE) * 2
                    + (this.InputConnectors.Count == 0 ? 0 : this.InputConnectors.Max(v => v.InvalidationWidth))
                    + (this.OutputConnectors.Count == 0 ? 0 : this.OutputConnectors.Max(v => v.InvalidationWidth)),
                    this.Height + (this.m_AdditionalInformation == null ? 0 : this.m_AdditionalInformation.Height)
                );
            }
        }

        public Rectangle TitleRegion
        {
            get
            {
                return new Rectangle(
                    this.X,
                    this.Y,
                    this.Width,
                    20
                );
            }
        }

        private static readonly List<FlowConnector> EmptyFlowConnectorList = new List<FlowConnector>();

        public virtual List<FlowConnector> InputConnectors
        {
            get
            {
                return EmptyFlowConnectorList;
            }
        }

        public virtual List<FlowConnector> OutputConnectors
        {
            get
            {
                return EmptyFlowConnectorList;
            }
        }

        #endregion

        #region Static Methods and Rendering

        private static SolidBrush m_TitleHighlight = new SolidBrush(Color.FromArgb(255, 255, 192));

        internal static void RenderTo(FlowElement el, RenderAttributes re, bool selected)
        {
            int ex = (int)(el.X * re.Zoom);
            int ey = (int)(el.Y * re.Zoom);
            int ew = (int)(el.Width * re.Zoom);
            int eh = (int)(el.Height * re.Zoom);
            int eiw = (int)Math.Floor(el.ImageWidth * re.Zoom);
            int eih = (int)Math.Floor(el.ImageHeight * re.Zoom);
            int etx = (int)((el.X + 4) * re.Zoom);
            int ety = (int)((el.Y + 4) * re.Zoom);
            int aiw = 0, aih = 0;
            if (el.m_AdditionalInformation != null)
            {
                aiw = (int)Math.Floor(el.m_AdditionalInformation.Width * re.Zoom);
                aih = (int)Math.Floor(el.m_AdditionalInformation.Height * re.Zoom);
            }

            re.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            re.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            re.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            re.Graphics.FillRectangle(selected ? m_TitleHighlight : SystemBrushes.Control, ex, ey, ew - 1 * re.Zoom, eh - 1 * re.Zoom);
            re.Graphics.DrawRectangle(Pens.Black, ex, ey, ew - 1 * re.Zoom, eh - 1 * re.Zoom);
            re.Graphics.DrawString(el.Name, re.Font, SystemBrushes.ControlText, new PointF(etx, ety));
            var image = el.Image;
            if (image != null)
                re.Graphics.DrawImage(image, ex + 1 * re.Zoom, ey + 21 * re.Zoom, eiw, eih);
            var additional = el.m_AdditionalInformation;
            if (additional != null)
                re.Graphics.DrawImage(additional, ex + 1 * re.Zoom, ey + 21 * re.Zoom + eih, aiw, aih);

            foreach (FlowConnector fl in el.OutputConnectors)
                FlowConnector.RenderTo(fl, re);
            foreach (FlowConnector fl in el.InputConnectors)
                FlowConnector.RenderTo(fl, re);
        }

        #endregion

        public static int GetConnectorIndex(FlowElement el, FlowConnector fl)
        {
            if (fl.IsInput)
                return el.InputConnectors.IndexOf(fl);
            return el.OutputConnectors.IndexOf(fl);
        }

        public IEnumerable<Rectangle> GetConnectorRegionsToInvalidate()
        {
            foreach (FlowConnector f in this.InputConnectors)
                foreach (Rectangle r in f.GetConnectorRegionsToInvalidate())
                    yield return r;
            foreach (FlowConnector f in this.OutputConnectors)
                foreach (Rectangle r in f.GetConnectorRegionsToInvalidate())
                    yield return r;
        }

        /// <summary>
        /// The additional information bitmap.  We can assume that this
        /// won't change unless ObjectReprocessRequested() is called.
        /// </summary>
        protected internal Bitmap m_AdditionalInformation = null;

        public virtual object GetObjectToInspect()
        {
            return null;
        }

        public virtual void ObjectPropertyUpdated()
        {
        }

        public virtual void ObjectReprocessRequested()
        {
        }

        public virtual void SetDeserializationData(FlowInterfaceControl control)
        {
        }
    }
}
