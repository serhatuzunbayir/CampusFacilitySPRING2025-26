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
    private readonly Button   _btnCancel;
    private readonly Label    _lblError;

    public bool FacilityCreated { get; private set; }

    public AddFacilityForm(ApiClient api)
    {
        _api = api;

        Text            = "Add Facility";
        Size            = new Size(540, 460);
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
            Text      = "Add Facility",
            Font      = MainTheme.Heading,
            ForeColor = Color.White,
            AutoSize  = false,
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

        var stack = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize    = false,
            BackColor   = Color.Transparent
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        // 4 fields × (label + input + hint) plus error + buttons
        for (int i = 0; i < 14; i++) stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        int row = 0;

        stack.Controls.Add(Styles.FieldLabel("Name"), 0, row++);
        _txtName = MakeInput();
        stack.Controls.Add(_txtName, 0, row++);
        stack.Controls.Add(Spacer(8), 0, row++);

        stack.Controls.Add(Styles.FieldLabel("Type"), 0, row++);
        _cmbType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock          = DockStyle.Top,
            Font          = MainTheme.Body,
            FlatStyle     = FlatStyle.Flat
        };
        stack.Controls.Add(_cmbType, 0, row++);
        stack.Controls.Add(Spacer(8), 0, row++);

        stack.Controls.Add(Styles.FieldLabel("Capacity"), 0, row++);
        _txtCapacity = MakeInput();
        _txtCapacity.Text = "30";
        stack.Controls.Add(_txtCapacity, 0, row++);
        stack.Controls.Add(Hint("e.g. 24"), 0, row++);

        stack.Controls.Add(Styles.FieldLabel("Location"), 0, row++);
        _txtLocation = MakeInput();
        stack.Controls.Add(_txtLocation, 0, row++);
        stack.Controls.Add(Hint("e.g. Engineering Building / 1st floor"), 0, row++);

        _lblError = new Label
        {
            ForeColor = MainTheme.Danger,
            Font      = MainTheme.Small,
            Dock      = DockStyle.Top,
            AutoSize  = false,
            Height    = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin    = new Padding(0, 8, 0, 0)
        };
        stack.Controls.Add(_lblError, 0, row++);

        card.Controls.Add(stack);

        var buttonBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 60,
            BackColor = MainTheme.SurfaceAlt,
            Padding   = new Padding(20, 12, 20, 12)
        };

        _btnSave = Styles.PrimaryButton("Create", 110);
        _btnSave.Dock = DockStyle.Right;
        _btnSave.Click += btnSave_Click;

        _btnCancel = Styles.SecondaryButton("Cancel", 100);
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Margin = new Padding(0, 0, 8, 0);
        _btnCancel.Click += (_, _) => Close();

        var spacer = new Panel { Dock = DockStyle.Right, Width = 8 };

        buttonBar.Controls.Add(_btnSave);
        buttonBar.Controls.Add(spacer);
        buttonBar.Controls.Add(_btnCancel);

        Controls.Add(card);
        Controls.Add(buttonBar);
        Controls.Add(titleBar);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

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

    private static TextBox MakeInput() => new()
    {
        Dock        = DockStyle.Top,
        Font        = MainTheme.Body,
        BorderStyle = BorderStyle.FixedSingle,
        Margin      = new Padding(0, 0, 0, 0)
    };

    private static Label Hint(string text) => new()
    {
        Text      = text,
        Font      = MainTheme.Small,
        ForeColor = MainTheme.TextMuted,
        AutoSize  = true,
        Margin    = new Padding(0, 2, 0, 0)
    };

    private static Panel Spacer(int height) => new()
    {
        Height    = height,
        Dock      = DockStyle.Top,
        BackColor = Color.Transparent
    };
}
