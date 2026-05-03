using CampusBooking.Desktop.UI.Components;
using CampusBooking.Desktop.UI.Themes;

namespace CampusBooking.Desktop.UI.Forms.Details;

internal static class DetailLayout
{
    // Root layout: title bar (64px), content card (fills), footer (60px). TableLayoutPanel
    // gives predictable docking — Dock=Fill on a regular Panel gets confused by sibling z-order
    // when controls are added in the wrong sequence, which was the layout bug in the prior version.
    public static TableLayoutPanel Root()
    {
        var t = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3,
            BackColor   = Color.Transparent
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        return t;
    }

    public static Panel Header(string title)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.Primary };
        p.Controls.Add(new Label
        {
            Text      = title,
            Dock      = DockStyle.Fill,
            Font      = MainTheme.Heading,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(24, 0, 0, 0)
        });
        return p;
    }

    public static Panel Footer(Form owner, out Button closeButton)
    {
        var p = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MainTheme.Surface,
            Padding   = new Padding(24, 12, 24, 12)
        };
        var btn = Styles.PrimaryButton("Close", 110);
        btn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        btn.Location = new Point(p.Width - 24 - btn.Width, 12);
        btn.Click += (_, _) => owner.Close();
        // Re-anchor on resize so the button stays in the bottom-right corner regardless of DPI.
        p.Resize += (_, _) => btn.Location = new Point(p.ClientSize.Width - 24 - btn.Width, 12);
        p.Controls.Add(btn);
        closeButton = btn;
        return p;
    }

    public static void AddRow(TableLayoutPanel grid, int row, string key, string value, Font? valueFont = null)
    {
        grid.Controls.Add(KeyLabel(key), 0, row);
        grid.Controls.Add(ValueLabel(value, valueFont), 1, row);
    }

    public static void AddBadgeRow(TableLayoutPanel grid, int row, string key, string value, string badgeKind)
    {
        grid.Controls.Add(KeyLabel(key), 0, row);

        var holder = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        var badge = StatusBadge.For(badgeKind);
        badge.Text     = value;
        badge.Width    = Math.Max(80, TextRenderer.MeasureText(value, badge.Font).Width + 24);
        badge.Location = new Point(0, 6);
        holder.Controls.Add(badge);
        grid.Controls.Add(holder, 1, row);
    }

    private static Label KeyLabel(string text) => new()
    {
        Text      = text,
        Font      = MainTheme.Small,
        ForeColor = MainTheme.TextSecondary,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding   = new Padding(0, 0, 8, 0)
    };

    private static Label ValueLabel(string text, Font? font = null) => new()
    {
        Text         = text,
        Font         = font ?? MainTheme.Body,
        ForeColor    = MainTheme.TextPrimary,
        Dock         = DockStyle.Fill,
        TextAlign    = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
}
