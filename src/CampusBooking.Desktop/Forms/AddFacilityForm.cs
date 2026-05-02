using CampusBooking.Desktop.Models;
using CampusBooking.Desktop.Services;

namespace CampusBooking.Desktop.Forms;

/// <summary>
/// Modal dialog that allows a FacilityManager to add a new facility.
/// Loads available facility types from the API and validates required fields.
/// </summary>
public class AddFacilityForm : Form
{
    private readonly ApiClient _api;

    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly TextBox  _txtName;
    private readonly ComboBox _cmbType;
    private readonly TextBox  _txtCapacity;
    private readonly TextBox  _txtLocation;
    private readonly Button   _btnSave;
    private readonly Label    _lblError;

    /// <summary>Set to true after the facility is saved so the caller can refresh.</summary>
    public bool FacilityCreated { get; private set; }

    public AddFacilityForm(ApiClient api)
    {
        _api = api;

        Text            = "Add Facility";
        Size            = new Size(400, 300);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 10f);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 6,
            Padding     = new Padding(20)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        // Name
        layout.Controls.Add(MakeLabel("Name:"), 0, 0);
        _txtName = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_txtName, 1, 0);

        // Type dropdown
        layout.Controls.Add(MakeLabel("Type:"), 0, 1);
        _cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        layout.Controls.Add(_cmbType, 1, 1);

        // Capacity
        layout.Controls.Add(MakeLabel("Capacity:"), 0, 2);
        _txtCapacity = new TextBox { Dock = DockStyle.Fill, Text = "30" };
        layout.Controls.Add(_txtCapacity, 1, 2);

        // Location
        layout.Controls.Add(MakeLabel("Location:"), 0, 3);
        _txtLocation = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_txtLocation, 1, 3);

        // Save button
        layout.Controls.Add(new Label(), 0, 4);
        _btnSave = new Button { Text = "Save", Dock = DockStyle.Fill };
        _btnSave.Click += BtnSave_ClickAsync;
        layout.Controls.Add(_btnSave, 1, 4);

        // Error
        _lblError = new Label { ForeColor = Color.Crimson, Dock = DockStyle.Fill };
        layout.SetColumnSpan(_lblError, 2);
        layout.Controls.Add(_lblError, 0, 5);

        Controls.Add(layout);
        AcceptButton = _btnSave;

        // Load facility types when the form opens
        Load += async (_, _) => await LoadTypesAsync();
    }

    // ── Methods ───────────────────────────────────────────────────────────────

    /// <summary>Fetches facility types from the API and populates the dropdown.</summary>
    private async Task LoadTypesAsync()
    {
        var types = await _api.GetFacilityTypesAsync();
        _cmbType.DataSource    = types;
        _cmbType.DisplayMember = "Name";
        _cmbType.ValueMember   = "Id";
    }

    /// <summary>Validates inputs and posts the new facility to the API.</summary>
    private async void BtnSave_ClickAsync(object? sender, EventArgs e)
    {
        _lblError.Text = string.Empty;

        // Basic client-side validation
        if (string.IsNullOrWhiteSpace(_txtName.Text))     { _lblError.Text = "Name is required.";     return; }
        if (_cmbType.SelectedItem is null)                 { _lblError.Text = "Select a type.";         return; }
        if (!int.TryParse(_txtCapacity.Text, out int cap)) { _lblError.Text = "Capacity must be a number."; return; }
        if (string.IsNullOrWhiteSpace(_txtLocation.Text)) { _lblError.Text = "Location is required."; return; }

        var selectedType = (FacilityTypeDto)_cmbType.SelectedItem;

        _btnSave.Enabled = false;
        var ok = await _api.CreateFacilityAsync(_txtName.Text.Trim(), selectedType.Id, cap, _txtLocation.Text.Trim());

        if (ok)
        {
            FacilityCreated = true;
            Close();
        }
        else
        {
            _lblError.Text   = "Failed to create facility. Check the API logs.";
            _btnSave.Enabled = true;
        }
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight
    };
}
