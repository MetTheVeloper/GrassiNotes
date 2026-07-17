using System;
using System.Collections.Generic;
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
		RichTextBox editor = new RichTextBox
		{
			Width = 640.0,
			Height = 320.0,
			Padding = new Thickness(0.0),
			BorderThickness = new Thickness(0.0),
			FontFamily = AppFont,
			FontSize = 14.0
		};
		Window host = new Window
		{
			Width = 660.0,
			Height = 360.0,
			ShowInTaskbar = false,
			WindowStyle = WindowStyle.None,
			Content = editor
		};

		try
		{
			host.Show();
			editor.Focus();
			DrainDispatcher();

			EditorFormattingController formatting =
				new EditorFormattingController(editor, AppFont);

			DirectionShortcutsAnchorTextVisually(editor, formatting, host);
			AlignmentCommandsPreserveFlow(editor, formatting, host);
			TypingAndEnterKeepVisualAlignment(editor, formatting, host);
			HeadingEnterCreatesNormalParagraph(editor, formatting);
			ListItemsStayOnTheIntendedSide(editor, formatting, host);
			BulletsAndNumbersUseTheSameTextIndent(editor, formatting, host);
			ZoomChangesRenderedMetricsWithoutChangingFontSizes(editor, host);
			NativeDocumentZoomRoundTrips();

			Console.WriteLine("Formatting and visual-layout behavior tests passed.");
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

	private static void DirectionShortcutsAnchorTextVisually(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host)
	{
		Paragraph paragraph = Reset(editor, "سلام دنیا");

		AssertTrue(
			formatting.TryApplyDirectionShortcut(
				Key.RightShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"Ctrl+Right Shift handled");
		DrainDispatcher();
		AssertFormatting(
			paragraph,
			FlowDirection.RightToLeft,
			TextAlignment.Left,
			"Ctrl+Right Shift logical properties");
		AssertAnchoredRight(paragraph, editor, host, "Ctrl+Right Shift visual position");

		AssertTrue(
			formatting.TryApplyDirectionShortcut(
				Key.LeftShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"Ctrl+Left Shift handled");
		DrainDispatcher();
		AssertFormatting(
			paragraph,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"Ctrl+Left Shift logical properties");
		AssertAnchoredLeft(paragraph, editor, host, "Ctrl+Left Shift visual position");
	}

	private static void AlignmentCommandsPreserveFlow(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host)
	{
		Paragraph paragraph = Reset(editor, "سلام دنیا");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);

		AssertTrue(
			!formatting.TryApplyDirectionShortcut(Key.R, ModifierKeys.Control),
			"Ctrl+R is not interpreted as a direction shortcut");
		formatting.ApplyVisualAlignment(TextAlignment.Right);
		DrainDispatcher();

		AssertEqual(
			FlowDirection.RightToLeft,
			paragraph.FlowDirection,
			"Ctrl+R preserves RTL flow");
		AssertAnchoredRight(paragraph, editor, host, "Ctrl+R visual position");

		formatting.ApplyVisualAlignment(TextAlignment.Left);
		DrainDispatcher();
		AssertEqual(
			FlowDirection.RightToLeft,
			paragraph.FlowDirection,
			"Ctrl+L preserves RTL flow");
		AssertAnchoredLeft(paragraph, editor, host, "Ctrl+L visual position");
	}

	private static void TypingAndEnterKeepVisualAlignment(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host)
	{
		Paragraph paragraph = Reset(editor, "شروع");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);
		editor.Selection.Text = " نویسه";
		DrainDispatcher();

		Paragraph typed = editor.CaretPosition.Paragraph ?? paragraph;
		AssertEqual(FlowDirection.RightToLeft, typed.FlowDirection, "RTL after typing flow");
		AssertAnchoredRight(typed, editor, host, "RTL after typing visual position");

		formatting.InsertParagraphBreak();
		editor.Selection.Text = "پاراگراف جدید";
		DrainDispatcher();

		Paragraph next = editor.CaretPosition.Paragraph ??
			throw new InvalidOperationException("Enter did not create a paragraph.");
		AssertEqual(FlowDirection.RightToLeft, next.FlowDirection, "RTL Enter flow");
		AssertAnchoredRight(next, editor, host, "RTL Enter visual position");
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
		AssertEqual(FlowDirection.RightToLeft, next.FlowDirection, "heading Enter flow");
	}

	private static void ListItemsStayOnTheIntendedSide(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host)
	{
		foreach (TextMarkerStyle markerStyle in new[]
			{
				TextMarkerStyle.Disc,
				TextMarkerStyle.Decimal
			})
		{
			Paragraph paragraph = ResetAsList(editor, markerStyle, "مورد اول");
			formatting.ApplyDirection(ParagraphDirection.RightToLeft);
			editor.Selection.Text = " جدید";
			DrainDispatcher();

			System.Windows.Documents.List list =
				(System.Windows.Documents.List)((ListItem)paragraph.Parent).Parent;
			AssertEqual(
				FlowDirection.RightToLeft,
				list.FlowDirection,
				$"{markerStyle} marker flow");
			AssertAnchoredRight(
				paragraph,
				editor,
				host,
				$"{markerStyle} RTL visual position");

			formatting.InsertParagraphBreak();
			editor.Selection.Text = "مورد دوم";
			DrainDispatcher();
			Paragraph next = editor.CaretPosition.Paragraph ??
				throw new InvalidOperationException($"{markerStyle} Enter failed.");
			AssertAnchoredRight(
				next,
				editor,
				host,
				$"{markerStyle} Enter visual position");
		}
	}

	private static void BulletsAndNumbersUseTheSameTextIndent(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host)
	{
		double bulletStart = ListTextStart(
			editor,
			formatting,
			host,
			TextMarkerStyle.Disc);
		double numberStart = ListTextStart(
			editor,
			formatting,
			host,
			TextMarkerStyle.Decimal);

		AssertNear(
			bulletStart,
			numberStart,
			1.0,
			"bullet and numbering text indent");
	}

	private static double ListTextStart(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host,
		TextMarkerStyle markerStyle)
	{
		Paragraph paragraph = ResetAsList(editor, markerStyle, "English item");
		formatting.ApplyDirection(ParagraphDirection.LeftToRight);
		DrainDispatcher();
		return PhysicalBounds(paragraph, editor, host).Left;
	}

	private static void ZoomChangesRenderedMetricsWithoutChangingFontSizes(
		RichTextBox editor,
		Window host)
	{
		Paragraph heading = Reset(editor, "Heading");
		heading.FontSize = 28.0;
		double storedFontSize = heading.FontSize;
		double heightAt100 = PhysicalBounds(heading, editor, host).Height;

		EditorZoom.Apply(editor, 2.0);
		DrainDispatcher();
		double heightAt200 = PhysicalBounds(heading, editor, host).Height;

		AssertEqual(storedFontSize, heading.FontSize, "zoom preserves document FontSize");
		AssertTrue(
			heightAt200 > heightAt100 * 1.7,
			$"200% zoom changes rendered height ({heightAt100:0.##} -> {heightAt200:0.##})");

		EditorZoom.Apply(editor, 1.0);
		DrainDispatcher();
		AssertTrue(
			ReferenceEquals(editor.LayoutTransform, Transform.Identity),
			"Ctrl+0 reset uses identity transform");
	}

	private static void NativeDocumentZoomRoundTrips()
	{
		FlowDocument document = DocumentSerializer.NewDocument("saved");
		DocumentMetadata.SetZoom(document, 2.0);

		string xaml = DocumentSerializer.ToXaml(document);
		FlowDocument restored = DocumentSerializer.FromXaml(xaml);

		AssertNear(2.0, DocumentMetadata.GetZoom(restored), 0.001, "native zoom persistence");
	}

	private static Paragraph Reset(RichTextBox editor, string text)
	{
		EditorZoom.Apply(editor, 1.0);
		Paragraph paragraph = new Paragraph(new Run(text))
		{
			Margin = new Thickness(0.0)
		};
		editor.Document = new FlowDocument(paragraph)
		{
			PagePadding = new Thickness(0.0),
			FontFamily = AppFont,
			FontSize = 14.0
		};
		editor.CaretPosition = paragraph.ContentEnd;
		editor.Selection.Select(editor.CaretPosition, editor.CaretPosition);
		editor.Focus();
		DrainDispatcher();
		return paragraph;
	}

	private static Paragraph ResetAsList(
		RichTextBox editor,
		TextMarkerStyle markerStyle,
		string text)
	{
		EditorZoom.Apply(editor, 1.0);
		Paragraph paragraph = new Paragraph(new Run(text))
		{
			Margin = new Thickness(0.0)
		};
		System.Windows.Documents.List list = new System.Windows.Documents.List
		{
			MarkerStyle = markerStyle
		};
		list.ListItems.Add(new ListItem(paragraph));
		editor.Document = new FlowDocument(list)
		{
			PagePadding = new Thickness(0.0),
			FontFamily = AppFont,
			FontSize = 14.0
		};
		editor.CaretPosition = paragraph.ContentEnd;
		editor.Selection.Select(editor.CaretPosition, editor.CaretPosition);
		editor.Focus();
		DrainDispatcher();
		return paragraph;
	}

	private static Rect PhysicalBounds(
		Paragraph paragraph,
		RichTextBox editor,
		Window host)
	{
		DrainDispatcher();
		List<Rect> rectangles = new List<Rect>();
		TextPointer? pointer = paragraph.ContentStart;
		while (pointer != null && pointer.CompareTo(paragraph.ContentEnd) <= 0)
		{
			Rect rectangle = pointer.GetCharacterRect(LogicalDirection.Forward);
			if (!rectangle.IsEmpty)
				rectangles.Add(rectangle);

			if (pointer.CompareTo(paragraph.ContentEnd) == 0)
				break;
			pointer = pointer.GetNextInsertionPosition(LogicalDirection.Forward) ??
				paragraph.ContentEnd;
		}

		if (rectangles.Count == 0)
			throw new InvalidOperationException("No rendered character rectangles were found.");

		Rect logical = rectangles.Aggregate(Rect.Union);
		GeneralTransform transform = editor.TransformToAncestor(host);
		return transform.TransformBounds(logical);
	}

	private static void AssertAnchoredRight(
		Paragraph paragraph,
		RichTextBox editor,
		Window host,
		string label)
	{
		Rect bounds = PhysicalBounds(paragraph, editor, host);
		double editorRight = editor.TransformToAncestor(host)
			.Transform(new Point(editor.ActualWidth, 0.0)).X;
		AssertTrue(
			bounds.Left > editorRight / 2.0,
			$"{label}: expected right half, bounds={bounds}, editorRight={editorRight:0.##}");
	}

	private static void AssertAnchoredLeft(
		Paragraph paragraph,
		RichTextBox editor,
		Window host,
		string label)
	{
		Rect bounds = PhysicalBounds(paragraph, editor, host);
		double editorRight = editor.TransformToAncestor(host)
			.Transform(new Point(editor.ActualWidth, 0.0)).X;
		AssertTrue(
			bounds.Right < editorRight / 2.0,
			$"{label}: expected left half, bounds={bounds}, editorRight={editorRight:0.##}");
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

	private static void AssertNear(
		double expected,
		double actual,
		double tolerance,
		string label)
	{
		if (Math.Abs(expected - actual) > tolerance)
		{
			throw new InvalidOperationException(
				$"{label}: expected {expected:0.###} ± {tolerance:0.###}, actual {actual:0.###}.");
		}
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
