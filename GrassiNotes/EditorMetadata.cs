using System.Windows;

namespace GrassiNotes;

public static class EditorMetadata
{
    public static readonly DependencyProperty ExplicitDirectionProperty =
        DependencyProperty.RegisterAttached(
            "ExplicitDirection",
            typeof(string),
            typeof(EditorMetadata),
            new FrameworkPropertyMetadata("Auto", FrameworkPropertyMetadataOptions.Inherits));

    public static string GetExplicitDirection(DependencyObject element) =>
        (string)element.GetValue(ExplicitDirectionProperty);

    public static void SetExplicitDirection(DependencyObject element, string value) =>
        element.SetValue(ExplicitDirectionProperty, value);
}
