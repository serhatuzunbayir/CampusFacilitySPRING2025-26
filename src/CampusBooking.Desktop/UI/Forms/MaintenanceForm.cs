using System.Text;
using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Maintenance;
using CampusBooking.Shared.Dtos.Users;
using CampusBooking.Shared.Enums;

namespace CampusBooking.Desktop.UI.Forms;

public class MaintenanceForm : Form
{
    private readonly ApiClient _api;
    private readonly UserSession _session;
    private readonly IServiceProvider _services;

    private readonly TabControl _tabs;

    private readonly TabPage? _tabOpen;
    private readonly DataGridView? _gridOpen;
    private readonly Button? _btnRefreshOpen;
    private readonly Button? _btnAssign;

    private readonly TabPage? _tabMine;
    private readonly DataGridView? _gridMine;
    private readonly Button? _btnRefreshMine;
    private readonly Button? _btnStart;
    private readonly Button? _btnResolve;

    private readonly TabPage? _tabLog;
    private readonly DataGridView? _gridLog;
    private readonly ComboBox? _cmbLogType;
    private readonly ComboBox? _cmbLogStatus;
    private readonly DateTimePicker? _dtpLogFrom;
    private readonly DateTimePicker? _dtpLogTo;
    private readonly Button? _btnApplyFilters;
    private readonly Button? _btnExportCsv;

    private List<MaintenanceIssueResponse> _logCache = new();
    private Dictionary<int, string> _facilityTypeByFacilityId = new();

