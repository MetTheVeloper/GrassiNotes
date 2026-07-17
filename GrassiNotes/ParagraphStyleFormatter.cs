using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GrassiNotes;

internal static class ParagraphStyleFormatter
{
	public static bool IsHeading(Paragraph paragraph) =>
		paragraph.Tag?.ToString() is "h1" or "h2" or "h3" or "h4";

	public static void ApplyNormal(Paragraph paragraph, FontFamily fontFamily)
	{
		paragraph.Tag = null;
		paragraph.FontFamily = fontFamily;
		paragraph.FontSize = 14.0;
		paragraph.FontWeight = FontWeights.Normal;
		paragraph.FontStyle = FontStyles.Normal;
		paragraph.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);

		TextRange range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
		range.ApplyPropertyValue(TextElement.FontFamilyProperty, fontFamily);
		range.ApplyPropertyValue(TextElement.FontSizeProperty, 14.0);
		range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
		range.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
	}
}
