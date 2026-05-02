using CampusBooking.Desktop.Services;
using CampusBooking.Shared;

namespace CampusBooking.Desktop.Forms;

/// <summary>
/// The main application window shown after a successful login.
/// Contains three tabs: Facilities, Bookings, and Notifications.
///
/// Delegate usage:
///   A System.Windows.Forms.Timer polls the notifications endpoint every 30 seconds.
///   When new unread notifications arrive, it calls NotificationService.Raise() which
///   fires the NotificationHandler delegate. The subscribed handler updates the tab
///   header and shows a balloon tip in the status bar.
/// </summary>
public class MainForm : Form
{
    private readonly ApiClient          _api;
    private readonly NotificationService _notificationService;

    // ── Tabs ──────────────────────────────────────────────────────────────────
    private readonly TabControl  _tabs;
    private readonly TabPage     _tabFacilities;
    private readonly TabPage     _tabBookings;
    private readonly TabPage     _tabNotifications;

    // ── Facilities tab controls ───────────────────────────────────────────────
    private readonly DataGridView _gridFacilities;
    private readonly Button       _btnAddFacility;
    private readonly Button       _btnDeactivateFacility;
    private readonly Button       _btnRefreshFacilities;

    // ── Bookings tab controls ─────────────────────────────────────────────────
    private readonly DataGridView _gridBookings;
    private readonly Button       _btnNewBooking;
    private readonly Button       _btnCancelBooking;
    private readonly Button       _btnApproveBooking;
    private readonly Button       _btnRejectBooking;
    private readonly Button       _btnRefreshBookings;

    // ── Notifications tab controls ────────────────────────────────────────────
    private readonly DataGridView _gridNotifications;
    private readonly Button       _btnMarkAllRead;
    private readonly Button       _btnRefreshNotifications;
    private readonly Label        _lblUnreadBadge;

    // ── Status strip ─────────────────────────────────────────────────────────
    private readonly StatusStrip       _statusStrip;
    private readonly ToolStripLabel    _lblStatusUser;

    // ── Notification polling timer ────────────────────────────────────────────
    /// <summary>Fires every 30 seconds to check for new notifications via the API.</summary>
    private readonly System.Windows.Forms.Timer _notificationTimer;

    /// <summary>Tracks the last known unread count to detect new arrivals.</summary>
    private int _lastUnreadCount;

