using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GrassiNotes.FormattingTests;

internal static class Program
{
	private static readonly FontFamily AppFont = new FontFamily("Segoe UI");

	[STAThread]
	private static int Main()
	{
		Application application = new Application
		{
			ShutdownMode = ShutdownMode.OnExplicitShutdown
		};
		RichTextBox editor = new RichTextBox();
		Window host = new Window
		{
			Width = 640,
			Height = 320,
			ShowInTaskbar = false,
			Content = editor
		};

		try
		{
			host.Show();
			editor.Focus();
			DrainDispatcher();

			EditorFormattingController formatting =
				new EditorFormattingController(editor, AppFont);

			DirectionShortcutsSetBothProperties(editor, formatting);
			TypingDoesNotChangeDirection(editor, formatting);
			EnterInheritsDirectionAndAlignment(editor, formatting);
			HeadingEnterCreatesNormalParagraph(editor, formatting);
			ListItemsKeepDirectionWhileTyping(editor, formatting);
			DocumentNormalizationRepairsLegacyAlignment(editor, formatting);

			Console.WriteLine("Formatting behavior tests passed.");
			return 0;
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine(exception);
			return 1;
		}
		finally
		{
			host.Close();
			application.Shutdown();
		}
	}

	private static void DirectionShortcutsSetBothProperties(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		Paragraph paragraph = Reset(editor, "متن");

		AssertTrue(
			formatting.TryApplyDirectionShortcut(
				Key.RightShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"Ctrl+Right Shift handled");
		AssertFormatting(
			paragraph,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"Ctrl+Right Shift");

		AssertTrue(
			formatting.TryApplyDirectionShortcut(
				Key.LeftShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"Ctrl+Left Shift handled");
		AssertFormatting(
			paragraph,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"Ctrl+Left Shift");
	}

	private static void TypingDoesNotChangeDirection(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		Paragraph paragraph = Reset(editor, "شروع");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);

		editor.Selection.Text = " نویسه";
		DrainDispatcher();

		AssertFormatting(
			editor.CaretPosition.Paragraph ?? paragraph,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"RTL after typing");

		formatting.ApplyDirection(ParagraphDirection.LeftToRight);
		editor.Selection.Text = " character";
		DrainDispatcher();

		AssertFormatting(
			editor.CaretPosition.Paragraph ?? paragraph,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"LTR after typing");
	}

	private static void EnterInheritsDirectionAndAlignment(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		_ = Reset(editor, "پاراگراف");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);
		formatting.InsertParagraphBreak();
		DrainDispatcher();

		Paragraph next = editor.CaretPosition.Paragraph ??
			throw new InvalidOperationException("Enter did not create a paragraph.");
		AssertFormatting(
			next,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"RTL Enter inheritance");

		formatting.ApplyDirection(ParagraphDirection.LeftToRight);
		formatting.InsertParagraphBreak();
		DrainDispatcher();

		Paragraph ltrNext = editor.CaretPosition.Paragraph ??
			throw new InvalidOperationException("LTR Enter did not create a paragraph.");
		AssertFormatting(
			ltrNext,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"LTR Enter inheritance");
	}

	private static void HeadingEnterCreatesNormalParagraph(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		Paragraph heading = Reset(editor, "عنوان");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);
		formatting.ApplyHeading(2);
		AssertTrue(ParagraphStyleFormatter.IsHeading(heading), "heading applied");

		editor.CaretPosition = heading.ContentEnd;
		editor.Selection.Select(editor.CaretPosition, editor.CaretPosition);
		formatting.InsertParagraphBreak();
		DrainDispatcher();

