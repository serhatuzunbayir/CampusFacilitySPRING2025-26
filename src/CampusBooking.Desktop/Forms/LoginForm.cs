using CampusBooking.Desktop.Services;

namespace CampusBooking.Desktop.Forms;

/// <summary>
/// The application's entry screen. Collects API URL, email and password,
/// calls the login endpoint, and opens MainForm on success.
/// </summary>
public class LoginForm : Form
{
    // ── Controls ─────────────────────────────────────────────────────────────
    private readonly TextBox _txtApiUrl;
    private readonly TextBox _txtEmail;
    private readonly TextBox _txtPassword;
    private readonly Button  _btnLogin;
    private readonly Label   _lblError;

    public LoginForm()
    {
        // ── Window properties ────────────────────────────────────────────────
        Text             = "CampusBooking — Login";
        Size             = new Size(420, 280);
        StartPosition    = FormStartPosition.CenterScreen;
        FormBorderStyle  = FormBorderStyle.FixedSingle;
        MaximizeBox      = false;
        Font             = new Font("Segoe UI", 10f);

        // ── Layout: 2-column grid ────────────────────────────────────────────
        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 6,
            Padding     = new Padding(24, 20, 24, 20)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        // Row heights: label-rows 36px, button row 40px, error row 30px
        for (int i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // spacer
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // error

        // Row 0 — API URL
        layout.Controls.Add(MakeLabel("API URL:"), 0, 0);
        _txtApiUrl = new TextBox { Text = "http://localhost:5279", Dock = DockStyle.Fill };
        layout.Controls.Add(_txtApiUrl, 1, 0);

        // Row 1 — Email
        layout.Controls.Add(MakeLabel("Email:"), 0, 1);
        _txtEmail = new TextBox { Text = "admin@campus.local", Dock = DockStyle.Fill };
        layout.Controls.Add(_txtEmail, 1, 1);

        // Row 2 — Password
        layout.Controls.Add(MakeLabel("Password:"), 0, 2);
        _txtPassword = new TextBox { Text = "Admin!123", PasswordChar = '*', Dock = DockStyle.Fill };
        layout.Controls.Add(_txtPassword, 1, 2);

        // Row 3 — Login button
        layout.Controls.Add(new Label(), 0, 3);
        _btnLogin = new Button { Text = "Login", Dock = DockStyle.Fill };
        _btnLogin.Click += BtnLogin_ClickAsync;
        layout.Controls.Add(_btnLogin, 1, 3);

        // Row 5 — Error message
        _lblError = new Label { ForeColor = Color.Crimson, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        layout.SetColumnSpan(_lblError, 2);
        layout.Controls.Add(_lblError, 0, 5);

        Controls.Add(layout);
        AcceptButton = _btnLogin; // Enter key triggers login
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the entered credentials via the API. On success stores the
    /// session and opens the main application window.
    /// </summary>
    private async void BtnLogin_ClickAsync(object? sender, EventArgs e)
    {
        _btnLogin.Enabled = false;
        _lblError.Text    = string.Empty;

        try
        {
            var baseUrl = _txtApiUrl.Text.TrimEnd('/') + "/";
            var client  = new ApiClient(baseUrl);
            var result  = await client.LoginAsync(_txtEmail.Text.Trim(), _txtPassword.Text);

            if (result is null)
            {
                _lblError.Text = "Invalid email or password.";
                return;
            }

            // Persist session so all forms can read current user info
            SessionState.Token       = result.Token;
            SessionState.UserId      = result.UserId;
            SessionState.DisplayName = result.DisplayName;
            SessionState.Role        = result.Role;
            client.SetToken(result.Token);

            // Open main window; close login when main closes
            Hide();
            var main = new MainForm(client);
            main.FormClosed += (_, _) => Close();
            main.Show();
        }
        catch (Exception ex)
        {
            _lblError.Text = $"Connection error: {ex.Message}";
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a right-aligned label for the form grid.</summary>
    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight
    };
}
