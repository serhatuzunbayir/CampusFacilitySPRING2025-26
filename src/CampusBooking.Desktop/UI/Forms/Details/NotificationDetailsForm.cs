using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Components;
using CampusBooking.Desktop.UI.Themes;

namespace CampusBooking.Desktop.UI.Forms.Details;

public class NotificationDetailsForm : Form
{
    public NotificationDetailsForm(NotificationItem item)
    {
        Text            = $"Notification #{item.Id}";
        Size            = new Size(560, 480);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        BackColor       = MainTheme.Background;
        Font            = MainTheme.Body;

        var root = DetailLayout.Root();
        root.Controls.Add(DetailLayout.Header("Notification"), 0, 0);
        root.Controls.Add(BuildContent(item), 0, 1);
        root.Controls.Add(DetailLayout.Footer(this, out var btnClose), 0, 2);
        Controls.Add(root);

        AcceptButton = btnClose;
        CancelButton = btnClose;
    }

    private static Panel BuildContent(NotificationItem n)
    {
        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MainTheme.Surface,
            Padding   = new Padding(28, 22, 28, 22)
        };

        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 6,
            BackColor   = Color.Transparent,
            AutoSize    = false
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        DetailLayout.AddRow(grid, 0, "ID", n.Id.ToString(), MainTheme.Mono);
        DetailLayout.AddBadgeRow(grid, 1, "Kind", n.Kind, n.Kind);
        DetailLayout.AddRow(grid, 2, "Created", n.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"));
        DetailLayout.AddRow(grid, 3, "Read", n.IsRead ? "Yes" : "No");

        var msgTitle = new Label
        {
            Text      = "Message",
            Font      = MainTheme.Subheading,
            ForeColor = MainTheme.TextPrimary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin    = new Padding(0, 8, 0, 4)
        };
        grid.Controls.Add(msgTitle, 0, 4);
        grid.SetColumnSpan(msgTitle, 2);

        // Message body fills the rest of the card; Label with AutoSize=false wraps text.
        var msgBody = new Label
        {
            Text      = n.Message,
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            Font      = MainTheme.Body,
            ForeColor = MainTheme.TextPrimary,
            BackColor = MainTheme.SurfaceAlt,
            Padding   = new Padding(14, 12, 14, 12),
            TextAlign = ContentAlignment.TopLeft
        };
        grid.Controls.Add(msgBody, 0, 5);
        grid.SetColumnSpan(msgBody, 2);

        card.Controls.Add(grid);
        return card;
    }
}
