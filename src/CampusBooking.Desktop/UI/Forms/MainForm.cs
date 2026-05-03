using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Forms.Details;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared;
using CampusBooking.Shared.Dtos.Bookings;
using CampusBooking.Shared.Dtos.Facilities;
using Microsoft.Extensions.DependencyInjection;

namespace CampusBooking.Desktop.UI.Forms;

public class MainForm : Form
{
    private readonly ApiClient _api;
    private readonly UserSession _session;
    private readonly NotificationPoller _poller;
    private readonly IServiceProvider _services;

    private readonly Color _accentDark;
    private readonly Color _accentSoft;
    private readonly string _roleLabel;

    private readonly Label _lblPageTitle    = new();
    private readonly Label _lblPageSubtitle = new();
    private readonly Label _lblHeaderRight  = new();

    private readonly Panel _pageFacilities;
    private readonly Panel _pageBookings;
    private readonly Panel _pageNotifications;
    private readonly Panel? _pageMaintenance;

    private readonly DataGridView _gridFacilities    = Styles.NiceGrid();
    private readonly DataGridView _gridBookings      = Styles.NiceGrid();
    private readonly DataGridView _gridNotifications = Styles.NiceGrid();

    private readonly Button _btnRefreshFacilities, _btnAddFacility, _btnDeactivateFacility;
    private readonly Button _btnRefreshBookings, _btnNewBooking, _btnCancelBooking;
    private readonly Button _btnApproveBooking, _btnRejectBooking;
    private readonly Button _btnRefreshNotifications, _btnMarkAllRead;

    private readonly Button _navFacilities, _navBookings, _navNotifications;
    private readonly Button? _navMaintenance;
    private Button _navSelected;

    private readonly System.Windows.Forms.Timer _pulseTimer;
    private int _pulseTicksLeft;

    private List<FacilityResponse>  _facilitiesCache    = new();
    private List<BookingResponse>   _bookingsCache      = new();
    private List<NotificationItem>  _notificationsCache = new();

