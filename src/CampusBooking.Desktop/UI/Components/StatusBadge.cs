using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CampusBooking.Desktop.UI.Themes;

namespace CampusBooking.Desktop.UI.Components;

public static class StatusBadge
{
    public static Label For(string status)
    {
        var (back, fore) = MapColors(status);

        var lbl = new Label
        {
            Text         = status,
            AutoSize     = false,
            Width        = 80,
            Height       = 22,
            TextAlign    = ContentAlignment.MiddleCenter,
            Font         = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor    = fore,
            BackColor    = Color.Transparent,
            Padding      = new Padding(6, 0, 6, 0),
            Margin       = new Padding(0)
        };

        // Round corners + soft fill drawn manually so it looks like a pill rather than a flat label.
        lbl.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1);
            int radius = lbl.Height;
            using var path = RoundedPath(rect, radius);
            using var brush = new SolidBrush(back);
            g.FillPath(brush, path);
            TextRenderer.DrawText(g, lbl.Text, lbl.Font, rect, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        };

        return lbl;
    }

    private static (Color back, Color fore) MapColors(string status) => status switch
    {
        "Confirmed" or "Approved" or "Resolved" => (MainTheme.SuccessSoft, MainTheme.Success),
        "Pending" or "High"                     => (MainTheme.WarningSoft, MainTheme.Warning),
        "Cancelled" or "Rejected" or "Low"      => (MainTheme.NeutralSoft, MainTheme.Neutral),
        "Open" or "InProgress" or "Medium"      => (MainTheme.InfoSoft, MainTheme.Info),
        "Critical"                              => (MainTheme.DangerSoft, MainTheme.Danger),
        _                                       => (MainTheme.NeutralSoft, MainTheme.Neutral)
    };

    private static GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        int d = Math.Min(radius, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
