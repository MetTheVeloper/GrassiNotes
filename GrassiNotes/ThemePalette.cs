using System.Windows.Media;

namespace GrassiNotes;

public sealed class ThemePalette
{
    public Brush Window { get; init; } = Brushes.White;
    public Brush Panel { get; init; } = Brushes.White;
    public Brush SurfaceAlt { get; init; } = Brushes.White;
    public Brush Editor { get; init; } = Brushes.White;
    public Brush Border { get; init; } = Brushes.Gray;
    public Brush Text { get; init; } = Brushes.Black;
    public Brush Muted { get; init; } = Brushes.Gray;
    public Brush Hover { get; init; } = Brushes.LightGray;
    public Brush Pressed { get; init; } = Brushes.LightGray;
    public Brush Active { get; init; } = Brushes.White;
    public Brush Accent { get; init; } = Brushes.MediumPurple;
    public Brush Selection { get; init; } = Brushes.MediumPurple;
    public Brush ScrollThumb { get; init; } = Brushes.Gray;
    public Brush ScrollThumbHover { get; init; } = Brushes.DarkGray;
    public Brush Caret { get; init; } = Brushes.White;

    public static ThemePalette Dark { get; } = new()
    {
        Window = B("#151922"),
        Panel = B("#191E28"),
        SurfaceAlt = B("#1E2430"),
        Editor = B("#151922"),
        Border = B("#2D3442"),
        Text = B("#F1F3F7"),
        Muted = B("#AAB2C2"),
        Hover = B("#252C39"),
        Pressed = B("#303847"),
        Active = B("#1E2430"),
        Accent = B("#8B5CF6"),
        Selection = B("#665B3BC4"),
        ScrollThumb = B("#66768198"),
        ScrollThumbHover = B("#A08D98AB"),
        Caret = B("#FFFFFF")
    };

    public static ThemePalette Light { get; } = new()
    {
        Window = B("#F7F8FB"),
        Panel = B("#FFFFFF"),
        SurfaceAlt = B("#F2F4F8"),
        Editor = B("#FFFFFF"),
        Border = B("#DCE1E9"),
        Text = B("#171A21"),
        Muted = B("#677083"),
        Hover = B("#EEF0F5"),
        Pressed = B("#E2E6EE"),
        Active = B("#FFFFFF"),
        Accent = B("#7554E8"),
        Selection = B("#557554E8"),
        ScrollThumb = B("#667A8495"),
        ScrollThumbHover = B("#A05F6878"),
        Caret = B("#111111")
    };

    private static SolidColorBrush B(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
