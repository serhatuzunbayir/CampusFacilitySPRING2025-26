using System.Drawing;

namespace CampusBooking.Desktop.UI.Themes;

public static class MainTheme
{
    public static readonly Color Primary       = Color.FromArgb(0x1F, 0x4E, 0x79);
    public static readonly Color PrimaryDark   = Color.FromArgb(0x14, 0x36, 0x55);
    public static readonly Color PrimaryLight  = Color.FromArgb(0x42, 0x72, 0xA0);
    public static readonly Color Accent        = Color.FromArgb(0xFF, 0xC1, 0x07);
    public static readonly Color AccentDark    = Color.FromArgb(0xCB, 0x99, 0x06);

    public static readonly Color Background    = Color.FromArgb(0xF4, 0xF6, 0xF9);
    public static readonly Color Surface       = Color.White;
    public static readonly Color SurfaceAlt    = Color.FromArgb(0xEE, 0xF2, 0xF7);
    public static readonly Color Border        = Color.FromArgb(0xD7, 0xDF, 0xE8);
    public static readonly Color Divider       = Color.FromArgb(0xE5, 0xEB, 0xF1);

    public static readonly Color TextPrimary   = Color.FromArgb(0x1A, 0x22, 0x2E);
    public static readonly Color TextSecondary = Color.FromArgb(0x55, 0x60, 0x70);
    public static readonly Color TextMuted     = Color.FromArgb(0x8A, 0x95, 0xA5);

    public static readonly Color Success       = Color.FromArgb(0x19, 0x8C, 0x4F);
    public static readonly Color SuccessSoft   = Color.FromArgb(0xDE, 0xF3, 0xE6);
    public static readonly Color Warning       = Color.FromArgb(0xE6, 0x91, 0x09);
    public static readonly Color WarningSoft   = Color.FromArgb(0xFD, 0xEF, 0xCC);
    public static readonly Color Danger        = Color.FromArgb(0xC8, 0x2C, 0x3D);
    public static readonly Color DangerSoft    = Color.FromArgb(0xF8, 0xD9, 0xDD);
    public static readonly Color Info          = Color.FromArgb(0x16, 0x73, 0xB1);
    public static readonly Color InfoSoft      = Color.FromArgb(0xD7, 0xE9, 0xF5);
    public static readonly Color Neutral       = Color.FromArgb(0x6B, 0x77, 0x85);
    public static readonly Color NeutralSoft   = Color.FromArgb(0xEA, 0xEE, 0xF3);

    public static Font Display      => new("Segoe UI", 22, FontStyle.Bold);
    public static Font Heading      => new("Segoe UI", 14, FontStyle.Bold);
    public static Font Subheading   => new("Segoe UI", 11, FontStyle.Bold);
    public static Font Body         => new("Segoe UI", 10);
    public static Font BodyBold     => new("Segoe UI", 10, FontStyle.Bold);
    public static Font Small        => new("Segoe UI", 9);
    public static Font Mono         => new("Consolas", 9);

    public static Font HeadingFont  => Heading;
    public static Font BodyFont     => Body;

    public static Color BlendColors(Color a, Color b, double factor)
    {
        if (factor < 0) factor = 0;
        if (factor > 1) factor = 1;
        int r = (int)Math.Round(a.R + (b.R - a.R) * factor);
        int g = (int)Math.Round(a.G + (b.G - a.G) * factor);
        int bl = (int)Math.Round(a.B + (b.B - a.B) * factor);
        return Color.FromArgb(r, g, bl);
    }
}
