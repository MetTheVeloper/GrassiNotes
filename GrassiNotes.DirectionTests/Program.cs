using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GrassiNotes.DirectionTests;

internal static class Program
{
	[STAThread]
	private static int Main()
	{
		try
		{
			ShortcutFormattingSetsBothProperties();
			EnterFormattingInheritsBothProperties();
			ListFormattingStaysAligned();
			Console.WriteLine("RTL/LTR direction regression tests passed.");
			return 0;
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine(exception);
			return 1;
		}
	}

	private static void ShortcutFormattingSetsBothProperties()
	{
		Paragraph paragraph = new Paragraph(new Run("متن"));

		ParagraphDirectionFormatter.Apply(
			paragraph,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"RTL");
		AssertFormatting(paragraph, FlowDirection.RightToLeft, TextAlignment.Right, "RTL");

		ParagraphDirectionFormatter.Apply(
			paragraph,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"LTR");
		AssertFormatting(paragraph, FlowDirection.LeftToRight, TextAlignment.Left, "LTR");
	}

	private static void EnterFormattingInheritsBothProperties()
	{
		Paragraph source = new Paragraph(new Run("متن"));
		ParagraphDirectionFormatter.Apply(
			source,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"RTL");

		Paragraph newParagraph = new Paragraph();
		ParagraphDirectionFormatter.Apply(
			newParagraph,
			source.FlowDirection,
			source.TextAlignment,
			EditorMetadata.GetExplicitDirection(source));

		AssertFormatting(newParagraph, FlowDirection.RightToLeft, TextAlignment.Right, "RTL");
	}

	private static void ListFormattingStaysAligned()
	{
		Paragraph first = new Paragraph(new Run("یک"));
		Paragraph second = new Paragraph(new Run("دو"));
		List list = new List();
		list.ListItems.Add(new ListItem(first));
		list.ListItems.Add(new ListItem(second));
		FlowDocument document = new FlowDocument(list);

		ParagraphDirectionFormatter.Apply(
			first,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"RTL");
		AssertListFormatting(list, new[] { first, second }, FlowDirection.RightToLeft, TextAlignment.Right, "RTL");

		first.Inlines.Add(new Run(" سه"));
		ParagraphDirectionFormatter.Apply(
			first,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"RTL");
		AssertListFormatting(list, new[] { first, second }, FlowDirection.RightToLeft, TextAlignment.Right, "RTL");

		ParagraphDirectionFormatter.Apply(
			second,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"LTR");
		AssertListFormatting(list, new[] { first, second }, FlowDirection.LeftToRight, TextAlignment.Left, "LTR");

		GC.KeepAlive(document);
	}

	private static void AssertListFormatting(
		List list,
		IEnumerable<Paragraph> paragraphs,
		FlowDirection expectedFlowDirection,
		TextAlignment expectedTextAlignment,
		string expectedExplicitDirection)
	{
		AssertEqual(expectedFlowDirection, list.FlowDirection, "list FlowDirection");
		AssertEqual(expectedTextAlignment, list.TextAlignment, "list TextAlignment");
		foreach (Paragraph paragraph in paragraphs)
			AssertFormatting(paragraph, expectedFlowDirection, expectedTextAlignment, expectedExplicitDirection);
	}

	private static void AssertFormatting(
		Paragraph paragraph,
		FlowDirection expectedFlowDirection,
		TextAlignment expectedTextAlignment,
		string expectedExplicitDirection)
	{
		AssertEqual(expectedFlowDirection, paragraph.FlowDirection, "paragraph FlowDirection");
		AssertEqual(expectedTextAlignment, paragraph.TextAlignment, "paragraph TextAlignment");
		AssertEqual(expectedExplicitDirection, EditorMetadata.GetExplicitDirection(paragraph), "explicit direction");
	}

	private static void AssertEqual<T>(T expected, T actual, string name)
		where T : notnull
	{
		if (!EqualityComparer<T>.Default.Equals(expected, actual))
			throw new InvalidOperationException($"{name}: expected {expected}, got {actual}.");
	}
}
