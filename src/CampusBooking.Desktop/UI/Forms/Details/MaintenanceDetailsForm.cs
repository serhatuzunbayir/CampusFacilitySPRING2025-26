using CampusBooking.Desktop.UI.Components;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Maintenance;

namespace CampusBooking.Desktop.UI.Forms.Details;

public class MaintenanceDetailsForm : Form
{
    public MaintenanceDetailsForm(MaintenanceIssueResponse issue)
    {
        Text            = $"Maintenance Issue #{issue.Id}";
        Size            = new Size(680, 680);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        BackColor       = MainTheme.Background;
        Font            = MainTheme.Body;

        var root = DetailLayout.Root();
        root.Controls.Add(DetailLayout.Header("Maintenance Issue"), 0, 0);
        root.Controls.Add(BuildContent(issue), 0, 1);
        root.Controls.Add(DetailLayout.Footer(this, out var btnClose), 0, 2);
        Controls.Add(root);

        AcceptButton = btnClose;
        CancelButton = btnClose;
    }

    private static Panel BuildContent(MaintenanceIssueResponse i)
    {
        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MainTheme.Surface,
            Padding   = new Padding(28, 22, 28, 22)
        };

        var rows = new List<(string Key, string Value, string? Badge)>
        {
            ("ID",       i.Id.ToString(),                 null),
            ("Facility", i.FacilityName,                  null),
            ("Reporter", i.ReporterName,                  null),
            ("Severity", i.Severity.ToString(),           i.Severity.ToString()),
            ("Status",   i.Status.ToString(),             i.Status.ToString()),
            ("Created",  i.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"), null)
        };
        if (i.AssignedAtUtc.HasValue)
            rows.Add(("Assigned", i.AssignedAtUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm"), null));
        if (!string.IsNullOrEmpty(i.AssigneeName))
            rows.Add(("Assignee", i.AssigneeName!, null));
        if (i.StartedAtUtc.HasValue)
            rows.Add(("Started", i.StartedAtUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm"), null));
        if (i.ResolvedAtUtc.HasValue)
            rows.Add(("Resolved", i.ResolvedAtUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm"), null));

        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = rows.Count + 2,
            BackColor   = Color.Transparent,
            AutoSize    = false
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int r = 0; r < rows.Count; r++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        for (int r = 0; r < rows.Count; r++)
        {
            var (k, v, badge) = rows[r];
            if (badge != null) DetailLayout.AddBadgeRow(grid, r, k, v, badge);
            else if (k == "ID") DetailLayout.AddRow(grid, r, k, v, MainTheme.Mono);
            else DetailLayout.AddRow(grid, r, k, v);
        }

        // Description section header
        var descTitle = new Label
        {
            Text      = "Description",
            Font      = MainTheme.Subheading,
            ForeColor = MainTheme.TextPrimary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin    = new Padding(0, 8, 0, 4)
        };
        grid.Controls.Add(descTitle, 0, rows.Count);
        grid.SetColumnSpan(descTitle, 2);

        // Description text (wraps automatically when AutoSize is false)
        var descBody = new Label
        {
            Text      = string.IsNullOrWhiteSpace(i.Description) ? "(no description)" : i.Description,
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            Font      = MainTheme.Body,
            ForeColor = MainTheme.TextPrimary,
            BackColor = MainTheme.SurfaceAlt,
            Padding   = new Padding(12, 10, 12, 10),
            TextAlign = ContentAlignment.TopLeft
        };
        grid.Controls.Add(descBody, 0, rows.Count + 1);
        grid.SetColumnSpan(descBody, 2);

        card.Controls.Add(grid);
        return card;
    }
}