    public MainForm(ApiClient api, UserSession session, NotificationPoller poller, IServiceProvider services)
    {
        _api      = api;
        _session  = session;
        _poller   = poller;
        _services = services;

        var accent = RoleAccent(_session.Role);
        _accentDark = accent.Dark;
        _accentSoft = accent.Soft;
        _roleLabel  = accent.Label;

        Text          = $"CampusBooking - {_session.DisplayName}";
        Size          = new Size(1280, 800);
        MinimumSize   = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = MainTheme.Body;
        BackColor     = MainTheme.Background;

        // Buttons (created up-front so BuildPage can wire them in).
        _btnRefreshFacilities  = Styles.SecondaryButton("Refresh", 100);
        _btnAddFacility        = Styles.PrimaryButton("Add", 90);
        _btnDeactivateFacility = Styles.SecondaryButton("Deactivate", 110);
        _btnAddFacility.Visible        = _session.IsManager;
        _btnDeactivateFacility.Visible = _session.IsManager;
        _btnRefreshFacilities.Click  += async (_, _) => await LoadFacilitiesAsync();
        _btnAddFacility.Click        += BtnAddFacility_Click;
        _btnDeactivateFacility.Click += BtnDeactivateFacility_Click;

        var canBook = _session.Role is "Student" or "Staff";
        _btnRefreshBookings = Styles.SecondaryButton("Refresh", 100);
        _btnNewBooking      = Styles.PrimaryButton("New", 90);
        _btnCancelBooking   = Styles.SecondaryButton("Cancel", 90);
        _btnApproveBooking  = Styles.PrimaryButton("Approve", 100);
        _btnRejectBooking   = Styles.SecondaryButton("Reject", 90);
        _btnNewBooking.Visible     = canBook;
        _btnCancelBooking.Visible  = canBook;
        _btnApproveBooking.Visible = _session.IsManager;
        _btnRejectBooking.Visible  = _session.IsManager;
        _btnRefreshBookings.Click += async (_, _) => await LoadBookingsAsync();
        _btnNewBooking.Click      += BtnNewBooking_Click;
        _btnCancelBooking.Click   += BtnCancelBooking_Click;
        _btnApproveBooking.Click  += BtnApproveBooking_Click;
        _btnRejectBooking.Click   += BtnRejectBooking_Click;

        _btnRefreshNotifications = Styles.SecondaryButton("Refresh", 100);
        _btnMarkAllRead          = Styles.PrimaryButton("Mark All Read", 130);
        _btnRefreshNotifications.Click += async (_, _) => await LoadNotificationsAsync();
        _btnMarkAllRead.Click          += BtnMarkAllRead_Click;

        // Grids and column setup.
        AddCols(_gridFacilities,
            ("Id", "Id", false), ("Name", "Facility", true), ("Type", "Type", true),
            ("Capacity", "Capacity", true), ("Location", "Location", true), ("Active", "Active", true));
        _gridFacilities.CellDoubleClick += GridFacilities_DoubleClick;

        AddCols(_gridBookings,
            ("Id", "Id", false), ("Facility", "Facility", true), ("User", "User", true),
            ("Date", "Date", true), ("TimeSlot", "Hour", true), ("Status", "Status", true));
        _gridBookings.CellDoubleClick += GridBookings_DoubleClick;
        // Color-code the Status cell so confirmed/pending/cancelled are visually distinct at a glance.
        _gridBookings.CellFormatting  += GridBookings_CellFormatting;

        AddCols(_gridNotifications,
            ("Id", "Id", false), ("Kind", "Kind", true), ("Message", "Message", true),
            ("Read", "Read", true), ("Time", "Time", true));
        _gridNotifications.CellDoubleClick += GridNotifications_DoubleClick;
        _gridNotifications.CellFormatting  += GridNotifications_CellFormatting;

        // Pages — each is a card with a 2-row TableLayoutPanel inside (toolbar + grid).
        _pageFacilities    = BuildPage(_gridFacilities,
            _btnRefreshFacilities, _btnAddFacility, _btnDeactivateFacility);
        _pageBookings      = BuildPage(_gridBookings,
            _btnRefreshBookings, _btnNewBooking, _btnCancelBooking, _btnApproveBooking, _btnRejectBooking);
        _pageNotifications = BuildPage(_gridNotifications,
            _btnRefreshNotifications, _btnMarkAllRead);
        _pageFacilities.Visible    = false;
        _pageBookings.Visible      = false;
        _pageNotifications.Visible = false;

        if (_session.IsManager || _session.Role == "MaintenancePersonnel")
        {
            _pageMaintenance = BuildMaintenancePage();
            _pageMaintenance.Visible = false;
        }

        _navFacilities    = MakeNavButton("Facilities");
        _navBookings      = MakeNavButton("Bookings");
        _navNotifications = MakeNavButton("Notifications");
        if (_session.IsManager || _session.Role == "MaintenancePersonnel")
            _navMaintenance = MakeNavButton("Maintenance");

        _navSelected = _navFacilities;

        // Compose the form after all child controls exist.
        BuildLayout();

        _navFacilities.Click    += (_, _) => SwitchTo(_navFacilities, _pageFacilities,
            "Facilities", "Browse the rooms and venues you can book.");
        _navBookings.Click      += (_, _) => SwitchTo(_navBookings, _pageBookings,
            "Bookings", _session.IsManager ? "All bookings across the campus." : "Your reservations.");
        _navNotifications.Click += (_, _) => SwitchTo(_navNotifications, _pageNotifications,
            "Notifications", "Updates about your bookings and facilities.");
        if (_navMaintenance is not null && _pageMaintenance is not null)
        {
            _navMaintenance.Click += (_, _) => SwitchTo(_navMaintenance, _pageMaintenance,
                "Maintenance", "Report and track maintenance issues.");
        }

        _pulseTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _pulseTimer.Tick += PulseTimer_Tick;

        _poller.OnNotification += HandleNewNotification;

        Load += async (_, _) =>
        {
            SwitchTo(_navFacilities, _pageFacilities,
                "Facilities", "Browse the rooms and venues you can book.");
            _poller.Start();
            await LoadFacilitiesAsync();
            await LoadBookingsAsync();
            await LoadNotificationsAsync();
        };
    }

