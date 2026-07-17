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
		ForceTextAlignment(list, textAlignment);
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

	public static void EnforceAlignmentFromFlowDirection(Paragraph paragraph)
	{
		ForceTextAlignment(paragraph, AlignmentFor(paragraph.FlowDirection));

		List? list = FindOwningList(paragraph);
		if (list == null) return;

		ForceTextAlignment(list, AlignmentFor(list.FlowDirection));
		foreach (ListItem item in list.ListItems)
		{
			foreach (Paragraph child in EnumerateParagraphs(item.Blocks))
				ForceTextAlignment(child, AlignmentFor(child.FlowDirection));
		}
	}

	public static void SynchronizeWithTypingFlow(
		Paragraph paragraph,
		object typingFlowDirection,
		string? forcedDirection = null)
	{
		string explicitDirection = forcedDirection ?? EditorMetadata.GetExplicitDirection(paragraph);
		FlowDirection flowDirection;

		if (explicitDirection == "RTL")
			flowDirection = FlowDirection.RightToLeft;
		else if (explicitDirection == "LTR")
			flowDirection = FlowDirection.LeftToRight;
		else if (typingFlowDirection is FlowDirection currentTypingFlow)
			flowDirection = currentTypingFlow;
		else
			flowDirection = paragraph.FlowDirection;

		Apply(
			paragraph,
			flowDirection,
			AlignmentFor(flowDirection),
			explicitDirection == "RTL" || explicitDirection == "LTR" ? explicitDirection : null);
	}

	private static void ApplyToParagraph(
		Paragraph paragraph,
		FlowDirection flowDirection,
		TextAlignment textAlignment,
		string? explicitDirection)
	{
		paragraph.SetCurrentValue(FrameworkElement.FlowDirectionProperty, flowDirection);
		ForceTextAlignment(paragraph, textAlignment);
		if (explicitDirection != null)
			EditorMetadata.SetExplicitDirection(paragraph, explicitDirection);
	}

	private static TextAlignment AlignmentFor(FlowDirection flowDirection) =>
		flowDirection == FlowDirection.RightToLeft ? TextAlignment.Right : TextAlignment.Left;

	private static void ForceTextAlignment(Block block, TextAlignment textAlignment)
	{
		object localValue = block.ReadLocalValue(Block.TextAlignmentProperty);
		if (!Equals(localValue, textAlignment))
			block.SetValue(Block.TextAlignmentProperty, textAlignment);
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