		Paragraph next = editor.CaretPosition.Paragraph ??
			throw new InvalidOperationException("Heading Enter did not create a paragraph.");
		AssertTrue(!ParagraphStyleFormatter.IsHeading(next), "new paragraph is Normal");
		AssertEqual(14.0, next.FontSize, "Normal font size");
		AssertEqual(FontWeights.Normal, next.FontWeight, "Normal font weight");
		AssertFormatting(
			next,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"heading Enter direction");
	}

	private static void ListItemsKeepDirectionWhileTyping(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		foreach (TextMarkerStyle markerStyle in new[]
			{
				TextMarkerStyle.Disc,
				TextMarkerStyle.Decimal
			})
		{
			Paragraph paragraph = new Paragraph(new Run("مورد"));
			System.Windows.Documents.List list = new System.Windows.Documents.List
			{
				MarkerStyle = markerStyle
			};
			list.ListItems.Add(new ListItem(paragraph));
			editor.Document = new FlowDocument(list);
			editor.CaretPosition = paragraph.ContentEnd;
			editor.Selection.Select(editor.CaretPosition, editor.CaretPosition);

			formatting.ApplyDirection(ParagraphDirection.RightToLeft);
			editor.Selection.Text = " جدید";
			DrainDispatcher();

			Paragraph result = editor.CaretPosition.Paragraph ?? paragraph;
			AssertFormatting(
				result,
				FlowDirection.RightToLeft,
				TextAlignment.Right,
				$"{markerStyle} RTL typing");
			AssertEqual(
				FlowDirection.RightToLeft,
				list.FlowDirection,
				$"{markerStyle} list flow");

			formatting.ApplyDirection(ParagraphDirection.LeftToRight);
			editor.Selection.Text = " item";
			DrainDispatcher();
			AssertFormatting(
				editor.CaretPosition.Paragraph ?? result,
				FlowDirection.LeftToRight,
				TextAlignment.Left,
				$"{markerStyle} LTR typing");
		}
	}

	private static void DocumentNormalizationRepairsLegacyAlignment(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		Paragraph rtl = new Paragraph(new Run("متن"))
		{
			FlowDirection = FlowDirection.RightToLeft,
			TextAlignment = TextAlignment.Left
		};
		Paragraph ltr = new Paragraph(new Run("text"))
		{
			FlowDirection = FlowDirection.LeftToRight,
			TextAlignment = TextAlignment.Right
		};
		editor.Document = new FlowDocument();
		editor.Document.Blocks.Add(rtl);
		editor.Document.Blocks.Add(ltr);

		formatting.NormalizeDocument(editor.Document);

		AssertFormatting(
			rtl,
			FlowDirection.RightToLeft,
			TextAlignment.Right,
			"legacy RTL repair");
		AssertFormatting(
			ltr,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"legacy LTR repair");
	}

	private static Paragraph Reset(RichTextBox editor, string text)
	{
		Paragraph paragraph = new Paragraph(new Run(text));
		editor.Document = new FlowDocument(paragraph);
		editor.CaretPosition = paragraph.ContentEnd;
		editor.Selection.Select(editor.CaretPosition, editor.CaretPosition);
		editor.Focus();
		DrainDispatcher();
		return paragraph;
	}

	private static void AssertFormatting(
		Paragraph paragraph,
		FlowDirection expectedFlow,
		TextAlignment expectedAlignment,
		string label)
	{
		AssertEqual(expectedFlow, paragraph.FlowDirection, label + " flow");
		AssertEqual(expectedAlignment, paragraph.TextAlignment, label + " alignment");
		AssertEqual(
			expectedAlignment,
			(TextAlignment)paragraph.ReadLocalValue(Block.TextAlignmentProperty),
			label + " local alignment");
	}

	private static void AssertTrue(bool actual, string label)
	{
		if (!actual)
			throw new InvalidOperationException(label + ": expected true.");
	}

	private static void AssertEqual<T>(T expected, T actual, string label)
	{
		if (!Equals(expected, actual))
			throw new InvalidOperationException(
				$"{label}: expected {expected}, actual {actual}.");
	}

	private static void DrainDispatcher()
	{
		DispatcherFrame frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke((Action)delegate
		{
			frame.Continue = false;
		}, DispatcherPriority.ApplicationIdle);
		Dispatcher.PushFrame(frame);
	}
}
