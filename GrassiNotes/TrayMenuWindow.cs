using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace GrassiNotes;

public sealed class TrayMenuWindow : Window
{
    private readonly Action _show;
    private readonly Action _toggleTopmost;
    private readonly Action _exit;
    private readonly FontFamily _appFont;
    private Border _root = null!;
    private StackPanel _panel = null!;
    private Button _topmostButton = null!;
    private ThemePalette _theme;
    private bool _topmost;

    public TrayMenuWindow(ThemePalette theme, bool topmost, FontFamily appFont, Action show, Action toggleTopmost, Action exit)
    {
        _theme = theme;
        _topmost = topmost;
        _appFont = appFont;
        _show = show;
        _toggleTopmost = toggleTopmost;
        _exit = exit;
        Width = 276;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        FontFamily = appFont;
        Deactivated += (_, _) => Close();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
        BuildUi();
        ApplyTheme(theme);
    }

    private void BuildUi()
    {
        _panel = new StackPanel { Margin = new Thickness(6) };
        _root = new Border
        {
            Child = _panel,
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 4, Opacity = .28 }
        };
        Content = _root;
        _panel.Children.Add(MenuButton("\uE8A7", "Show GrassiNotes", "Ctrl+Alt+Shift+G", () => { Close(); _show(); }));
        _topmostButton = MenuButton("\uE840", "Always on top", "Ctrl+Alt+Shift+T", () =>
        {
            _toggleTopmost();
            _topmost = !_topmost;
            SetTopmost(_topmost);
        });
        _panel.Children.Add(_topmostButton);
        _panel.Children.Add(new Border { Height = 1, Margin = new Thickness(9, 5, 9, 5) });
        _panel.Children.Add(MenuButton("\uE8BB", "Exit", "Ctrl+Alt+Shift+Q", () => { Close(); _exit(); }));
    }

    private Button MenuButton(string glyph, string text, string shortcut, Action action)
    {
        var row = new Grid { Margin = new Thickness(7, 0, 9, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(31) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var icon = new TextBlock
        {
            Tag = "Icon",
            Text = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        row.Children.Add(icon);
        var label = new TextBlock { Text = text, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(label, 1); row.Children.Add(label);
        var keys = new TextBlock { Tag = "Muted", Text = shortcut, FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
        Grid.SetColumn(keys, 2); row.Children.Add(keys);
        var button = new Button
        {
            Height = 38,
            Content = row,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Cursor = Cursors.Hand,
            Template = ButtonTemplate()
        };
        button.Click += (_, _) => action();
        button.MouseEnter += (_, _) => button.Background = _theme.Hover;
        button.MouseLeave += (_, _) => button.Background = Brushes.Transparent;
        button.PreviewMouseLeftButtonDown += (_, _) => button.Background = _theme.Pressed;
        return button;
    }

    private static ControlTemplate ButtonTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }

    public void SetTopmost(bool value)
    {
        _topmost = value;
        if (_topmostButton.Content is Grid grid && grid.Children.Count > 1 && grid.Children[1] is TextBlock label)
            label.Text = value ? "Always on top   ✓" : "Always on top";
    }

    public void ApplyTheme(ThemePalette theme)
    {
        _theme = theme;
        _root.Background = theme.SurfaceAlt;
        _root.BorderBrush = theme.Border;
        foreach (Button button in Children<Button>(_panel))
        {
            button.Background = Brushes.Transparent;
            button.Foreground = theme.Text;
            foreach (TextBlock text in Children<TextBlock>(button))
                text.Foreground = text.Tag as string == "Muted" ? theme.Muted : theme.Text;
        }
        foreach (Border line in Children<Border>(_panel))
            if (Math.Abs(line.Height - 1) < .01) line.Background = theme.Border;
        SetTopmost(_topmost);
    }

    private static IEnumerable<T> Children<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Children<T>(child)) yield return nested;
        }
    }

    public void ShowAtCursor()
    {
        System.Drawing.Point point = System.Windows.Forms.Cursor.Position;
        Rect work = SystemParameters.WorkArea;
        Left = Math.Min(work.Right - Width - 8, Math.Max(work.Left + 8, point.X - Width + 12));
        Top = Math.Min(work.Bottom - 200, Math.Max(work.Top + 8, point.Y - 200));
        Show();
        Activate();
        Top = Math.Min(work.Bottom - ActualHeight - 8, Math.Max(work.Top + 8, point.Y - ActualHeight - 8));
    }
}
