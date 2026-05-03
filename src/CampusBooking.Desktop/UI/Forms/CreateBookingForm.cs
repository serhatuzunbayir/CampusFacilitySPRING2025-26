using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Bookings;
using CampusBooking.Shared.Dtos.Facilities;

namespace CampusBooking.Desktop.UI.Forms;

public class CreateBookingForm : Form
{
    private readonly ApiClient _api;

    private readonly DateTimePicker _dtpDate;
    private readonly ComboBox       _cmbFacility;
    private readonly FlowLayoutPanel _slotRow;
    private readonly Label          _lblIndicator;
    private readonly Label          _lblValidation;
    private readonly Button         _btnCreate;
    private readonly Button         _btnCancel;

    private readonly Dictionary<int, Button> _slotButtons = new();
    private readonly HashSet<int> _selected = new();
    private readonly Dictionary<int, bool?> _availability = new();

    private List<FacilityResponse> _facilities = new();
    private CancellationTokenSource? _availabilityCts;

    public bool BookingCreated { get; private set; }

    public CreateBookingForm(ApiClient api)
    {
        _api = api;

        Text            = "New Booking";
        Size            = new Size(700, 540);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = MainTheme.Body;
        BackColor       = MainTheme.Background;

        var titleBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 56,
            BackColor = MainTheme.Primary
        };
        titleBar.Controls.Add(new Label
        {
            Text      = "New Booking",
            Font      = MainTheme.Heading,
            ForeColor = Color.White,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(20, 0, 0, 0)
        });

        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MainTheme.Surface,
            Padding   = new Padding(24)
        };

        var pickRow = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            Height      = 70,
            ColumnCount = 2,
            RowCount    = 2,
            BackColor   = Color.Transparent
        };
        pickRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        pickRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pickRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        pickRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        pickRow.Controls.Add(Styles.FieldLabel("Date"), 0, 0);
        pickRow.Controls.Add(Styles.FieldLabel("Facility"), 1, 0);

        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value  = DateTime.Today.AddDays(1),
            Width  = 180,
            Dock   = DockStyle.Top,
            Font   = MainTheme.Body
        };
        _dtpDate.ValueChanged += (_, _) => OnInputsChanged();
        pickRow.Controls.Add(_dtpDate, 0, 1);

        _cmbFacility = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock          = DockStyle.Top,
            Font          = MainTheme.Body,
            FlatStyle     = FlatStyle.Flat,
            DisplayMember = nameof(FacilityResponse.Name),
            ValueMember   = nameof(FacilityResponse.Id)
        };
        _cmbFacility.SelectedIndexChanged += (_, _) => OnInputsChanged();
        pickRow.Controls.Add(_cmbFacility, 1, 1);

        var slotsLabel = Styles.FieldLabel("Time slots");
        slotsLabel.Margin = new Padding(0, 12, 0, 4);
        slotsLabel.Dock = DockStyle.Top;

        var slotsArea = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 110,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 4, 0, 0)
        };

        _slotRow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = true,
            BackColor     = Color.Transparent,
            AutoScroll    = false
        };
        for (int h = 8; h <= 19; h++)
        {
            var hour = h;
            var btn = MakeSlotButton(hour);
            btn.Click += (_, _) => ToggleSlot(hour);
            _slotButtons[hour] = btn;
            _slotRow.Controls.Add(btn);
        }
        slotsArea.Controls.Add(_slotRow);

        _lblIndicator = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            Font      = MainTheme.Small,
            ForeColor = MainTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(2, 0, 0, 0),
            Text      = "Pick a date and facility to see availability."
        };

        var legend = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 22,
            Font      = MainTheme.Small,
            ForeColor = MainTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(2, 0, 0, 0),
            Text      = "Tap a slot to toggle. Selected slots are amber, booked slots are red."
        };

        _lblValidation = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 22,
            Font      = MainTheme.Small,
            ForeColor = MainTheme.Danger,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(2, 0, 0, 0)
        };

        // Stacking order: pickRow on top, then slots area, then indicator/legend/validation at bottom of card.
        card.Controls.Add(_lblValidation);
        card.Controls.Add(legend);
        card.Controls.Add(_lblIndicator);
        card.Controls.Add(slotsArea);
        card.Controls.Add(slotsLabel);
        card.Controls.Add(pickRow);

        var buttonBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 60,
            BackColor = MainTheme.SurfaceAlt,
            Padding   = new Padding(20, 12, 20, 12)
        };

        _btnCreate = Styles.PrimaryButton("Create", 110);
        _btnCreate.Dock = DockStyle.Right;
        _btnCreate.Click += btnCreate_Click;

        _btnCancel = Styles.SecondaryButton("Cancel", 100);
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Margin = new Padding(0, 0, 8, 0);
        _btnCancel.Click += (_, _) => Close();

        var sp = new Panel { Dock = DockStyle.Right, Width = 8 };

        buttonBar.Controls.Add(_btnCreate);
        buttonBar.Controls.Add(sp);
        buttonBar.Controls.Add(_btnCancel);

        Controls.Add(card);
        Controls.Add(buttonBar);
        Controls.Add(titleBar);

        AcceptButton = _btnCreate;
        CancelButton = _btnCancel;

        Load += async (_, _) => await LoadFacilitiesAsync();
    }

    private Button MakeSlotButton(int hour)
    {
        var btn = Styles.SecondaryButton($"{hour:00}:00", 70);
        btn.Height = 38;
        btn.Margin = new Padding(0, 0, 6, 6);
        btn.Tag = hour;
        return btn;
    }

    private void ToggleSlot(int hour)
    {
        if (_selected.Contains(hour)) _selected.Remove(hour);
        else _selected.Add(hour);
        ApplySlotStyles();
    }

    private void ApplySlotStyles()
    {
        foreach (var (hour, btn) in _slotButtons)
        {
            var selected = _selected.Contains(hour);
            var avail    = _availability.TryGetValue(hour, out var a) ? a : null;

            if (selected)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = MainTheme.Accent;
                btn.ForeColor = MainTheme.TextPrimary;
                btn.FlatAppearance.BorderSize  = 0;
                btn.FlatAppearance.MouseOverBackColor = MainTheme.AccentDark;
                btn.FlatAppearance.MouseDownBackColor = MainTheme.AccentDark;
            }
            else if (avail == false)
            {
                btn.BackColor = MainTheme.DangerSoft;
                btn.ForeColor = MainTheme.Danger;
                btn.FlatAppearance.BorderSize  = 1;
                btn.FlatAppearance.BorderColor = MainTheme.Danger;
                btn.FlatAppearance.MouseOverBackColor = MainTheme.DangerSoft;
            }
            else if (avail == true)
            {
                btn.BackColor = MainTheme.Surface;
                btn.ForeColor = MainTheme.Success;
                btn.FlatAppearance.BorderSize  = 1;
                btn.FlatAppearance.BorderColor = MainTheme.Success;
                btn.FlatAppearance.MouseOverBackColor = MainTheme.SuccessSoft;
            }
            else
            {
                btn.BackColor = MainTheme.Surface;
                btn.ForeColor = MainTheme.Primary;
                btn.FlatAppearance.BorderSize  = 1;
                btn.FlatAppearance.BorderColor = MainTheme.PrimaryLight;
                btn.FlatAppearance.MouseOverBackColor = MainTheme.SurfaceAlt;
            }
        }
    }

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            _facilities = await _api.GetFacilitiesAsync();
            _cmbFacility.DataSource = _facilities;
            if (_facilities.Count > 0) _cmbFacility.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _lblValidation.Text = $"Could not load facilities: {ex.Message}";
        }
    }

    private void OnInputsChanged()
    {
        // any selected slots become invalid when context changes; clear them and refresh availability
        _selected.Clear();
        _availability.Clear();
        ApplySlotStyles();
        _ = RefreshAvailabilityAsync();
    }

    private async Task RefreshAvailabilityAsync()
    {
        if (_cmbFacility.SelectedItem is not FacilityResponse facility)
        {
            _lblIndicator.Text = "Pick a facility.";
            return;
        }

        // cancel previous in-flight refresh so older results don't overwrite newer ones
        _availabilityCts?.Cancel();
        var cts = new CancellationTokenSource();
        _availabilityCts = cts;

        var date = DateOnly.FromDateTime(_dtpDate.Value);
        _lblIndicator.Text = "Checking availability...";

        try
        {
            for (int h = 8; h <= 19; h++)
            {
                if (cts.IsCancellationRequested) return;
                var list = await _api.GetAvailabilityAsync(date, h, facilityTypeId: null);
                _availability[h] = list.Any(f => f.Id == facility.Id);
                ApplySlotStyles();
            }

            if (cts.IsCancellationRequested) return;

            var openCount = _availability.Count(kv => kv.Value == true);
            _lblIndicator.Text = openCount == 0
                ? $"No open slots for {facility.Name} on {date:d}."
                : $"{openCount} open slot(s) for {facility.Name} on {date:d}.";
        }
        catch (Exception ex)
        {
            if (!cts.IsCancellationRequested)
                _lblIndicator.Text = $"Availability check failed: {ex.Message}";
        }
    }

    private async void btnCreate_Click(object? sender, EventArgs e)
    {
        _lblValidation.Text = string.Empty;

        if (_cmbFacility.SelectedItem is not FacilityResponse facility)
        {
            _lblValidation.Text = "Pick a facility.";
            return;
        }
        if (_selected.Count == 0)
        {
            _lblValidation.Text = "Select at least one time slot.";
            return;
        }

        var date = DateOnly.FromDateTime(_dtpDate.Value);
        var slots = _selected.OrderBy(x => x).ToList();

        _btnCreate.Enabled = false;
        try
        {
            await _api.CreateBookingAsync(new CreateBookingRequest
            {
                FacilityId = facility.Id,
                Date       = date,
                TimeSlots  = slots
            });
            BookingCreated = true;
            Close();
        }
        catch (ApiException ex)
        {
            _lblValidation.Text = ex.Message;
            _btnCreate.Enabled = true;
        }
        catch (Exception ex)
        {
            _lblValidation.Text = ex.Message;
            _btnCreate.Enabled = true;
        }
    }
}
