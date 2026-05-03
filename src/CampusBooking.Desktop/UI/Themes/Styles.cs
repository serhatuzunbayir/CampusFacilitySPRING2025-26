using System.Drawing;
using System.Windows.Forms;

namespace CampusBooking.Desktop.UI.Themes;

public static class Styles
{
    public static Button PrimaryButton(string text, int width = 110)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = MainTheme.Primary,
            ForeColor = Color.White,
            Font      = MainTheme.BodyBold,
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = MainTheme.PrimaryDark;
        btn.FlatAppearance.MouseDownBackColor = MainTheme.PrimaryDark;
        return btn;
    }

    public static Button SecondaryButton(string text, int width = 110)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = MainTheme.Surface,
            ForeColor = MainTheme.Primary,
            Font      = MainTheme.BodyBold,
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize  = 1;
        btn.FlatAppearance.BorderColor = MainTheme.PrimaryLight;
        btn.FlatAppearance.MouseOverBackColor = MainTheme.SurfaceAlt;
        return btn;
    }

    public static Button TextButton(string text, int width = 90)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = MainTheme.Primary,
            Font      = MainTheme.BodyBold,
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = MainTheme.SurfaceAlt;
        return btn;
    }

    public static Label SectionTitle(string text) => new()
    {
        Text      = text,
        Font      = MainTheme.Subheading,
        ForeColor = MainTheme.TextPrimary,
        AutoSize  = true,
        Margin    = new Padding(0, 0, 0, 8)
    };

    public static Label FieldLabel(string text) => new()
    {
        Text      = text,
        Font      = MainTheme.Small,
        ForeColor = MainTheme.TextSecondary,
        AutoSize  = true,
        Margin    = new Padding(0, 0, 0, 2)
    };

    public static Panel KeyValueRow(string key, string value)
    {
        var row = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 32,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 4, 0, 4)
        };

        var keyLbl = new Label
        {
            Text      = key,
            Font      = MainTheme.Small,
            ForeColor = MainTheme.TextSecondary,
            Width     = 140,
            Dock      = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var valueLbl = new Label
        {
            Text      = value,
            Font      = MainTheme.Body,
            ForeColor = MainTheme.TextPrimary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        row.Controls.Add(valueLbl);
        row.Controls.Add(keyLbl);
        return row;
    }

    public static DataGridView NiceGrid()
    {
        var grid = new DataGridView
        {
            BackgroundColor          = MainTheme.Surface,
            BorderStyle              = BorderStyle.None,
            RowHeadersVisible        = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight      = 36,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            GridColor                = MainTheme.Divider,
            CellBorderStyle          = DataGridViewCellBorderStyle.SingleHorizontal,
            AutoSizeColumnsMode      = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows       = false,
            AllowUserToDeleteRows    = false,
            AllowUserToResizeRows    = false,
            MultiSelect              = false,
            ReadOnly                 = true,
            SelectionMode            = DataGridViewSelectionMode.FullRowSelect,
            Font                     = MainTheme.Body
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = MainTheme.Primary;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font      = MainTheme.BodyBold;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = MainTheme.Primary;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Padding  = new Padding(8, 0, 8, 0);

        grid.RowsDefaultCellStyle.BackColor          = MainTheme.Surface;
        grid.RowsDefaultCellStyle.ForeColor          = MainTheme.TextPrimary;
        grid.RowsDefaultCellStyle.SelectionBackColor = MainTheme.PrimaryLight;
        grid.RowsDefaultCellStyle.SelectionForeColor = Color.White;

        grid.AlternatingRowsDefaultCellStyle.BackColor = MainTheme.SurfaceAlt;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = MainTheme.PrimaryLight;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

        grid.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        grid.RowTemplate.Height       = 32;

        return grid;
    }
}