    public MainForm(ApiClient api)
    {
        _api                 = api;
        _notificationService = new NotificationService();

        // Subscribe the UI handler to the delegate event (FR6 delegate requirement)
        _notificationService.OnNotification += HandleNewNotification;

        // ── Window ────────────────────────────────────────────────────────────
        Text          = $"CampusBooking — {SessionState.DisplayName} ({SessionState.Role})";
        Size          = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = new Font("Segoe UI", 10f);

        // ── Status strip (bottom bar) ─────────────────────────────────────────
        _statusStrip  = new StatusStrip();
        _lblStatusUser = new ToolStripLabel($"Logged in as: {SessionState.DisplayName}  |  Role: {SessionState.Role}");
        _statusStrip.Items.Add(_lblStatusUser);
        Controls.Add(_statusStrip);

        // ── Tab control ───────────────────────────────────────────────────────
        _tabs = new TabControl { Dock = DockStyle.Fill };

        // ─ Facilities tab ─────────────────────────────────────────────────────
        _tabFacilities = new TabPage("Facilities");
        _gridFacilities = MakeGrid(
            ("Id",       "Id",       false),
            ("Name",     "Facility", true),
            ("Type",     "Type",     true),
            ("Capacity", "Capacity", true),
            ("Location", "Location", true),
            ("Active",   "Active",   true));

        _btnRefreshFacilities   = new Button { Text = "Refresh",    Width = 90 };
        _btnAddFacility         = new Button { Text = "Add",        Width = 80, Enabled = SessionState.IsManager };
        _btnDeactivateFacility  = new Button { Text = "Deactivate", Width = 90, Enabled = SessionState.IsManager };

        _btnRefreshFacilities.Click  += async (_, _) => await LoadFacilitiesAsync();
        _btnAddFacility.Click        += BtnAddFacility_Click;
        _btnDeactivateFacility.Click += BtnDeactivateFacility_ClickAsync;

        _tabFacilities.Controls.Add(BuildTabLayout(_gridFacilities,
            _btnRefreshFacilities, _btnAddFacility, _btnDeactivateFacility));

        // ─ Bookings tab ───────────────────────────────────────────────────────
        _tabBookings = new TabPage("Bookings");
        _gridBookings = MakeGrid(
            ("Id",       "Id",       false),
            ("Facility", "Facility", true),
            ("User",     "User",     true),
            ("Date",     "Date",     true),
            ("TimeSlot", "Hour",     true),
            ("Status",   "Status",   true));

        _btnRefreshBookings = new Button { Text = "Refresh",  Width = 90 };
        _btnNewBooking      = new Button { Text = "New",      Width = 80,
                                           Enabled = SessionState.Role is "Student" or "Staff" };
        _btnCancelBooking   = new Button { Text = "Cancel",   Width = 80,
                                           Enabled = SessionState.Role is "Student" or "Staff" };
        _btnApproveBooking  = new Button { Text = "Approve",  Width = 90, Enabled = SessionState.IsManager };
        _btnRejectBooking   = new Button { Text = "Reject",   Width = 80, Enabled = SessionState.IsManager };

        _btnRefreshBookings.Click += async (_, _) => await LoadBookingsAsync();
        _btnNewBooking.Click      += BtnNewBooking_Click;
        _btnCancelBooking.Click   += BtnCancelBooking_ClickAsync;
        _btnApproveBooking.Click  += BtnApproveBooking_ClickAsync;
        _btnRejectBooking.Click   += BtnRejectBooking_ClickAsync;

        _tabBookings.Controls.Add(BuildTabLayout(_gridBookings,
            _btnRefreshBookings, _btnNewBooking, _btnCancelBooking,
            _btnApproveBooking, _btnRejectBooking));

        // ─ Notifications tab ──────────────────────────────────────────────────
        _tabNotifications = new TabPage("Notifications");
        _gridNotifications = MakeGrid(
            ("Id",      "Id",      false),
            ("Kind",    "Kind",    true),
            ("Message", "Message", true),
            ("Read",    "Read",    true),
            ("Time",    "Time",    true));

        _lblUnreadBadge          = new Label { Text = string.Empty, ForeColor = Color.DarkRed, AutoSize = true };
        _btnMarkAllRead          = new Button { Text = "Mark All Read", Width = 120 };
        _btnRefreshNotifications = new Button { Text = "Refresh",       Width = 90 };

        _btnMarkAllRead.Click          += BtnMarkAllRead_ClickAsync;
        _btnRefreshNotifications.Click += async (_, _) => await LoadNotificationsAsync();

        _tabNotifications.Controls.Add(BuildTabLayout(_gridNotifications,
            _btnRefreshNotifications, _btnMarkAllRead, _lblUnreadBadge));

        // Add tabs
        _tabs.TabPages.Add(_tabFacilities);
        _tabs.TabPages.Add(_tabBookings);
        _tabs.TabPages.Add(_tabNotifications);
        Controls.Add(_tabs);

        // ── Notification polling timer ─────────────────────────────────────────
        _notificationTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _notificationTimer.Tick += NotificationTimer_TickAsync;
        _notificationTimer.Start();

        // Load initial data when the form first appears
        Load += async (_, _) =>
        {
            await LoadFacilitiesAsync();
            await LoadBookingsAsync();
            await LoadNotificationsAsync();
        };
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches facilities from the API and populates the grid.
    /// LINQ is used server-side (in the API) to order and filter; here we
    /// just project into grid rows.
    /// </summary>
    private async Task LoadFacilitiesAsync()
    {
        _gridFacilities.Rows.Clear();
        var facilities = await _api.GetFacilitiesAsync(includeInactive: SessionState.IsManager);
        foreach (var f in facilities)
            _gridFacilities.Rows.Add(f.Id, f.Name, f.FacilityTypeName, f.Capacity, f.Location, f.IsActive);
    }

    /// <summary>
    /// Loads bookings for the current user (or all bookings for managers).
    /// Demonstrates LINQ-style aggregation: the grid shows a count in the tab header.
    /// </summary>
    private async Task LoadBookingsAsync()
    {
        _gridBookings.Rows.Clear();

        // Managers see all bookings; other roles see only their own
        var bookings = SessionState.IsManager
            ? await _api.GetAllBookingsAsync()
            : await _api.GetMyBookingsAsync();

        foreach (var b in bookings)
            _gridBookings.Rows.Add(
                b.Id, b.FacilityName, b.UserDisplayName,
                b.Date.ToString("yyyy-MM-dd"),
                $"{b.TimeSlot:00}:00", b.Status);

        // Update tab header with total count (simple aggregation)
        _tabBookings.Text = $"Bookings ({bookings.Count})";
    }

    /// <summary>Loads the current user's notification inbox.</summary>
    private async Task LoadNotificationsAsync()
    {
        _gridNotifications.Rows.Clear();
        var notifications = await _api.GetNotificationsAsync();

        foreach (var n in notifications)
            _gridNotifications.Rows.Add(
                n.Id, n.Kind, n.Message,
                n.IsRead ? "Yes" : "No",
                n.CreatedAtUtc.ToLocalTime().ToString("g"));

        // Count unread notifications using LINQ
        var unreadCount = notifications.Count(n => !n.IsRead);
        _lastUnreadCount   = unreadCount;
        _lblUnreadBadge.Text = unreadCount > 0 ? $"  {unreadCount} unread" : string.Empty;
        _tabNotifications.Text = unreadCount > 0 ? $"Notifications ({unreadCount}!)" : "Notifications";
    }

    // ── Facilities handlers ───────────────────────────────────────────────────

    /// <summary>Opens the Add Facility dialog and refreshes the list on success.</summary>
    private void BtnAddFacility_Click(object? sender, EventArgs e)
    {
        var dlg = new AddFacilityForm(_api);
        dlg.ShowDialog(this);
        if (dlg.FacilityCreated)
            _ = LoadFacilitiesAsync();
    }

    /// <summary>Deactivates the selected facility after confirmation.</summary>
    private async void BtnDeactivateFacility_ClickAsync(object? sender, EventArgs e)
    {
        if (_gridFacilities.CurrentRow is null) return;

        var id   = (int)_gridFacilities.CurrentRow.Cells["Id"].Value;
        var name = (string)_gridFacilities.CurrentRow.Cells["Name"].Value;

        if (MessageBox.Show($"Deactivate \"{name}\"?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var ok = await _api.DeactivateFacilityAsync(id);
        if (ok)
            await LoadFacilitiesAsync();
        else
            MessageBox.Show("Failed to deactivate facility.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ── Booking handlers ──────────────────────────────────────────────────────

    /// <summary>Opens the Create Booking dialog and refreshes the list on success.</summary>
    private void BtnNewBooking_Click(object? sender, EventArgs e)
    {
        var dlg = new CreateBookingForm(_api);
        dlg.ShowDialog(this);
        if (dlg.BookingCreated)
            _ = LoadBookingsAsync();
    }

    /// <summary>Cancels the selected booking (must be > 2 hours before the slot).</summary>
    private async void BtnCancelBooking_ClickAsync(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;

        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;

        if (MessageBox.Show("Cancel this booking?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var ok = await _api.CancelBookingAsync(id);
        if (ok)
            await LoadBookingsAsync();
        else
            MessageBox.Show("Cannot cancel — either the slot starts within 2 hours, or the booking no longer exists.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>Approves the selected pending booking (FacilityManager only).</summary>
    private async void BtnApproveBooking_ClickAsync(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;
        var ok = await _api.ApproveBookingAsync(id);
        if (ok) await LoadBookingsAsync();
        else MessageBox.Show("Approve failed (booking may no longer be pending).", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>Rejects the selected pending booking (FacilityManager only).</summary>
    private async void BtnRejectBooking_ClickAsync(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;
        var ok = await _api.RejectBookingAsync(id);
        if (ok) await LoadBookingsAsync();
        else MessageBox.Show("Reject failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ── Notification handlers ─────────────────────────────────────────────────

    /// <summary>
    /// Polls the API every 30 seconds for new unread notifications.
    /// If new ones arrive, fires the NotificationService delegate so all
    /// subscribers (including HandleNewNotification below) are notified.
    /// </summary>
    private async void NotificationTimer_TickAsync(object? sender, EventArgs e)
    {
        try
        {
            var notifications  = await _api.GetNotificationsAsync(unreadOnly: true);
            var currentUnread  = notifications.Count;

            // Fire the delegate only when the unread count has increased
            if (currentUnread > _lastUnreadCount)
            {
                foreach (var n in notifications.Take(currentUnread - _lastUnreadCount))
                    _notificationService.Raise((NotificationKind)Enum.Parse(typeof(NotificationKind), n.Kind), n.Message);
            }

            _lastUnreadCount = currentUnread;
        }
        catch
        {
            // Silently swallow timer errors (e.g. network loss) to avoid crashing
        }
    }

    /// <summary>
    /// NotificationHandler delegate subscriber. Called by NotificationService.Raise()
    /// when a new notification arrives. Updates the tab badge and status bar.
    /// Always runs on the UI thread because WinForms Timer fires on it.
    /// </summary>
    private void HandleNewNotification(NotificationKind kind, string message)
    {
        _lastUnreadCount++;
        _tabNotifications.Text = $"Notifications ({_lastUnreadCount}!)";
        _lblStatusUser.Text    = $"🔔 New: [{kind}] {message}";

        // Refresh the grid to show the new entry
        _ = LoadNotificationsAsync();
    }

    /// <summary>Marks all notifications as read and refreshes the grid.</summary>
    private async void BtnMarkAllRead_ClickAsync(object? sender, EventArgs e)
    {
        await _api.MarkAllReadAsync();
        await LoadNotificationsAsync();
        _lblStatusUser.Text = $"Logged in as: {SessionState.DisplayName}  |  Role: {SessionState.Role}";
    }

    // ── Form close ────────────────────────────────────────────────────────────

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _notificationTimer.Stop();
        _notificationTimer.Dispose();
        base.OnFormClosed(e);
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a read-only DataGridView with the given columns.
    /// Each column spec: (name, header, visible).
    /// </summary>
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

    /// <summary>
    /// Builds a tab layout: DataGridView fills the centre, buttons appear in a
    /// panel at the bottom.
    /// </summary>
    private static Panel BuildTabLayout(DataGridView grid, params Control[] buttons)
    {
        var root       = new Panel { Dock = DockStyle.Fill };
        var btnPanel   = new FlowLayoutPanel
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
