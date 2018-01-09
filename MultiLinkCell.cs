using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace DoubanCrawler
{
    public class DataGridViewMultiLinkColumn : DataGridViewButtonColumn
    {
        public DataGridViewMultiLinkColumn()
        {
            this.CellTemplate = new DataGridViewMultiLinkCell();
        }
    }

    public class DataGridViewMultiLinkCell : DataGridViewButtonCell
    {
        private bool enabledValue;

        public bool Enabled
        {
            get
            {
                return enabledValue;
            }
            set
            {
                enabledValue = value;
            }
        }

        // Override the Clone method so that the Enabled property is copied.
        public override object Clone()
        {
            DataGridViewMultiLinkCell cell =
                (DataGridViewMultiLinkCell)base.Clone();
            cell.Enabled = this.Enabled;
            return cell;
        }

        // By default, enable the button cell.
        public DataGridViewMultiLinkCell()
        {
            this.enabledValue = true;
        }

        protected override void Paint(Graphics graphics,
            Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates elementState, object value,
            object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            try
            {
                var v = this.FormattedValue;
            }
            catch (Exception)
            {
                base.Paint(graphics, clipBounds, cellBounds, RowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
                return;
            }

            // Draw the cell background, if specified.
            if ((paintParts & DataGridViewPaintParts.Background) ==
                DataGridViewPaintParts.Background)
            {
                SolidBrush cellBackground =
                    new SolidBrush(cellStyle.BackColor);
                graphics.FillRectangle(cellBackground, cellBounds);
                cellBackground.Dispose();
            }

            // Draw the cell borders, if specified.
            if ((paintParts & DataGridViewPaintParts.Border) ==
                DataGridViewPaintParts.Border)
            {
                PaintBorder(graphics, clipBounds, cellBounds, cellStyle,
                    advancedBorderStyle);
            }

            //each value,each button,each text


            // Calculate the area in which to draw the button.
            Rectangle buttonArea = cellBounds;
            Rectangle buttonAdjustment =
                this.BorderWidths(advancedBorderStyle);
            buttonArea.X += buttonAdjustment.X;
            buttonArea.Y += buttonAdjustment.Y;
            buttonArea.Height -= buttonAdjustment.Height;
            buttonArea.Width -= buttonAdjustment.Width;

            foreach (var tag in this.FormattedValue.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var text = TextRenderer.MeasureText(tag, this.DataGridView.Font);
                buttonArea.Width = text.Width;

                // Draw the disabled button.
                ButtonRenderer.DrawButton(graphics, buttonArea,
                    PushButtonState.Normal);

                TextRenderer.DrawText(graphics,
                      tag,
                        this.DataGridView.Font,
                        buttonArea, SystemColors.GrayText);
                buttonArea.X += buttonArea.Width;
            }




            // Draw the disabled button text.
            //if (this.FormattedValue is String)
            //{
            //    TextRenderer.DrawText(graphics,
            //        (string)this.FormattedValue,
            //        this.DataGridView.Font,
            //        buttonArea, SystemColors.GrayText);
            //}


        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
        {
            //var left = 0;
            //foreach (var tag in this.FormattedValue.ToString().Split(','))
            //{
            //    var text = TextRenderer.MeasureText(tag, this.DataGridView.Font);
            //    left += text.Width;
            //    if (left > e.Location.X)
            //    {
            //        MessageBox.Show(tag);

            //        break;
            //    }
            //}

            base.OnMouseClick(e);
        }

    }
}