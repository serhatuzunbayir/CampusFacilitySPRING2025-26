using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Themes;
using CampusBooking.Shared.Dtos.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace CampusBooking.Desktop.UI.Forms;

public class LoginForm : Form
{
    private readonly ApiClient _api;
    private readonly UserSession _session;
    private readonly IServiceProvider _services;

    private readonly TextBox _txtEmail;
    private readonly TextBox _txtPassword;
    private readonly Button  _btnLogin;
    private readonly Label   _lblError;

    public LoginForm(ApiClient api, UserSession session, IServiceProvider services)
    {
        _api = api;
        _session = session;
        _services = services;

        Text            = "Login";
        Size            = new Size(400, 230);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Font            = MainTheme.BodyFont;
        BackColor       = MainTheme.Background;

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 5,
            Padding     = new Padding(24, 20, 24, 20)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        layout.Controls.Add(MakeLabel("Email:"), 0, 0);
        _txtEmail = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_txtEmail, 1, 0);

        layout.Controls.Add(MakeLabel("Password:"), 0, 1);
        _txtPassword = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill };
        layout.Controls.Add(_txtPassword, 1, 1);

        layout.Controls.Add(new Label(), 0, 2);
        _btnLogin = new Button
        {
            Text = "Login",
            Dock = DockStyle.Fill,
            BackColor = MainTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnLogin.Click += btnLogin_Click;
        layout.Controls.Add(_btnLogin, 1, 2);

        _lblError = new Label
        {
            ForeColor = MainTheme.Danger,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.SetColumnSpan(_lblError, 2);
        layout.Controls.Add(_lblError, 0, 4);

        Controls.Add(layout);
        AcceptButton = _btnLogin;
    }

    private async void btnLogin_Click(object? sender, EventArgs e)
    {
        _btnLogin.Enabled = false;
        _lblError.Text = string.Empty;

        try
        {
            var req = new LoginRequest
            {
                Email = _txtEmail.Text.Trim(),
                Password = _txtPassword.Text
            };
            var result = await _api.LoginAsync(req);

            _session.Token = result.Token;
            _session.UserId = result.UserId;
            _session.DisplayName = result.DisplayName;
            _session.Role = result.Role;

            Hide();
            var main = _services.GetRequiredService<MainForm>();
            main.FormClosed += (_, _) => Close();
            main.Show();
        }
        catch (ApiException ex)
        {
            _lblError.Text = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Invalid email or password."
                : ex.Message;
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

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight
    };
}