    // Top-level: 2-column TableLayoutPanel (sidebar + content). Each child is a single
    // Fill descendant in its cell, which is the layout pattern WinForms is reliable with.
    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = MainTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildContent(), 1, 0);

        Controls.Add(root);
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.PrimaryDark };

        // Inner column has rows: brand, divider, user, divider, nav (fill), logout.
        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 6,
            BackColor   = Color.Transparent
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

        var brand = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        brand.Controls.Add(new Label
        {
            Text      = "CampusBooking",
            Font      = MainTheme.Heading,
            ForeColor = Color.White,
            AutoSize  = true,
            Location  = new Point(22, 22)
        });
        brand.Controls.Add(new Label
        {
            Text      = "Facility booking & maintenance",
            Font      = MainTheme.Small,
            ForeColor = MainTheme.PrimaryLight,
            AutoSize  = true,
            Location  = new Point(22, 56)
        });
        inner.Controls.Add(brand, 0, 0);

        inner.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.PrimaryLight }, 0, 1);

        var userBlock = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        userBlock.Controls.Add(new Label
        {
            Text      = "Signed in as",
            Font      = MainTheme.Small,
            ForeColor = MainTheme.PrimaryLight,
            AutoSize  = true,
            Location  = new Point(22, 12)
        });
        userBlock.Controls.Add(new Label
        {
            Text      = _session.DisplayName,
            Font      = MainTheme.BodyBold,
            ForeColor = Color.White,
            AutoSize  = true,
            Location  = new Point(22, 32)
        });
        userBlock.Controls.Add(new Label
        {
            Text      = _roleLabel,
            Font      = MainTheme.Small,
            ForeColor = _accentDark,
            AutoSize  = true,
            Location  = new Point(22, 56)
        });
        inner.Controls.Add(userBlock, 0, 2);

        inner.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.PrimaryLight }, 0, 3);

        var navHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 12, 6, 12), BackColor = Color.Transparent };
        var navFlow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Width         = 208
        };
        navFlow.Controls.Add(_navFacilities);
        navFlow.Controls.Add(_navBookings);
        navFlow.Controls.Add(_navNotifications);
        if (_navMaintenance is not null) navFlow.Controls.Add(_navMaintenance);
        navHost.Controls.Add(navFlow);
        inner.Controls.Add(navHost, 0, 4);

        var logoutHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 20), BackColor = Color.Transparent };
        var btnLogout = Styles.SecondaryButton("Logout", 180);
        btnLogout.Dock      = DockStyle.Fill;
        btnLogout.BackColor = MainTheme.PrimaryDark;
        btnLogout.ForeColor = Color.White;
        btnLogout.FlatAppearance.BorderColor         = MainTheme.PrimaryLight;
        btnLogout.FlatAppearance.MouseOverBackColor  = MainTheme.Primary;
        btnLogout.Click += BtnLogout_Click;
        logoutHost.Controls.Add(btnLogout);
        inner.Controls.Add(logoutHost, 0, 5);

        // Accent strip docked left, added LAST so it paints over the inner table's left edge.
        var accentStrip = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 6,
            BackColor = _accentDark
        };

        sidebar.Controls.Add(inner);
        sidebar.Controls.Add(accentStrip);
        return sidebar;
    }

    private TableLayoutPanel BuildContent()
    {
        var content = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 2,
            BackColor   = MainTheme.Background
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        content.Controls.Add(BuildHeaderStrip(), 0, 0);
        content.Controls.Add(BuildPageHost(),    0, 1);
        return content;
    }

    private Panel BuildHeaderStrip()
    {
        var strip = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.Surface };

        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 2,
            BackColor   = Color.Transparent
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));

        var leftCell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 0, 0), BackColor = Color.Transparent };
        _lblPageTitle.Font      = MainTheme.Heading;
        _lblPageTitle.ForeColor = MainTheme.TextPrimary;
        _lblPageTitle.AutoSize  = true;
        _lblPageTitle.Location  = new Point(0, 0);
        _lblPageSubtitle.Font      = MainTheme.Small;
        _lblPageSubtitle.ForeColor = MainTheme.TextSecondary;
        _lblPageSubtitle.AutoSize  = true;
        _lblPageSubtitle.Location  = new Point(0, 30);
        leftCell.Controls.Add(_lblPageTitle);
        leftCell.Controls.Add(_lblPageSubtitle);
        inner.Controls.Add(leftCell, 0, 0);

        var rightCell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 24, 0), BackColor = Color.Transparent };
        _lblHeaderRight.Dock      = DockStyle.Fill;
        _lblHeaderRight.Font      = MainTheme.Small;
        _lblHeaderRight.ForeColor = MainTheme.TextSecondary;
        _lblHeaderRight.TextAlign = ContentAlignment.MiddleRight;
        rightCell.Controls.Add(_lblHeaderRight);
        inner.Controls.Add(rightCell, 1, 0);

        var accentBar = new Panel { Dock = DockStyle.Fill, BackColor = _accentDark };
        inner.Controls.Add(accentBar, 0, 1);
        inner.SetColumnSpan(accentBar, 2);

        strip.Controls.Add(inner);
        return strip;
    }

    private Panel BuildPageHost()
    {
        // Single Fill child of the content layout's bottom row. Each page panel is Dock=Fill;
        // multiple Fill siblings overlap with the same bounds, so toggling Visible swaps them.
        var host = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.Background, Padding = new Padding(16) };
        host.Controls.Add(_pageFacilities);
        host.Controls.Add(_pageBookings);
        host.Controls.Add(_pageNotifications);
        if (_pageMaintenance is not null) host.Controls.Add(_pageMaintenance);
        return host;
    }

    private static Panel BuildPage(DataGridView grid, params Button[] buttons)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.Surface };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(MainTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 2,
            BackColor   = Color.Transparent,
            Padding     = new Padding(16, 12, 16, 16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0, 8, 0, 8)
        };
        foreach (var btn in buttons)
        {
            btn.Margin = new Padding(0, 0, 8, 0);
            toolbar.Controls.Add(btn);
        }
        layout.Controls.Add(toolbar, 0, 0);

        grid.Dock = DockStyle.Fill;
        layout.Controls.Add(grid, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Panel BuildMaintenancePage()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.Surface, Padding = new Padding(40) };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(MainTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var inner = new Panel { Dock = DockStyle.Fill, BackColor = MainTheme.Surface };

        var title = new Label
        {
            Text      = "Maintenance Window",
            Font      = MainTheme.Subheading,
            ForeColor = MainTheme.TextPrimary,
            AutoSize  = true
        };
        var explainer = new Label
        {
            Text      = "Open the maintenance dialog to file a new issue or update existing ones.",
            Font      = MainTheme.Small,
            ForeColor = MainTheme.TextSecondary,
            AutoSize  = true
        };
        var btnOpen = Styles.PrimaryButton("Open Maintenance Window", 240);
        btnOpen.Click += (_, _) =>
        {
            var form = _services.GetRequiredService<MaintenanceForm>();
            form.ShowDialog(this);
        };

        // Center the three controls inside `inner` whenever it resizes.
        inner.Resize += (_, _) =>
        {
            title.Location     = new Point((inner.Width - title.Width) / 2, inner.Height / 2 - 64);
            explainer.Location = new Point((inner.Width - explainer.Width) / 2, inner.Height / 2 - 32);
            btnOpen.Location   = new Point((inner.Width - btnOpen.Width) / 2, inner.Height / 2 + 8);
        };

        inner.Controls.Add(title);
        inner.Controls.Add(explainer);
        inner.Controls.Add(btnOpen);
        card.Controls.Add(inner);
        return card;
    }

    private static Button MakeNavButton(string text)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = 204,
            Height    = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font      = MainTheme.BodyBold,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(18, 0, 0, 0),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 2, 0, 2),
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize         = 0;
        btn.FlatAppearance.MouseOverBackColor = MainTheme.Primary;
        return btn;
    }

    private static (Color Dark, Color Soft, string Label) RoleAccent(string role) => role switch
    {
        "Student"              => (MainTheme.Info,    MainTheme.InfoSoft,    "Student"),
        "Staff"                => (MainTheme.Success, MainTheme.SuccessSoft, "Staff"),
        "FacilityManager"      => (MainTheme.Accent,  Color.FromArgb(0xFF, 0xF1, 0xB8), "Facility Manager"),
        "MaintenancePersonnel" => (MainTheme.Danger,  MainTheme.DangerSoft,  "Maintenance"),
        _                      => (MainTheme.Neutral, MainTheme.NeutralSoft, role)
    };

    private static void AddCols(DataGridView grid, params (string name, string header, bool visible)[] cols)
    {
        foreach (var (name, header, visible) in cols)
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Visible = visible });
    }

    private void SwitchTo(Button navBtn, Panel page, string title, string subtitle)
    {
        _pageFacilities.Visible    = false;
        _pageBookings.Visible      = false;
        _pageNotifications.Visible = false;
        if (_pageMaintenance is not null) _pageMaintenance.Visible = false;

        page.Visible = true;
        page.BringToFront();

        _lblPageTitle.Text    = title;
        _lblPageSubtitle.Text = subtitle;
        _lblHeaderRight.Text  = HeaderRightText(page);

        _navSelected = navBtn;
        ApplyNavStyles();
    }

    private string HeaderRightText(Panel page)
    {
        if (page == _pageBookings)      return $"{_bookingsCache.Count} booking(s)";
        if (page == _pageFacilities)    return $"{_facilitiesCache.Count} facility(s)";
        if (page == _pageNotifications)
        {
            var unread = _notificationsCache.Count(n => !n.IsRead);
            return unread > 0 ? $"{unread} unread" : "All caught up";
        }
        return string.Empty;
    }

    private void ApplyNavStyles()
    {
        foreach (var btn in NavButtons())
            btn.BackColor = (btn == _navSelected) ? MainTheme.Primary : Color.Transparent;
    }

    private IEnumerable<Button> NavButtons()
    {
        yield return _navFacilities;
        yield return _navBookings;
        yield return _navNotifications;
        if (_navMaintenance is not null) yield return _navMaintenance;
    }

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            _facilitiesCache = await _api.GetFacilitiesAsync(includeInactive: _session.IsManager);
            _gridFacilities.Rows.Clear();
            foreach (var f in _facilitiesCache)
                _gridFacilities.Rows.Add(f.Id, f.Name, f.FacilityTypeName, f.Capacity, f.Location,
                    f.IsActive ? "Active" : "Inactive");
            if (_pageFacilities.Visible) _lblHeaderRight.Text = HeaderRightText(_pageFacilities);
        }
        catch (Exception ex) { ShowError("Could not load facilities", ex); }
    }

    private async Task LoadBookingsAsync()
    {
        try
        {
            _bookingsCache = _session.IsManager
                ? await _api.GetAllBookingsAsync()
                : await _api.GetMyBookingsAsync();

            _gridBookings.Rows.Clear();
            foreach (var b in _bookingsCache)
                _gridBookings.Rows.Add(b.Id, b.FacilityName, b.UserDisplayName,
                    b.Date.ToString("yyyy-MM-dd"), $"{b.TimeSlot:00}:00", b.Status.ToString());

            if (_pageBookings.Visible) _lblHeaderRight.Text = HeaderRightText(_pageBookings);
        }
        catch (Exception ex) { ShowError("Could not load bookings", ex); }
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            _notificationsCache = await _api.GetNotificationsAsync();
            _gridNotifications.Rows.Clear();
            foreach (var n in _notificationsCache)
                _gridNotifications.Rows.Add(n.Id, n.Kind, n.Message,
                    n.IsRead ? "Yes" : "No",
                    n.CreatedAtUtc.ToLocalTime().ToString("g"));

            var unread = _notificationsCache.Count(n => !n.IsRead);
            _navNotifications.Text = unread > 0 ? $"Notifications ({unread})" : "Notifications";
            if (_pageNotifications.Visible) _lblHeaderRight.Text = HeaderRightText(_pageNotifications);
        }
        catch (Exception ex) { ShowError("Could not load notifications", ex); }
    }

    private void GridBookings_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
        if (_gridBookings.Columns[e.ColumnIndex].Name != "Status") return;

        var status = e.Value?.ToString() ?? string.Empty;
        var (back, fore) = status switch
        {
            "Confirmed" or "Approved" => (MainTheme.SuccessSoft, MainTheme.Success),
            "Pending"                 => (MainTheme.WarningSoft, MainTheme.Warning),
            "Cancelled" or "Rejected" => (MainTheme.NeutralSoft, MainTheme.Neutral),
            _                         => (MainTheme.NeutralSoft, MainTheme.Neutral)
        };
        e.CellStyle!.BackColor = back;
        e.CellStyle.ForeColor  = fore;
        e.CellStyle.Font       = MainTheme.BodyBold;
    }

    private void GridNotifications_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
        if (_gridNotifications.Columns[e.ColumnIndex].Name != "Kind") return;

        var kind = e.Value?.ToString() ?? string.Empty;
        var (back, fore) = kind switch
        {
            "BookingConfirmed" or "MaintenanceResolved" => (MainTheme.SuccessSoft, MainTheme.Success),
            "BookingPending"   or "MaintenanceAssigned" => (MainTheme.WarningSoft, MainTheme.Warning),
            "BookingApproved"  or "Info"                => (MainTheme.InfoSoft,    MainTheme.Info),
            _                                            => (MainTheme.NeutralSoft, MainTheme.Neutral)
        };
        e.CellStyle!.BackColor = back;
        e.CellStyle.ForeColor  = fore;
        e.CellStyle.Font       = MainTheme.BodyBold;
    }

    private void GridFacilities_DoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var id = (int)_gridFacilities.Rows[e.RowIndex].Cells["Id"].Value;
        var facility = _facilitiesCache.FirstOrDefault(f => f.Id == id);
        if (facility is null) return;
        new FacilityDetailsForm(facility).ShowDialog(this);
    }

    private void GridBookings_DoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var id = (int)_gridBookings.Rows[e.RowIndex].Cells["Id"].Value;
        var booking = _bookingsCache.FirstOrDefault(b => b.Id == id);
        if (booking is null) return;
        new BookingDetailsForm(booking).ShowDialog(this);
    }

    private void GridNotifications_DoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var id = (int)_gridNotifications.Rows[e.RowIndex].Cells["Id"].Value;
        var item = _notificationsCache.FirstOrDefault(n => n.Id == id);
        if (item is null) return;
        new NotificationDetailsForm(item).ShowDialog(this);
    }

    private void BtnAddFacility_Click(object? sender, EventArgs e)
    {
        var dlg = _services.GetRequiredService<AddFacilityForm>();
        dlg.ShowDialog(this);
        if (dlg.FacilityCreated) _ = LoadFacilitiesAsync();
    }

    private async void BtnDeactivateFacility_Click(object? sender, EventArgs e)
    {
        if (_gridFacilities.CurrentRow is null) return;
        var id   = (int)_gridFacilities.CurrentRow.Cells["Id"].Value;
        var name = (string)_gridFacilities.CurrentRow.Cells["Name"].Value;

        if (MessageBox.Show($"Deactivate \"{name}\"?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try { await _api.DeactivateFacilityAsync(id); await LoadFacilitiesAsync(); }
        catch (Exception ex) { ShowError("Deactivate failed", ex); }
    }

    private void BtnNewBooking_Click(object? sender, EventArgs e)
    {
        var dlg = _services.GetRequiredService<CreateBookingForm>();
        dlg.ShowDialog(this);
        if (dlg.BookingCreated) _ = LoadBookingsAsync();
    }

    private async void BtnCancelBooking_Click(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;

        if (MessageBox.Show("Cancel this booking?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try { await _api.CancelBookingAsync(id); await LoadBookingsAsync(); }
        catch (Exception ex) { ShowError("Cancel failed", ex); }
    }

    private async void BtnApproveBooking_Click(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;
        try { await _api.ApproveBookingAsync(id); await LoadBookingsAsync(); }
        catch (Exception ex) { ShowError("Approve failed", ex); }
    }

    private async void BtnRejectBooking_Click(object? sender, EventArgs e)
    {
        if (_gridBookings.CurrentRow is null) return;
        var id = (int)_gridBookings.CurrentRow.Cells["Id"].Value;
        try { await _api.RejectBookingAsync(id); await LoadBookingsAsync(); }
        catch (Exception ex) { ShowError("Reject failed", ex); }
    }

    private async void BtnMarkAllRead_Click(object? sender, EventArgs e)
    {
        try { await _api.MarkAllReadAsync(); await LoadNotificationsAsync(); }
        catch (Exception ex) { ShowError("Mark all read failed", ex); }
    }

    // Re-resolve LoginForm via DI so a fresh sign-in path is wired up (clears any stale handler state).
    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        _poller.Stop();
        _session.Clear();
        var login = _services.GetRequiredService<LoginForm>();
        login.Show();
        Close();
    }

    private void HandleNewNotification(NotificationKind kind, string message)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => HandleNewNotification(kind, message))); return; }
        StartPulse();
        _ = LoadNotificationsAsync();
    }

    private void StartPulse()
    {
        _pulseTicksLeft = 12;
        _pulseTimer.Start();
    }

    // Brief flash of the Notifications nav item when a new event arrives.
    private void PulseTimer_Tick(object? sender, EventArgs e)
    {
        _pulseTicksLeft--;
        _navNotifications.BackColor = (_pulseTicksLeft % 2 == 0) ? _accentSoft : MainTheme.Primary;
        if (_pulseTicksLeft <= 0)
        {
            _pulseTimer.Stop();
            ApplyNavStyles();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _poller.Stop();
        _poller.OnNotification -= HandleNewNotification;
        _pulseTimer.Stop();
        _pulseTimer.Dispose();
        base.OnFormClosed(e);
    }

    private static void ShowError(string title, Exception ex)
        => MessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
