using System;
using System.Windows;
using System.Windows.Documents;

namespace GrassiNotes;

public static class DocumentMetadata
{
	private const double MinimumZoom = 0.6;
	private const double MaximumZoom = 2.2;

	public static readonly DependencyProperty ZoomProperty =
		DependencyProperty.RegisterAttached(
			"Zoom",
			typeof(double),
			typeof(DocumentMetadata),
			new FrameworkPropertyMetadata(1.0));

	public static double GetZoom(DependencyObject element)
	{
		double zoom = (double)element.GetValue(ZoomProperty);
		return IsValidZoom(zoom) ? zoom : 1.0;
	}

	public static void SetZoom(DependencyObject element, double value)
	{
		element.SetValue(ZoomProperty, NormalizeZoom(value));
	}

	public static double NormalizeZoom(double value) =>
		IsValidZoom(value) ? Math.Round(value, 2) : 1.0;

	private static bool IsValidZoom(double value) =>
		double.IsFinite(value) &&
		value >= MinimumZoom &&
		value <= MaximumZoom;
}
