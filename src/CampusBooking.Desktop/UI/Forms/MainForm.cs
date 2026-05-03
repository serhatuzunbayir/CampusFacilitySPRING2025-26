using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace CampusBooking.Desktop.UI.Forms;

public class MainForm : Form
{
    private readonly ApiClient _api;
    private readonly UserSession _session;
    private readonly NotificationPoller _poller;
    private readonly IServiceProvider _services;

    private readonly TabControl _tabs;
    private readonly TabPage _tabFacilities;
    private readonly TabPage _tabBookings;
    private readonly TabPage _tabNotifications;

    private readonly DataGridView _gridFacilities;
    private readonly Button _btnAddFacility;
    private readonly Button _btnDeactivateFacility;
    private readonly Button _btnRefreshFacilities;

    private readonly DataGridView _gridBookings;
    private readonly Button _btnNewBooking;
    private readonly Button _btnCancelBooking;
    private readonly Button _btnApproveBooking;
    private readonly Button _btnRejectBooking;
    private readonly Button _btnRefreshBookings;

    private readonly DataGridView _gridNotifications;
    private readonly Button _btnMarkAllRead;
    private readonly Button _btnRefreshNotifications;
    private readonly Label _lblUnreadBadge;

    private readonly StatusStrip _statusStrip;
    private readonly ToolStripLabel _lblStatusUser;

    public MainForm(ApiClient api, UserSession session, NotificationPoller poller, IServiceProvider services)
    {
        _api = api;
        _session = session;
        _poller = poller;
        _services = services;

        _poller.OnNotification += HandleNewNotification;

        Text          = $"CampusBooking - {_session.DisplayName} ({_session.Role})";
        Size          = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = MainTheme.BodyFont;
        BackColor     = MainTheme.Background;

        _statusStrip   = new StatusStrip();
        _lblStatusUser = new ToolStripLabel($"Logged in as: {_session.DisplayName}  |  Role: {_session.Role}");
        _statusStrip.Items.Add(_lblStatusUser);

        _tabs = new TabControl { Dock = DockStyle.Fill };

        _tabFacilities = new TabPage("Facilities");
        _gridFacilities = MakeGrid(
            ("Id",       "Id",       false),
            ("Name",     "Facility", true),
            ("Type",     "Type",     true),
            ("Capacity", "Capacity", true),
            ("Location", "Location", true),
            ("Active",   "Active",   true));

        _btnRefreshFacilities  = MakeButton("Refresh", 90);
        _btnAddFacility        = MakeButton("Add", 80, _session.IsManager);
        _btnDeactivateFacility = MakeButton("Deactivate", 90, _session.IsManager);

        _btnRefreshFacilities.Click  += async (_, _) => await LoadFacilitiesAsync();
        _btnAddFacility.Click        += btnAddFacility_Click;
        _btnDeactivateFacility.Click += btnDeactivateFacility_Click;

        _tabFacilities.Controls.Add(BuildTabLayout(_gridFacilities,
            _btnRefreshFacilities, _btnAddFacility, _btnDeactivateFacility));

        _tabBookings = new TabPage("Bookings");
        _gridBookings = MakeGrid(
            ("Id",       "Id",       false),
            ("Facility", "Facility", true),
            ("User",     "User",     true),
            ("Date",     "Date",     true),
            ("TimeSlot", "Hour",     true),
            ("Status",   "Status",   true));

        var canBook = _session.Role is "Student" or "Staff";
        _btnRefreshBookings = MakeButton("Refresh", 90);
        _btnNewBooking      = MakeButton("New", 80, canBook);
        _btnCancelBooking   = MakeButton("Cancel", 80, canBook);
        _btnApproveBooking  = MakeButton("Approve", 90, _session.IsManager);
        _btnRejectBooking   = MakeButton("Reject", 80, _session.IsManager);

        _btnRefreshBookings.Click += async (_, _) => await LoadBookingsAsync();
        _btnNewBooking.Click      += btnNewBooking_Click;
        _btnCancelBooking.Click   += btnCancelBooking_Click;
        _btnApproveBooking.Click  += btnApproveBooking_Click;
        _btnRejectBooking.Click   += btnRejectBooking_Click;

        _tabBookings.Controls.Add(BuildTabLayout(_gridBookings,
            _btnRefreshBookings, _btnNewBooking, _btnCancelBooking,
            _btnApproveBooking, _btnRejectBooking));

        _tabNotifications = new TabPage("Notifications");
        _gridNotifications = MakeGrid(
            ("Id",      "Id",      false),
            ("Kind",    "Kind",    true),
            ("Message", "Message", true),
            ("Read",    "Read",    true),
            ("Time",    "Time",    true));

        _lblUnreadBadge          = new Label { Text = string.Empty, ForeColor = MainTheme.Danger, AutoSize = true };
        _btnMarkAllRead          = MakeButton("Mark All Read", 120);
        _btnRefreshNotifications = MakeButton("Refresh", 90);

        _btnMarkAllRead.Click          += btnMarkAllRead_Click;
        _btnRefreshNotifications.Click += async (_, _) => await LoadNotificationsAsync();

        _tabNotifications.Controls.Add(BuildTabLayout(_gridNotifications,
            _btnRefreshNotifications, _btnMarkAllRead, _lblUnreadBadge));

        _tabs.TabPages.Add(_tabFacilities);
        _tabs.TabPages.Add(_tabBookings);
        _tabs.TabPages.Add(_tabNotifications);

        if (_session.IsManager || _session.Role == "MaintenancePersonnel")
        {
            var tabMaint = new TabPage("Maintenance");
            var btnOpen = MakeButton("Open Maintenance Window", 220);
            btnOpen.Height = 36;
            btnOpen.Margin = new Padding(20);
            btnOpen.Click += (_, _) =>
            {
                var form = _services.GetRequiredService<MaintenanceForm>();
                form.ShowDialog(this);
            };
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                FlowDirection = FlowDirection.LeftToRight
            };
            panel.Controls.Add(btnOpen);
            tabMaint.Controls.Add(panel);
            _tabs.TabPages.Add(tabMaint);
        }

