using CampusBooking.Desktop.UI.Components;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Facilities;

namespace CampusBooking.Desktop.UI.Forms.Details;

public class FacilityDetailsForm : Form
{
    public FacilityDetailsForm(FacilityResponse facility)
    {
        Text            = $"Facility #{facility.Id}";
        Size            = new Size(560, 440);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        BackColor       = MainTheme.Background;
        Font            = MainTheme.Body;

        var root = DetailLayout.Root();
        root.Controls.Add(DetailLayout.Header("Facility Details"), 0, 0);
        root.Controls.Add(BuildContent(facility), 0, 1);
        root.Controls.Add(DetailLayout.Footer(this, out var btnClose), 0, 2);
        Controls.Add(root);

        AcceptButton = btnClose;
        CancelButton = btnClose;
    }

    private static Panel BuildContent(FacilityResponse f)
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
            RowCount    = 7,
            BackColor   = Color.Transparent,
            AutoSize    = false
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 6; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        DetailLayout.AddRow(grid, 0, "ID", f.Id.ToString(), MainTheme.Mono);
        DetailLayout.AddRow(grid, 1, "Name", f.Name);
        DetailLayout.AddRow(grid, 2, "Type", f.FacilityTypeName);
        DetailLayout.AddRow(grid, 3, "Capacity", f.Capacity.ToString());
        DetailLayout.AddRow(grid, 4, "Location", f.Location);
        DetailLayout.AddRow(grid, 5, "Approval", f.RequiresApproval ? "Required" : "Auto-confirm");
        DetailLayout.AddBadgeRow(grid, 6, "Active", f.IsActive ? "Yes" : "No", f.IsActive ? "Confirmed" : "Cancelled");

        card.Controls.Add(grid);
        return card;
    }
}
