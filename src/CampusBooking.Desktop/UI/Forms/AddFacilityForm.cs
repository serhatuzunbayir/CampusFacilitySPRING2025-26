using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Facilities;

namespace CampusBooking.Desktop.UI.Forms;

public class AddFacilityForm : Form
{
    private readonly ApiClient _api;

    private readonly TextBox  _txtName;
    private readonly ComboBox _cmbType;
    private readonly TextBox  _txtCapacity;
    private readonly TextBox  _txtLocation;
    private readonly Button   _btnSave;
    private readonly Label    _lblError;

    public bool FacilityCreated { get; private set; }

    public AddFacilityForm(ApiClient api)
    {
        _api = api;

        Text            = "Add Facility";
        Size            = new Size(400, 300);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        Font            = MainTheme.BodyFont;
        BackColor       = MainTheme.Background;

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

        layout.Controls.Add(MakeLabel("Name:"), 0, 0);
        _txtName = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_txtName, 1, 0);

        layout.Controls.Add(MakeLabel("Type:"), 0, 1);
        _cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        layout.Controls.Add(_cmbType, 1, 1);

        layout.Controls.Add(MakeLabel("Capacity:"), 0, 2);
        _txtCapacity = new TextBox { Dock = DockStyle.Fill, Text = "30" };
        layout.Controls.Add(_txtCapacity, 1, 2);

        layout.Controls.Add(MakeLabel("Location:"), 0, 3);
        _txtLocation = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_txtLocation, 1, 3);

        layout.Controls.Add(new Label(), 0, 4);
        _btnSave = new Button
        {
            Text = "Save",
            Dock = DockStyle.Fill,
            BackColor = MainTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += btnSave_Click;
        layout.Controls.Add(_btnSave, 1, 4);

        _lblError = new Label { ForeColor = MainTheme.Danger, Dock = DockStyle.Fill };
        layout.SetColumnSpan(_lblError, 2);
        layout.Controls.Add(_lblError, 0, 5);

        Controls.Add(layout);
        AcceptButton = _btnSave;

        Load += async (_, _) => await LoadTypesAsync();
    }

    private async Task LoadTypesAsync()
    {
        try
        {
            var types = await _api.GetFacilityTypesAsync();
            _cmbType.DataSource    = types;
            _cmbType.DisplayMember = "Name";
            _cmbType.ValueMember   = "Id";
        }
        catch (Exception ex)
        {
            _lblError.Text = $"Could not load types: {ex.Message}";
        }
    }

    private async void btnSave_Click(object? sender, EventArgs e)
    {
        _lblError.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(_txtName.Text))     { _lblError.Text = "Name is required.";          return; }
        if (_cmbType.SelectedItem is null)                { _lblError.Text = "Select a type.";             return; }
        if (!int.TryParse(_txtCapacity.Text, out int cap)){ _lblError.Text = "Capacity must be a number."; return; }
        if (string.IsNullOrWhiteSpace(_txtLocation.Text)) { _lblError.Text = "Location is required.";      return; }

        var selectedType = (FacilityTypeResponse)_cmbType.SelectedItem;

        _btnSave.Enabled = false;
        try
        {
            await _api.CreateFacilityAsync(new CreateFacilityRequest
            {
                Name = _txtName.Text.Trim(),
                FacilityTypeId = selectedType.Id,
                Capacity = cap,
                Location = _txtLocation.Text.Trim()
            });
            FacilityCreated = true;
            Close();
        }
        catch (ApiException ex)
        {
            _lblError.Text = ex.Message;
            _btnSave.Enabled = true;
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
            _btnSave.Enabled = true;
        }
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight
    };
}
