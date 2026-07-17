using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GrassiNotes;

internal static class ParagraphStyleFormatter
{
	public static bool IsHeading(Paragraph paragraph) =>
		paragraph.Tag?.ToString() is "h1" or "h2" or "h3" or "h4";

	public static void ApplyNormal(Paragraph paragraph, FontFamily appFont)
	{
		paragraph.Tag = null;
		paragraph.FontFamily = appFont;
		paragraph.FontSize = 14.0;
		paragraph.FontWeight = FontWeights.Normal;
		paragraph.FontStyle = FontStyles.Normal;
		paragraph.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
	}

	public static void ApplyHeading(Paragraph paragraph, int level, FontFamily appFont)
	{
		paragraph.Tag = "h" + level;
		paragraph.FontFamily = appFont;
		MarkdownCodec.ApplyHeadingStyle(paragraph, level);
		paragraph.Margin = level switch
		{
			1 => new Thickness(0.0, 16.0, 0.0, 8.0),
			2 => new Thickness(0.0, 14.0, 0.0, 7.0),
			_ => new Thickness(0.0, 11.0, 0.0, 6.0)
		};
	}
}
