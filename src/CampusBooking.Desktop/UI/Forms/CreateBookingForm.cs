using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Bookings;

namespace CampusBooking.Desktop.UI.Forms;

public class CreateBookingForm : Form
{
    private readonly ApiClient _api;

    private readonly DateTimePicker _dtpDate;
    private readonly ComboBox       _cmbTimeSlot;
    private readonly Button         _btnSearch;
    private readonly DataGridView   _gridFacilities;
    private readonly Button         _btnBook;
    private readonly Label          _lblStatus;

    public bool BookingCreated { get; private set; }

    public CreateBookingForm(ApiClient api)
    {
        _api = api;

        Text            = "New Booking";
        Size            = new Size(600, 420);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        Font            = MainTheme.BodyFont;
        BackColor       = MainTheme.Background;

        var topPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            Height        = 46,
            Padding       = new Padding(8, 8, 8, 4),
            FlowDirection = FlowDirection.LeftToRight
        };

        topPanel.Controls.Add(new Label { Text = "Date:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Width = 120 };
        topPanel.Controls.Add(_dtpDate);

        topPanel.Controls.Add(new Label { Text = "  Hour:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });

        _cmbTimeSlot = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
        for (int h = 8; h <= 19; h++)
            _cmbTimeSlot.Items.Add($"{h:00}:00");
        _cmbTimeSlot.SelectedIndex = 0;
        topPanel.Controls.Add(_cmbTimeSlot);

        _btnSearch = new Button
        {
            Text = "Search",
            Width = 80,
            Margin = new Padding(8, 2, 0, 0),
            BackColor = MainTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSearch.Click += btnSearch_Click;
        topPanel.Controls.Add(_btnSearch);

        Controls.Add(topPanel);

        _gridFacilities = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = false,
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
            Enabled = false,
            BackColor = MainTheme.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnBook.Click += btnBook_Click;

        bottomPanel.Controls.Add(_lblStatus);
        bottomPanel.Controls.Add(_btnBook);
        Controls.Add(bottomPanel);
    }

    private async void btnSearch_Click(object? sender, EventArgs e)
    {
        _btnSearch.Enabled = false;
        _btnBook.Enabled   = false;
        _gridFacilities.Rows.Clear();
        _lblStatus.Text    = "Searching...";

        try
        {
            var date     = DateOnly.FromDateTime(_dtpDate.Value);
            var timeSlot = 8 + _cmbTimeSlot.SelectedIndex;

            var facilities = await _api.GetAvailabilityAsync(date, timeSlot);

            if (facilities.Count == 0)
            {
                _lblStatus.Text = "No facilities available for that slot.";
                return;
            }

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

    private async void btnBook_Click(object? sender, EventArgs e)
    {
        if (_gridFacilities.CurrentRow is null) return;

        var facilityId   = (int)_gridFacilities.CurrentRow.Cells["Id"].Value;
        var facilityName = (string)_gridFacilities.CurrentRow.Cells["Name"].Value;
        var date         = DateOnly.FromDateTime(_dtpDate.Value);
        var timeSlot     = 8 + _cmbTimeSlot.SelectedIndex;

        var confirm = MessageBox.Show(
            $"Book {facilityName} on {date} at {timeSlot:00}:00?",
            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _btnBook.Enabled = false;

        try
        {
            await _api.CreateBookingAsync(new CreateBookingRequest
            {
                FacilityId = facilityId,
                Date = date,
                TimeSlots = new List<int> { timeSlot }
            });
            BookingCreated = true;
            MessageBox.Show("Booking created.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (ApiException ex)
        {
            _lblStatus.Text = ex.Message;
            _btnBook.Enabled = true;
        }
    }
}
