using CampusBooking.Desktop.UI.Components;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Bookings;

namespace CampusBooking.Desktop.UI.Forms.Details;

public class BookingDetailsForm : Form
{
    public BookingDetailsForm(BookingResponse booking)
    {
        Text            = $"Booking #{booking.Id}";
        Size            = new Size(560, 480);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        BackColor       = MainTheme.Background;
        Font            = MainTheme.Body;

        var root = DetailLayout.Root();
        root.Controls.Add(DetailLayout.Header("Booking Details"), 0, 0);
        root.Controls.Add(BuildContent(booking), 0, 1);
        root.Controls.Add(DetailLayout.Footer(this, out var btnClose), 0, 2);
        Controls.Add(root);

        AcceptButton = btnClose;
        CancelButton = btnClose;
    }

    private static Panel BuildContent(BookingResponse b)
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
            RowCount    = 8,
            BackColor   = Color.Transparent,
            AutoSize    = false
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 7; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        DetailLayout.AddRow(grid, 0, "ID", b.Id.ToString(), MainTheme.Mono);
        DetailLayout.AddRow(grid, 1, "Facility", b.FacilityName);
        DetailLayout.AddRow(grid, 2, "User", b.UserDisplayName);
        DetailLayout.AddRow(grid, 3, "Date", b.Date.ToString("dddd, dd MMM yyyy"));
        DetailLayout.AddRow(grid, 4, "Time", FormatSlot(b.TimeSlot));
        DetailLayout.AddBadgeRow(grid, 5, "Status", b.Status.ToString(), b.Status.ToString());
        DetailLayout.AddRow(grid, 6, "Created", b.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"));

        card.Controls.Add(grid);
        return card;
    }

    private static string FormatSlot(int slot)
    {
        var start = slot.ToString("00") + ":00";
        var end = ((slot + 1) % 24).ToString("00") + ":00";
        return $"{start} – {end} (1 hour)";
    }
}
