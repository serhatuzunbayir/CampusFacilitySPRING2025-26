using CampusBooking.Desktop.Models;
using CampusBooking.Desktop.Services;

namespace CampusBooking.Desktop.Forms;

/// <summary>
/// Modal dialog that lets a Student or Staff member create a new booking.
/// Steps: pick a date + time slot → search availability → select a facility → confirm.
/// </summary>
public class CreateBookingForm : Form
{
    private readonly ApiClient _api;

    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly DateTimePicker _dtpDate;
    private readonly ComboBox       _cmbTimeSlot;
    private readonly Button         _btnSearch;
    private readonly DataGridView   _gridFacilities;
    private readonly Button         _btnBook;
    private readonly Label          _lblStatus;

    /// <summary>Set to true after a successful booking so the caller can refresh its list.</summary>
    public bool BookingCreated { get; private set; }

    public CreateBookingForm(ApiClient api)
    {
        _api = api;

        Text            = "New Booking";
        Size            = new Size(600, 420);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 10f);

        // ── Top bar: date + slot + search ────────────────────────────────────
        var topPanel = new FlowLayoutPanel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            Padding   = new Padding(8, 8, 8, 4),
            FlowDirection = FlowDirection.LeftToRight
        };

        topPanel.Controls.Add(new Label { Text = "Date:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Width = 120 };
        topPanel.Controls.Add(_dtpDate);

        topPanel.Controls.Add(new Label { Text = "  Hour:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });

        // Time slots 08:00 – 19:00
        _cmbTimeSlot = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
        for (int h = 8; h <= 19; h++)
            _cmbTimeSlot.Items.Add($"{h:00}:00");
        _cmbTimeSlot.SelectedIndex = 0;
        topPanel.Controls.Add(_cmbTimeSlot);

        _btnSearch = new Button { Text = "Search", Width = 80, Margin = new Padding(8, 2, 0, 0) };
        _btnSearch.Click += BtnSearch_ClickAsync;
        topPanel.Controls.Add(_btnSearch);

        Controls.Add(topPanel);

        // ── Centre: available facilities grid ────────────────────────────────
        _gridFacilities = new DataGridView
        {
            Dock             = DockStyle.Fill,
            ReadOnly         = true,
            SelectionMode    = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect      = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        _gridFacilities.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",       HeaderText = "Id",       Visible = false });
        _gridFacilities.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",     HeaderText = "Facility"  });
        _gridFacilities.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",     HeaderText = "Type"      });
        _gridFacilities.Columns.Add(new DataGridViewTextBoxColumn { Name = "Capacity", HeaderText = "Capacity"  });
        _gridFacilities.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Location"  });
        _gridFacilities.Columns.Add(new DataGridViewTextBoxColumn { Name = "Approval", HeaderText = "Approval?" });
        Controls.Add(_gridFacilities);

        // ── Bottom bar: status + book button ─────────────────────────────────
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 44 };

        _lblStatus = new Label
        {
            Text      = "Select a date and time, then click Search.",
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0)
        };

        _btnBook = new Button
        {
            Text   = "Book Selected",
            Width  = 130,
            Height = 30,
            Dock   = DockStyle.Right,
            Enabled = false
        };
        _btnBook.Click += BtnBook_ClickAsync;

        bottomPanel.Controls.Add(_lblStatus);
        bottomPanel.Controls.Add(_btnBook);
        Controls.Add(bottomPanel);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    /// <summary>
    /// Queries the API for available facilities on the selected date and time slot.
    /// Uses LINQ (server-side via the API) to filter out already-booked slots.
    /// </summary>
    private async void BtnSearch_ClickAsync(object? sender, EventArgs e)
    {
        _btnSearch.Enabled = false;
        _btnBook.Enabled   = false;
        _gridFacilities.Rows.Clear();
        _lblStatus.Text    = "Searching…";

        try
        {
            var date     = DateOnly.FromDateTime(_dtpDate.Value);
            var timeSlot = 8 + _cmbTimeSlot.SelectedIndex; // slot 0 = hour 8

            // Call GET /api/bookings/availability – returns facilities with no conflict
            var facilities = await _api.GetAvailabilityAsync(date, timeSlot);

            if (facilities.Count == 0)
            {
                _lblStatus.Text = "No facilities available for that slot.";
                return;
            }

            // Populate the grid
            foreach (var f in facilities)
            {
                _gridFacilities.Rows.Add(
                    f.Id, f.Name, f.FacilityTypeName,
                    f.Capacity, f.Location,
                    f.RequiresApproval ? "Required" : "Auto");
            }

            _lblStatus.Text  = $"{facilities.Count} facility(ies) available.";
            _btnBook.Enabled = true;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _btnSearch.Enabled = true;
        }
    }

    /// <summary>Submits the booking for the selected facility and time slot.</summary>
    private async void BtnBook_ClickAsync(object? sender, EventArgs e)
    {
        if (_gridFacilities.CurrentRow is null) return;

        var facilityId   = (int)_gridFacilities.CurrentRow.Cells["Id"].Value;
        var facilityName = (string)_gridFacilities.CurrentRow.Cells["Name"].Value;
        var date         = DateOnly.FromDateTime(_dtpDate.Value);
        var timeSlot     = 8 + _cmbTimeSlot.SelectedIndex;

        var confirm = MessageBox.Show(
            $"Book {facilityName} on {date} at {timeSlot:00}:00?",
            "Confirm Booking", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _btnBook.Enabled = false;

        var (ok, error) = await _api.CreateBookingAsync(facilityId, date, [timeSlot]);

        if (ok)
        {
            BookingCreated = true;
            MessageBox.Show("Booking created successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else
        {
            _lblStatus.Text  = $"Failed: {error}";
            _btnBook.Enabled = true;
        }
    }
}
