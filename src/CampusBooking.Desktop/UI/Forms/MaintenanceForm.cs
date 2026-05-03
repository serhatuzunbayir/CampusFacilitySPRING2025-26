using System.Text;
using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Forms.Details;
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

    // Each grid keeps its own cache so double-click can resolve a row back to the full DTO.
    private List<MaintenanceIssueResponse> _openCache = new();
    private List<MaintenanceIssueResponse> _mineCache = new();

    public MaintenanceForm(ApiClient api, UserSession session, IServiceProvider services)
    {
        _api = api;
        _session = session;
        _services = services;

        Text            = "Maintenance";
        Size            = new Size(1100, 720);
        StartPosition   = FormStartPosition.CenterParent;
        Font            = MainTheme.Body;
        BackColor       = MainTheme.Background;
        MinimizeBox     = false;

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = MainTheme.Body };

        if (_session.IsManager)
        {
            _tabOpen = new TabPage("Open Issues");
            _gridOpen = MakeGrid(
                ("Id",          "Id",          false),
                ("Facility",    "Facility",    true),
                ("Reporter",    "Reporter",    true),
                ("Severity",    "Severity",    true),
                ("Description", "Description", true),
                ("CreatedAt",   "Created",     true));
            _gridOpen.CellFormatting += Grid_CellFormatting;
            _gridOpen.CellDoubleClick += GridOpen_CellDoubleClick;

            _btnRefreshOpen = Styles.SecondaryButton("Refresh", 100);
            _btnAssign      = Styles.PrimaryButton("Assign...", 120);
            _btnRefreshOpen.Click += async (_, _) => await LoadOpenAsync();
            _btnAssign.Click      += btnAssign_Click;

            _tabOpen.Controls.Add(BuildTabLayout(_gridOpen, _btnAssign, _btnRefreshOpen));
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
            _gridMine.CellFormatting += Grid_CellFormatting;
            _gridMine.CellDoubleClick += GridMine_CellDoubleClick;

            _btnRefreshMine = Styles.SecondaryButton("Refresh", 100);
            _btnStart       = Styles.PrimaryButton("Start", 90);
            _btnResolve     = Styles.PrimaryButton("Resolve", 100);
            _btnRefreshMine.Click += async (_, _) => await LoadMineAsync();
            _btnStart.Click       += async (_, _) => await UpdateMyStatusAsync(MaintenanceStatus.InProgress);
            _btnResolve.Click     += async (_, _) => await UpdateMyStatusAsync(MaintenanceStatus.Resolved);

            _tabMine.Controls.Add(BuildTabLayout(_gridMine, _btnResolve, _btnStart, _btnRefreshMine));
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
            _gridLog.CellFormatting += Grid_CellFormatting;
            _gridLog.CellDoubleClick += GridLog_CellDoubleClick;

            var filterPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                Padding   = new Padding(12, 10, 12, 10),
                BackColor = MainTheme.SurfaceAlt
            };

            // Lay out the filter row using a TableLayoutPanel so the FieldLabels sit above each input.
            var fl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 7,
                RowCount    = 2,
                BackColor   = Color.Transparent
            };
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            fl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fl.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            fl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            fl.Controls.Add(Styles.FieldLabel("Type"),    0, 0);
            fl.Controls.Add(Styles.FieldLabel("Status"),  1, 0);
            fl.Controls.Add(Styles.FieldLabel("From"),    2, 0);
            fl.Controls.Add(Styles.FieldLabel("To"),      3, 0);

            _cmbLogType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = MainTheme.Body, FlatStyle = FlatStyle.Flat };
            _cmbLogType.Items.Add("(any)");
            _cmbLogType.SelectedIndex = 0;
            fl.Controls.Add(_cmbLogType, 0, 1);

            _cmbLogStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = MainTheme.Body, FlatStyle = FlatStyle.Flat };
            _cmbLogStatus.Items.Add("(any)");
            foreach (var s in Enum.GetNames<MaintenanceStatus>())
                _cmbLogStatus.Items.Add(s);
            _cmbLogStatus.SelectedIndex = 0;
            fl.Controls.Add(_cmbLogStatus, 1, 1);

            _dtpLogFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today.AddMonths(-1), Font = MainTheme.Body };
            fl.Controls.Add(_dtpLogFrom, 2, 1);

            _dtpLogTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today, Font = MainTheme.Body };
            fl.Controls.Add(_dtpLogTo, 3, 1);

            _btnApplyFilters = Styles.PrimaryButton("Apply", 110);
            _btnApplyFilters.Click += (_, _) => ApplyLogFilters();
            fl.Controls.Add(_btnApplyFilters, 4, 1);

            _btnExportCsv = Styles.SecondaryButton("Export CSV", 110);
            _btnExportCsv.Click += btnExportCsv_Click;
            fl.Controls.Add(_btnExportCsv, 5, 1);

            var refresh = Styles.SecondaryButton("Refresh", 100);
            refresh.Click += async (_, _) => await LoadLogAsync();
            fl.Controls.Add(refresh, 6, 1);

            filterPanel.Controls.Add(fl);

            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
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
            _openCache = issues.Where(x => x.Status == MaintenanceStatus.Open).ToList();
            foreach (var i in _openCache)
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
            _mineCache = issues.Where(i =>
                i.AssigneeId == _session.UserId &&
                (i.Status == MaintenanceStatus.Pending || i.Status == MaintenanceStatus.InProgress)).ToList();

            foreach (var i in _mineCache)
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

    private void GridOpen_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => OpenIssueDetails(_gridOpen, _openCache, e);

    private void GridMine_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => OpenIssueDetails(_gridMine, _mineCache, e);

    private void GridLog_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        => OpenIssueDetails(_gridLog, _logCache, e);

    private void OpenIssueDetails(DataGridView? grid, IEnumerable<MaintenanceIssueResponse> source, DataGridViewCellEventArgs e)
    {
        if (grid is null || e.RowIndex < 0) return;
        var idCell = grid.Rows[e.RowIndex].Cells["Id"].Value;
        if (idCell is not int id) return;
        var issue = source.FirstOrDefault(x => x.Id == id);
        if (issue is null) return;
        using var dlg = new MaintenanceDetailsForm(issue);
        dlg.ShowDialog(this);
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid) return;
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var col = grid.Columns[e.ColumnIndex].Name;
        var style = e.CellStyle;
        if (style is null) return;

        if (col == "Severity" && e.Value is IssueSeverity sev)
        {
            ApplySeverityStyle(style, sev);
            e.Value = sev.ToString();
            e.FormattingApplied = true;
        }
        else if (col == "Status" && e.Value is MaintenanceStatus status)
        {
            ApplyStatusStyle(style, status);
            e.Value = status.ToString();
            e.FormattingApplied = true;
        }
    }

    private static void ApplySeverityStyle(DataGridViewCellStyle style, IssueSeverity sev)
    {
        switch (sev)
        {
            case IssueSeverity.Critical:
                style.BackColor = MainTheme.DangerSoft;
                style.ForeColor = MainTheme.Danger;
                break;
            case IssueSeverity.High:
                style.BackColor = MainTheme.WarningSoft;
                style.ForeColor = MainTheme.Warning;
                break;
            case IssueSeverity.Medium:
                style.BackColor = MainTheme.InfoSoft;
                style.ForeColor = MainTheme.Info;
                break;
            default:
                style.BackColor = MainTheme.NeutralSoft;
                style.ForeColor = MainTheme.Neutral;
                break;
        }
        style.Font = MainTheme.BodyBold;
    }

    private static void ApplyStatusStyle(DataGridViewCellStyle style, MaintenanceStatus status)
    {
        switch (status)
        {
            case MaintenanceStatus.Open:
                style.BackColor = MainTheme.InfoSoft;
                style.ForeColor = MainTheme.Info;
                break;
            case MaintenanceStatus.Pending:
                style.BackColor = MainTheme.WarningSoft;
                style.ForeColor = MainTheme.Warning;
                break;
            case MaintenanceStatus.InProgress:
                style.BackColor = MainTheme.InfoSoft;
                style.ForeColor = MainTheme.Info;
                break;
            case MaintenanceStatus.Resolved:
                style.BackColor = MainTheme.SuccessSoft;
                style.ForeColor = MainTheme.Success;
                break;
        }
        style.Font = MainTheme.BodyBold;
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

    private static DataGridView MakeGrid(params (string name, string header, bool visible)[] cols)
    {
        var grid = Styles.NiceGrid();
        grid.Dock = DockStyle.Fill;
        foreach (var (name, header, visible) in cols)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = name, HeaderText = header, Visible = visible });
        return grid;
    }

    private static Panel BuildTabLayout(DataGridView grid, params Control[] buttonsRightToLeft)
    {
        var root = new Panel { Dock = DockStyle.Fill };
        var btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 56,
            Padding   = new Padding(12, 10, 12, 10),
            BackColor = MainTheme.SurfaceAlt
        };

        // Right-align the action buttons in the order passed (first = furthest right).
        foreach (var btn in buttonsRightToLeft)
        {
            btn.Dock = DockStyle.Right;
            btn.Margin = new Padding(8, 0, 0, 0);
            btnPanel.Controls.Add(btn);
            // Spacer between adjacent docked controls so they don't touch.
            btnPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Color.Transparent });
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
        Size            = new Size(440, 420);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox     = false;
        MaximizeBox     = false;
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
            Text      = "Assign To",
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
            Padding   = new Padding(20)
        };

        var hint = Styles.FieldLabel("Pick a maintenance personnel");
        hint.Dock = DockStyle.Top;
        hint.Margin = new Padding(0, 0, 0, 6);

        _list = new ListBox
        {
            Dock          = DockStyle.Fill,
            DisplayMember = nameof(UserResponse.DisplayName),
            Font          = MainTheme.Body,
            BorderStyle   = BorderStyle.FixedSingle,
            ItemHeight    = 24
        };
        foreach (var u in personnel)
            _list.Items.Add(u);

        if (_list.Items.Count > 0) _list.SelectedIndex = 0;

        card.Controls.Add(_list);
        card.Controls.Add(hint);

        var bottom = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 60,
            BackColor = MainTheme.SurfaceAlt,
            Padding   = new Padding(20, 12, 20, 12)
        };

        _btnOk = Styles.PrimaryButton("Assign", 110);
        _btnOk.Dock = DockStyle.Right;
        _btnOk.Click += (_, _) =>
        {
            if (_list.SelectedItem is UserResponse u)
            {
                SelectedUserId = u.Id;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        _btnCancel = Styles.SecondaryButton("Cancel", 100);
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Margin = new Padding(0, 0, 8, 0);

        var sp = new Panel { Dock = DockStyle.Right, Width = 8 };

        bottom.Controls.Add(_btnOk);
        bottom.Controls.Add(sp);
        bottom.Controls.Add(_btnCancel);

        Controls.Add(card);
        Controls.Add(bottom);
        Controls.Add(titleBar);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }
}