    public MaintenanceForm(ApiClient api, UserSession session, IServiceProvider services)
    {
        _api = api;
        _session = session;
        _services = services;

        Text            = "Maintenance";
        Size            = new Size(1000, 600);
        StartPosition   = FormStartPosition.CenterParent;
        Font            = MainTheme.BodyFont;
        BackColor       = MainTheme.Background;
        MinimizeBox     = false;

        _tabs = new TabControl { Dock = DockStyle.Fill };

        if (_session.IsManager)
        {
            _tabOpen = new TabPage("Open Issues");
            _gridOpen = MakeGrid(
                ("Id",        "Id",          false),
                ("Facility",  "Facility",    true),
                ("Reporter",  "Reporter",    true),
                ("Severity",  "Severity",    true),
                ("Description", "Description", true),
                ("CreatedAt", "Created",     true));
            _btnRefreshOpen = MakeButton("Refresh", 90);
            _btnAssign      = MakeButton("Assign...", 110);
            _btnRefreshOpen.Click += async (_, _) => await LoadOpenAsync();
            _btnAssign.Click      += btnAssign_Click;
            _tabOpen.Controls.Add(BuildTabLayout(_gridOpen, _btnRefreshOpen, _btnAssign));
            _tabs.TabPages.Add(_tabOpen);
        }

        if (_session.Role == nameof(UserRole.MaintenancePersonnel))
        {
            _tabMine = new TabPage("My Tasks");
            _gridMine = MakeGrid(
                ("Id",          "Id",          false),
                ("Facility",    "Facility",    true),
                ("Severity",    "Severity",    true),
                ("Description", "Description", true),
                ("Status",      "Status",      true),
                ("AssignedAt",  "Assigned",    true));
            _btnRefreshMine = MakeButton("Refresh", 90);
            _btnStart       = MakeButton("Start", 80);
            _btnResolve     = MakeButton("Resolve", 90);
            _btnRefreshMine.Click += async (_, _) => await LoadMineAsync();
            _btnStart.Click       += async (_, _) => await UpdateMyStatusAsync(MaintenanceStatus.InProgress);
            _btnResolve.Click     += async (_, _) => await UpdateMyStatusAsync(MaintenanceStatus.Resolved);
            _tabMine.Controls.Add(BuildTabLayout(_gridMine, _btnRefreshMine, _btnStart, _btnResolve));
            _tabs.TabPages.Add(_tabMine);
        }

        if (_session.IsManager)
        {
            _tabLog = new TabPage("Log + Export");
            _gridLog = MakeGrid(
                ("Id",          "Id",          false),
                ("Facility",    "Facility",    true),
                ("Type",        "Type",        true),
                ("Reporter",    "Reporter",    true),
                ("Assignee",    "Assignee",    true),
                ("Severity",    "Severity",    true),
                ("Status",      "Status",      true),
                ("Description", "Description", true),
                ("CreatedAt",   "Created",     true),
                ("ResolvedAt",  "Resolved",    true));

            var filterPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 44,
                Padding       = new Padding(6, 6, 6, 4),
                FlowDirection = FlowDirection.LeftToRight
            };

            filterPanel.Controls.Add(new Label { Text = "Type:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            _cmbLogType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
            _cmbLogType.Items.Add("(any)");
            _cmbLogType.SelectedIndex = 0;
            filterPanel.Controls.Add(_cmbLogType);

            filterPanel.Controls.Add(new Label { Text = "  Status:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            _cmbLogStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
            _cmbLogStatus.Items.Add("(any)");
            foreach (var s in Enum.GetNames<MaintenanceStatus>())
                _cmbLogStatus.Items.Add(s);
            _cmbLogStatus.SelectedIndex = 0;
            filterPanel.Controls.Add(_cmbLogStatus);

            filterPanel.Controls.Add(new Label { Text = "  From:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            _dtpLogFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today.AddMonths(-1) };
            filterPanel.Controls.Add(_dtpLogFrom);

            filterPanel.Controls.Add(new Label { Text = "  To:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            _dtpLogTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
            filterPanel.Controls.Add(_dtpLogTo);

            _btnApplyFilters = MakeButton("Apply Filters", 120);
            _btnApplyFilters.Margin = new Padding(8, 2, 0, 0);
            _btnApplyFilters.Click += (_, _) => ApplyLogFilters();
            filterPanel.Controls.Add(_btnApplyFilters);

            _btnExportCsv = MakeButton("Export CSV", 110);
            _btnExportCsv.Margin = new Padding(4, 2, 0, 0);
            _btnExportCsv.Click += btnExportCsv_Click;
            filterPanel.Controls.Add(_btnExportCsv);

            var refresh = MakeButton("Refresh", 90);
            refresh.Margin = new Padding(4, 2, 0, 0);
            refresh.Click += async (_, _) => await LoadLogAsync();
            filterPanel.Controls.Add(refresh);

            var root = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(_gridLog);
            root.Controls.Add(filterPanel);
            _tabLog.Controls.Add(root);

            _tabs.TabPages.Add(_tabLog);
        }

        Controls.Add(_tabs);

        Load += async (_, _) =>
        {
            if (_tabOpen is not null) await LoadOpenAsync();
            if (_tabMine is not null) await LoadMineAsync();
            if (_tabLog  is not null) await LoadLogAsync();
        };
    }

    private async Task LoadOpenAsync()
    {
        if (_gridOpen is null) return;
        try
        {
            _gridOpen.Rows.Clear();
            var issues = await _api.GetMaintenanceIssuesAsync();
            foreach (var i in issues.Where(x => x.Status == MaintenanceStatus.Open))
            {
                _gridOpen.Rows.Add(
                    i.Id,
                    i.FacilityName,
                    i.ReporterName,
                    i.Severity,
                    Truncate(i.Description, 80),
                    i.CreatedAtUtc.ToLocalTime().ToString("g"));
            }
        }
        catch (Exception ex)
        {
            ShowError("Could not load issues", ex);
        }
    }

    private async Task LoadMineAsync()
    {
        if (_gridMine is null) return;
        try
        {
            _gridMine.Rows.Clear();
            var issues = await _api.GetMaintenanceIssuesAsync();
            var mine = issues.Where(i =>
                i.AssigneeId == _session.UserId &&
                (i.Status == MaintenanceStatus.Pending || i.Status == MaintenanceStatus.InProgress));

            foreach (var i in mine)
            {
                _gridMine.Rows.Add(
                    i.Id,
                    i.FacilityName,
                    i.Severity,
                    Truncate(i.Description, 80),
                    i.Status,
                    i.AssignedAtUtc?.ToLocalTime().ToString("g") ?? "");
            }
        }
        catch (Exception ex)
        {
            ShowError("Could not load tasks", ex);
        }
    }

    private async Task LoadLogAsync()
    {
        if (_gridLog is null || _cmbLogType is null) return;
        try
        {
            _logCache = await _api.GetMaintenanceIssuesAsync();

            var facilities = await _api.GetFacilitiesAsync(includeInactive: true);
            _facilityTypeByFacilityId = facilities.ToDictionary(f => f.Id, f => f.FacilityTypeName);

            var existing = _cmbLogType.SelectedItem as string;
            _cmbLogType.Items.Clear();
            _cmbLogType.Items.Add("(any)");
            foreach (var t in _facilityTypeByFacilityId.Values.Distinct().OrderBy(x => x))
                _cmbLogType.Items.Add(t);
            var idx = existing is null ? 0 : Math.Max(0, _cmbLogType.Items.IndexOf(existing));
            _cmbLogType.SelectedIndex = idx;

            ApplyLogFilters();
        }
        catch (Exception ex)
        {
            ShowError("Could not load log", ex);
        }
    }

    private void ApplyLogFilters()
    {
        if (_gridLog is null) return;

        var typeFilter   = _cmbLogType?.SelectedItem as string;
        var statusFilter = _cmbLogStatus?.SelectedItem as string;
        var from         = _dtpLogFrom?.Value.Date ?? DateTime.MinValue;
        // Push "to" to the last tick of the day so an inclusive end-of-day filter does not drop rows created later that day.
        var to           = (_dtpLogTo?.Value.Date ?? DateTime.MaxValue).AddDays(1).AddTicks(-1);

        // chain of optional filters; each guarded so "(any)" or null skips that step
        var query = _logCache.AsEnumerable();
        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "(any)")
            query = query.Where(i =>
                _facilityTypeByFacilityId.TryGetValue(i.FacilityId, out var t) && t == typeFilter);
        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "(any)"
            && Enum.TryParse<MaintenanceStatus>(statusFilter, out var st))
            query = query.Where(i => i.Status == st);
        query = query.Where(i =>
            i.CreatedAtUtc.ToLocalTime() >= from &&
            i.CreatedAtUtc.ToLocalTime() <= to);

        var rows = query.OrderByDescending(i => i.CreatedAtUtc).ToList();

        _gridLog.Rows.Clear();
        foreach (var i in rows)
        {
            var typeName = _facilityTypeByFacilityId.TryGetValue(i.FacilityId, out var t) ? t : "";
            _gridLog.Rows.Add(
                i.Id,
                i.FacilityName,
                typeName,
                i.ReporterName,
                i.AssigneeName ?? "",
                i.Severity,
                i.Status,
                Truncate(i.Description, 100),
                i.CreatedAtUtc.ToLocalTime().ToString("g"),
                i.ResolvedAtUtc?.ToLocalTime().ToString("g") ?? "");
        }

        _tabLog!.Text = $"Log + Export ({rows.Count})";
    }

    private async void btnAssign_Click(object? sender, EventArgs e)
    {
        if (_gridOpen?.CurrentRow is null) return;

        var id = (int)_gridOpen.CurrentRow.Cells["Id"].Value;

        try
        {
            var users = await _api.GetUsersAsync();
            var personnel = users
                .Where(u => u.IsActive && u.Role == nameof(UserRole.MaintenancePersonnel))
                .ToList();

            if (personnel.Count == 0)
            {
                MessageBox.Show("No active maintenance personnel found.", "Assign",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new AssigneePickerDialog(personnel);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedUserId is null)
                return;

            await _api.AssignMaintenanceAsync(id, new AssignMaintenanceRequest
            {
                AssigneeId = dlg.SelectedUserId
            });

            await LoadOpenAsync();
        }
        catch (Exception ex)
        {
            ShowError("Assign failed", ex);
        }
    }

    private async Task UpdateMyStatusAsync(MaintenanceStatus next)
    {
        if (_gridMine?.CurrentRow is null) return;
        var id = (int)_gridMine.CurrentRow.Cells["Id"].Value;
        try
        {
            await _api.UpdateMaintenanceStatusAsync(id, new UpdateStatusRequest { Status = next });
            await LoadMineAsync();
        }
        catch (Exception ex)
        {
            ShowError("Status update failed", ex);
        }
    }

    private void btnExportCsv_Click(object? sender, EventArgs e)
    {
        if (_gridLog is null) return;

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"maintenance-log-{DateTime.Today:yyyy-MM-dd}.csv"
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var sw = new StreamWriter(fs, new UTF8Encoding(true));

            var headers = new List<string>();
            foreach (DataGridViewColumn col in _gridLog.Columns)
                if (col.Visible) headers.Add(col.HeaderText);
            sw.WriteLine(string.Join(",", headers.Select(CsvEscape)));

            foreach (DataGridViewRow row in _gridLog.Rows)
            {
                if (row.IsNewRow) continue;
                var cells = new List<string>();
                foreach (DataGridViewColumn col in _gridLog.Columns)
                {
                    if (!col.Visible) continue;
                    var val = row.Cells[col.Index].Value?.ToString() ?? "";
                    cells.Add(CsvEscape(val));
                }
                sw.WriteLine(string.Join(",", cells));
            }

            MessageBox.Show($"Exported {_gridLog.Rows.Count} rows.", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError("Export failed", ex);
        }
    }

    // RFC 4180: wrap in quotes if the value contains a comma, quote, or newline; double internal quotes.
    private static string CsvEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var needsQuotes = s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        var escaped = s.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "...";

    private static void ShowError(string title, Exception ex)
        => MessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static Button MakeButton(string text, int width) => new()
    {
        Text = text,
        Width = width,
        BackColor = MainTheme.Primary,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
    };

    private static DataGridView MakeGrid(params (string name, string header, bool visible)[] cols)
    {
        var grid = new DataGridView
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
        foreach (var (name, header, visible) in cols)
            grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = name, HeaderText = header, Visible = visible });
        return grid;
    }

    private static Panel BuildTabLayout(DataGridView grid, params Control[] buttons)
    {
        var root = new Panel { Dock = DockStyle.Fill };
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            Height        = 44,
            Padding       = new Padding(4),
            FlowDirection = FlowDirection.LeftToRight
        };

        foreach (var btn in buttons)
        {
            btn.Height = 32;
            btn.Margin = new Padding(4, 4, 0, 0);
            btnPanel.Controls.Add(btn);
        }

        root.Controls.Add(grid);
        root.Controls.Add(btnPanel);
        return root;
    }
}

internal class AssigneePickerDialog : Form
{
    private readonly ListBox _list;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public string? SelectedUserId { get; private set; }

    public AssigneePickerDialog(List<UserResponse> personnel)
    {
        Text            = "Assign To";
        Size            = new Size(400, 360);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox     = false;
        MaximizeBox     = false;
        Font            = MainTheme.BodyFont;
        BackColor       = MainTheme.Background;

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = nameof(UserResponse.DisplayName)
        };
        foreach (var u in personnel)
            _list.Items.Add(u);

        if (_list.Items.Count > 0) _list.SelectedIndex = 0;

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            Width = 90,
            DialogResult = DialogResult.Cancel
        };

        _btnOk = new Button
        {
            Text = "OK",
            Width = 90,
            BackColor = MainTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOk.Click += (_, _) =>
        {
            if (_list.SelectedItem is UserResponse u)
            {
                SelectedUserId = u.Id;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        bottom.Controls.Add(_btnCancel);
        bottom.Controls.Add(_btnOk);

        Controls.Add(_list);
        Controls.Add(bottom);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }
}