        Controls.Add(_tabs);
        Controls.Add(_statusStrip);

        Load += async (_, _) =>
        {
            _poller.Start();
            await LoadFacilitiesAsync();
            await LoadBookingsAsync();
            await LoadNotificationsAsync();
        };
    }

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            _gridFacilities.Rows.Clear();
            var facilities = await _api.GetFacilitiesAsync(includeInactive: _session.IsManager);
            foreach (var f in facilities)
                _gridFacilities.Rows.Add(f.Id, f.Name, f.FacilityTypeName, f.Capacity, f.Location, f.IsActive);
        }
        catch (Exception ex)
        {
            ShowError("Could not load facilities", ex);
        }
    }

    private async Task LoadBookingsAsync()
    {
        try
        {
            _gridBookings.Rows.Clear();
            var bookings = _session.IsManager
                ? await _api.GetAllBookingsAsync()
                : await _api.GetMyBookingsAsync();

            foreach (var b in bookings)
                _gridBookings.Rows.Add(
                    b.Id, b.FacilityName, b.UserDisplayName,
                    b.Date.ToString("yyyy-MM-dd"),
                    $"{b.TimeSlot:00}:00", b.Status);

            _tabBookings.Text = $"Bookings ({bookings.Count})";
        }
        catch (Exception ex)
        {
            ShowError("Could not load bookings", ex);
        }
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            _gridNotifications.Rows.Clear();
            var notifications = await _api.GetNotificationsAsync();

            foreach (var n in notifications)
                _gridNotifications.Rows.Add(
                    n.Id, n.Kind, n.Message,
                    n.IsRead ? "Yes" : "No",
                    n.CreatedAtUtc.ToLocalTime().ToString("g"));

            var unreadCount = notifications.Count(n => !n.IsRead);
            _lblUnreadBadge.Text = unreadCount > 0 ? $"  {unreadCount} unread" : string.Empty;
            _tabNotifications.Text = unreadCount > 0 ? $"Notifications ({unreadCount})" : "Notifications";
        }
        catch (Exception ex)
        {
            ShowError("Could not load notifications", ex);
        }
    }

    private void btnAddFacility_Click(object? sender, EventArgs e)
    {
        var dlg = _services.GetRequiredService<AddFacilityForm>();
        dlg.ShowDialog(this);
        if (dlg.FacilityCreated)
            _ = LoadFacilitiesAsync();
    }

    private async void btnDeactivateFacility_Click(object? sender, EventArgs e)
    {
        if (_gridFacilities.CurrentRow is null) return;

        var id   = (int)_gridFacilities.CurrentRow.Cells["Id"].Value;
        var name = (string)_gridFacilities.CurrentRow.Cells["Name"].Value;

        if (MessageBox.Show($"Deactivate \"{name}\"?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            await _api.DeactivateFacilityAsync(id);
            await LoadFacilitiesAsync();
        }
        catch (Exception ex)
        {
            ShowError("Deactivate failed", ex);
        }
    }

    private void btnNewBooking_Click(object? sender, EventArgs e)
    {
        var dlg = _services.GetRequiredService<CreateBookingForm>();
        dlg.ShowDialog(this);
        if (dlg.BookingCreated)
            _ = LoadBookingsAsync();
    }

    private async void btnCancelBooking_Click(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;

        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;

        if (MessageBox.Show("Cancel this booking?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try
        {
            await _api.CancelBookingAsync(id);
            await LoadBookingsAsync();
        }
        catch (Exception ex)
        {
            ShowError("Cancel failed", ex);
        }
    }

    private async void btnApproveBooking_Click(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;
        try
        {
            await _api.ApproveBookingAsync(id);
            await LoadBookingsAsync();
        }
        catch (Exception ex)
        {
            ShowError("Approve failed", ex);
        }
    }

    private async void btnRejectBooking_Click(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;
        try
        {
            await _api.RejectBookingAsync(id);
            await LoadBookingsAsync();
        }
        catch (Exception ex)
        {
            ShowError("Reject failed", ex);
        }
    }

    private void HandleNewNotification(NotificationKind kind, string message)
    {
        // Poller fires on a worker thread; marshal back to the UI thread before touching controls.
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => HandleNewNotification(kind, message)));
            return;
        }
        _lblStatusUser.Text = $"New: [{kind}] {message}";
        _ = LoadNotificationsAsync();
    }

    private async void btnMarkAllRead_Click(object? sender, EventArgs e)
    {
        try
        {
            await _api.MarkAllReadAsync();
            await LoadNotificationsAsync();
            _lblStatusUser.Text = $"Logged in as: {_session.DisplayName}  |  Role: {_session.Role}";
        }
        catch (Exception ex)
        {
            ShowError("Mark all read failed", ex);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _poller.Stop();
        _poller.OnNotification -= HandleNewNotification;
        base.OnFormClosed(e);
    }

    private static void ShowError(string title, Exception ex)
        => MessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static Button MakeButton(string text, int width, bool enabled = true) => new()
    {
        Text = text,
        Width = width,
        Enabled = enabled,
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
