using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GrassiNotes;

internal enum ParagraphDirection
{
	LeftToRight,
	RightToLeft
}

internal sealed class EditorFormattingController
{
	private readonly RichTextBox _editor;
	private readonly FontFamily _appFont;

	public EditorFormattingController(RichTextBox editor, FontFamily appFont)
	{
		_editor = editor;
		_appFont = appFont;
	}

	public void ApplyDirection(ParagraphDirection direction)
	{
		List<Paragraph> paragraphs = SelectedParagraphs().Distinct().ToList();
		if (paragraphs.Count == 0 && _editor.CaretPosition.Paragraph is Paragraph current)
			paragraphs.Add(current);

		foreach (Paragraph paragraph in paragraphs)
			ApplyDirectionToParagraph(paragraph, direction);

		_editor.Focus();
	}

	public void ApplyVisualAlignment(TextAlignment alignment)
	{
		List<Paragraph> paragraphs = SelectedParagraphs().Distinct().ToList();
		if (paragraphs.Count == 0 && _editor.CaretPosition.Paragraph is Paragraph current)
			paragraphs.Add(current);

		foreach (Paragraph paragraph in paragraphs)
		{
			TextAlignment logicalAlignment =
				LogicalAlignmentForVisual(paragraph.FlowDirection, alignment);
			paragraph.SetValue(Block.TextAlignmentProperty, logicalAlignment);

			System.Windows.Documents.List? list = FindOwningList(paragraph);
			if (list != null)
			{
				list.SetValue(Block.TextAlignmentProperty, logicalAlignment);
				NormalizeListGeometry(list);
			}
		}

		_editor.Focus();
	}

	public void ExecuteInlineCommand(RoutedUICommand command)
	{
		command.Execute(null, _editor);
		_editor.Focus();
	}

	public void ApplyTextColor(Brush brush)
	{
		_editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
		_editor.Focus();
	}

	public void ApplyAutomaticTextColor(Brush themeTextBrush)
	{
		_editor.Selection.ApplyPropertyValue(
			TextElement.ForegroundProperty,
			themeTextBrush);
		_editor.Focus();
	}

	public void ToggleList(RoutedUICommand command)
	{
		ParagraphDirection direction = DirectionAtCaret();
		command.Execute(null, _editor);

		List<Paragraph> paragraphs = SelectedParagraphs().Distinct().ToList();
		if (paragraphs.Count == 0 && _editor.CaretPosition.Paragraph is Paragraph current)
			paragraphs.Add(current);

		foreach (Paragraph paragraph in paragraphs)
			ApplyDirectionToParagraph(paragraph, direction);

		_editor.Focus();
	}

	public void InsertParagraphBreak()
	{
		Paragraph? before = _editor.CaretPosition.Paragraph;
		if (before == null)
		{
			EditingCommands.EnterParagraphBreak.Execute(null, _editor);
			return;
		}

		ParagraphDirection direction = DirectionOf(before);
		bool resetHeading = ParagraphStyleFormatter.IsHeading(before);

		EditingCommands.EnterParagraphBreak.Execute(null, _editor);

		Paragraph? after = _editor.CaretPosition.Paragraph;
		if (after == null) return;

		if (resetHeading)
		{
			ParagraphStyleFormatter.ApplyNormal(after, _appFont);
			ApplyNormalTypingProperties();
		}

		ApplyDirectionToParagraph(after, direction);
	}

	public void ApplyHeading(int level)
	{
		foreach (Paragraph paragraph in SelectedParagraphs())
		{
			ParagraphDirection direction = DirectionOf(paragraph);
			if (level == 0)
				ParagraphStyleFormatter.ApplyNormal(paragraph, _appFont);
			else
				ParagraphStyleFormatter.ApplyHeading(paragraph, level, _appFont);

			ApplyDirectionToParagraph(paragraph, direction);
		}

		_editor.Focus();
	}

	public void NormalizeDocument(FlowDocument document)
	{
		foreach (Paragraph paragraph in EnumerateParagraphs(document.Blocks))
			ApplyDirectionToParagraph(paragraph, DirectionOf(paragraph));
	}

	public void NormalizeParagraphAtCaret()
	{
		Paragraph? paragraph = _editor.CaretPosition.Paragraph;
		if (paragraph != null)
			ApplyDirectionToParagraph(paragraph, DirectionOf(paragraph));
	}

	public static void RefreshAutomaticTextColors(
		FlowDocument document,
		Brush themeTextBrush)
	{
		foreach (Paragraph paragraph in EnumerateParagraphs(document.Blocks))
		{
			foreach (TextElement element in EnumerateInlineElements(paragraph.Inlines))
			{
				object local = element.ReadLocalValue(TextElement.ForegroundProperty);
				if (local is SolidColorBrush brush && IsAutomaticThemeColor(brush.Color))
					element.SetValue(TextElement.ForegroundProperty, themeTextBrush);
			}
		}
	}

