using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace GrassiNotes;

internal static class EditorZoom
{
	public static void Apply(RichTextBox editor, double value)
	{
		double zoom = DocumentMetadata.NormalizeZoom(value);
		editor.LayoutTransform = zoom == 1.0
			? Transform.Identity
			: new ScaleTransform(zoom, zoom);
	}
}
