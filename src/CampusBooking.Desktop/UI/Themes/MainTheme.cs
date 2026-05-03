using System.Drawing;

namespace CampusBooking.Desktop.UI.Themes;

public static class MainTheme
{
    public static readonly Color Primary      = Color.FromArgb(0x1F, 0x4E, 0x79);
    public static readonly Color PrimaryLight = Color.FromArgb(0x42, 0x72, 0xA0);
    public static readonly Color Accent       = Color.FromArgb(0xFF, 0xC1, 0x07);
    public static readonly Color Success      = Color.FromArgb(0x28, 0xA7, 0x45);
    public static readonly Color Warning      = Color.FromArgb(0xFF, 0xA5, 0x00);
    public static readonly Color Danger       = Color.FromArgb(0xDC, 0x35, 0x45);
    public static readonly Color Background   = Color.FromArgb(0xF8, 0xF9, 0xFA);

    public static Font HeadingFont => new("Segoe UI", 14, FontStyle.Bold);
    public static Font BodyFont    => new("Segoe UI", 10);
}
