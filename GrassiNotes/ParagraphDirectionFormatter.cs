using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GrassiNotes;

internal static class ParagraphDirectionFormatter
{
	public static void Apply(
		Paragraph paragraph,
		FlowDirection flowDirection,
		TextAlignment textAlignment,
		string? explicitDirection)
	{
		ApplyToParagraph(paragraph, flowDirection, textAlignment, explicitDirection);

		List? list = FindOwningList(paragraph);
		if (list == null) return;

		list.SetCurrentValue(FrameworkElement.FlowDirectionProperty, flowDirection);
		list.SetCurrentValue(Block.TextAlignmentProperty, textAlignment);
		list.MarkerOffset = 18.0;
		if (explicitDirection != null)
			EditorMetadata.SetExplicitDirection(list, explicitDirection);

		foreach (ListItem item in list.ListItems)
		{
			item.SetCurrentValue(FrameworkElement.FlowDirectionProperty, flowDirection);
			if (explicitDirection != null)
				EditorMetadata.SetExplicitDirection(item, explicitDirection);

			foreach (Paragraph child in EnumerateParagraphs(item.Blocks))
				ApplyToParagraph(child, flowDirection, textAlignment, explicitDirection);
		}
	}

	private static void ApplyToParagraph(
		Paragraph paragraph,
		FlowDirection flowDirection,
		TextAlignment textAlignment,
		string? explicitDirection)
	{
		paragraph.SetCurrentValue(FrameworkElement.FlowDirectionProperty, flowDirection);
		paragraph.SetCurrentValue(Block.TextAlignmentProperty, textAlignment);
		if (explicitDirection != null)
			EditorMetadata.SetExplicitDirection(paragraph, explicitDirection);
	}

	private static List? FindOwningList(Paragraph paragraph)
	{
		DependencyObject? current = paragraph.Parent;
		while (current != null)
		{
			if (current is List list) return list;
			current = current is FrameworkContentElement element ? element.Parent : null;
		}
		return null;
	}

	private static IEnumerable<Paragraph> EnumerateParagraphs(BlockCollection blocks)
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
			else if (block is List list)
			{
				foreach (ListItem item in list.ListItems)
				{
					foreach (Paragraph child in EnumerateParagraphs(item.Blocks))
						yield return child;
				}
			}
		}
	}
}
