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

        Text            = "CampusBooking - Sign in";
        Size            = new Size(460, 580);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Font            = MainTheme.Body;
        BackColor       = MainTheme.Background;

        var card = new Panel
        {
            Width     = 380,
            Height    = 440,
            BackColor = MainTheme.Surface,
            Padding   = new Padding(32, 28, 32, 28)
        };
        card.Location = new Point((ClientSize.Width - card.Width) / 2,
                                  (ClientSize.Height - card.Height) / 2);
        card.Anchor = AnchorStyles.None;
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(MainTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var brand = new Label
        {
            Text      = "CampusBooking",
            Font      = MainTheme.Display,
            ForeColor = MainTheme.Primary,
            AutoSize  = true,
            Location  = new Point(32, 28)
        };
        card.Controls.Add(brand);

        var subtitle = new Label
        {
            Text      = "Sign in to your account",
            Font      = MainTheme.Small,
            ForeColor = MainTheme.TextSecondary,
            AutoSize  = true,
            Location  = new Point(32, 70)
        };
        card.Controls.Add(subtitle);

        var divider = new Panel
        {
            Height    = 1,
            Width     = 316,
            BackColor = MainTheme.Divider,
            Location  = new Point(32, 100)
        };
        card.Controls.Add(divider);

        var emailLabel = Styles.FieldLabel("Email");
        emailLabel.Location = new Point(32, 124);
        card.Controls.Add(emailLabel);

        _txtEmail = new TextBox
        {
            Location  = new Point(32, 146),
            Width     = 316,
            Font      = MainTheme.Body,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_txtEmail);

        var passwordLabel = Styles.FieldLabel("Password");
        passwordLabel.Location = new Point(32, 188);
        card.Controls.Add(passwordLabel);

        _txtPassword = new TextBox
        {
            Location     = new Point(32, 210),
            Width        = 316,
            PasswordChar = '●',
            Font         = MainTheme.Body,
            BorderStyle  = BorderStyle.FixedSingle
        };
        _txtPassword.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnLogin_Click(_btnLogin!, EventArgs.Empty);
            }
        };
        card.Controls.Add(_txtPassword);

        _btnLogin = Styles.PrimaryButton("Sign in", 300);
        _btnLogin.Location = new Point(40, 264);
        _btnLogin.Click += btnLogin_Click;
        card.Controls.Add(_btnLogin);

        _lblError = new Label
        {
            ForeColor = MainTheme.Danger,
            Font      = MainTheme.Small,
            AutoSize  = false,
            Width     = 316,
            Height    = 40,
            Location  = new Point(32, 312),
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(_lblError);

        Controls.Add(card);
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
}