	internal ParagraphDirection DirectionAtCaret()
	{
		Paragraph? paragraph = _editor.CaretPosition.Paragraph;
		return paragraph == null ? ParagraphDirection.LeftToRight : DirectionOf(paragraph);
	}

	internal static ParagraphDirection DirectionOf(Paragraph paragraph) =>
		paragraph.FlowDirection == FlowDirection.RightToLeft
			? ParagraphDirection.RightToLeft
			: ParagraphDirection.LeftToRight;

	internal static void ApplyDirectionToParagraph(
		Paragraph paragraph,
		ParagraphDirection direction)
	{
		FlowDirection flowDirection = FlowFor(direction);
		TextAlignment textAlignment = AlignmentFor(direction);

		paragraph.SetValue(FrameworkElement.FlowDirectionProperty, flowDirection);
		paragraph.SetValue(Block.TextAlignmentProperty, textAlignment);

		System.Windows.Documents.List? list = FindOwningList(paragraph);
		if (list == null) return;

		list.SetValue(FrameworkElement.FlowDirectionProperty, flowDirection);
		list.SetValue(Block.TextAlignmentProperty, textAlignment);
		NormalizeListGeometry(list);

		foreach (ListItem item in list.ListItems)
		{
			item.SetValue(FrameworkElement.FlowDirectionProperty, flowDirection);
			foreach (Paragraph child in EnumerateParagraphs(item.Blocks))
			{
				child.SetValue(FrameworkElement.FlowDirectionProperty, flowDirection);
				child.SetValue(Block.TextAlignmentProperty, textAlignment);
			}
		}
	}

	internal static IEnumerable<Paragraph> EnumerateParagraphs(BlockCollection blocks)
	{
		foreach (Block block in blocks)
		{
			if (block is Paragraph paragraph)
			{
				yield return paragraph;
			}
			else if (block is Section section)
			{
				foreach (Paragraph child in EnumerateParagraphs(section.Blocks))
					yield return child;
			}
			else if (block is System.Windows.Documents.List list)
			{
				foreach (ListItem item in list.ListItems)
				{
					foreach (Paragraph child in EnumerateParagraphs(item.Blocks))
						yield return child;
				}
			}
		}
	}

	private void ApplyNormalTypingProperties()
	{
		_editor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, _appFont);
		_editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 14.0);
		_editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
		_editor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
	}

	private IEnumerable<Paragraph> SelectedParagraphs()
	{
		TextPointer start = _editor.Selection.Start;
		TextPointer end = _editor.Selection.End;

		foreach (Paragraph paragraph in EnumerateParagraphs(_editor.Document.Blocks))
		{
			if (paragraph.ContentEnd.CompareTo(start) >= 0 &&
				paragraph.ContentStart.CompareTo(end) <= 0)
			{
				yield return paragraph;
			}
		}
	}

	private static System.Windows.Documents.List? FindOwningList(Paragraph paragraph)
	{
		DependencyObject? current = paragraph.Parent;
		while (current != null)
		{
			if (current is System.Windows.Documents.List list) return list;
			current = current is FrameworkContentElement element ? element.Parent : null;
		}
		return null;
	}

	private static IEnumerable<TextElement> EnumerateInlineElements(
		InlineCollection inlines)
	{
		foreach (Inline inline in inlines)
		{
			yield return inline;
			if (inline is Span span)
			{
				foreach (TextElement child in EnumerateInlineElements(span.Inlines))
					yield return child;
			}
		}
	}

	private static FlowDirection FlowFor(ParagraphDirection direction) =>
		direction == ParagraphDirection.RightToLeft
			? FlowDirection.RightToLeft
			: FlowDirection.LeftToRight;

	private static TextAlignment AlignmentFor(ParagraphDirection direction) =>
		direction == ParagraphDirection.RightToLeft
			// WPF mirrors Left/Right alignment inside an RTL flow. "Left" is
			// therefore the logical value that places content on the visual
			// right edge; the inverse is true for a visual left alignment.
			? TextAlignment.Left
			: TextAlignment.Left;

	private static TextAlignment LogicalAlignmentForVisual(
		FlowDirection flowDirection,
		TextAlignment visualAlignment)
	{
		if (flowDirection != FlowDirection.RightToLeft)
			return visualAlignment;

		return visualAlignment switch
		{
			TextAlignment.Left => TextAlignment.Right,
			TextAlignment.Right => TextAlignment.Left,
			_ => visualAlignment
		};
	}

	private static void NormalizeListGeometry(System.Windows.Documents.List list)
	{
		const double gutter = 30.0;
		list.Margin = list.FlowDirection == FlowDirection.RightToLeft
			? new Thickness(0.0, 0.0, gutter, 0.0)
			: new Thickness(gutter, 0.0, 0.0, 0.0);
		list.Padding = new Thickness(0.0);
		list.MarkerOffset = 18.0;
	}

	private static bool IsAutomaticThemeColor(System.Windows.Media.Color color) =>
		color == ((SolidColorBrush)ThemePalette.Dark.Text).Color ||
		color == ((SolidColorBrush)ThemePalette.Light.Text).Color;
}
