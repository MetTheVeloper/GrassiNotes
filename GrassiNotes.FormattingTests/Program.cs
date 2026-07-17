using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
			DirectionShortcutWaitsForCleanKeyUp(editor, formatting);
			AlignmentCommandsPreserveFlow(editor, formatting, host);
			TypingAndEnterKeepVisualAlignment(editor, formatting, host);
			HeadingEnterCreatesNormalParagraph(editor, formatting);
			ListItemsStayOnTheIntendedSide(editor, formatting, host);
			BulletsAndNumbersUseTheSameTextIndent(editor, formatting, host);
			ListMarkersRenderInsideTheViewport(editor, formatting);
			AutomaticTextColorTracksTheme(editor, formatting);
			ZoomChangesRenderedMetricsWithoutChangingFontSizes(editor, host);
			NativeDocumentZoomRoundTrips();
			AssertEqual("2.2.2", AppVersion.Current, "displayed application version");
			AssertEqual(
				"Untitled 1 \u2014 GrassiNotes v. 2.2.2",
				AppVersion.WindowTitle("Untitled 1"),
				"versioned document window title");
			HeaderLogoUsesThemeSvgVariants();

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

		CompleteDirectionGesture(
			formatting,
			Key.RightShift,
			ParagraphDirection.RightToLeft);
		DrainDispatcher();
		AssertFormatting(
			paragraph,
			FlowDirection.RightToLeft,
			TextAlignment.Left,
			"Ctrl+Right Shift logical properties");
		AssertAnchoredRight(paragraph, editor, host, "Ctrl+Right Shift visual position");

		CompleteDirectionGesture(
			formatting,
			Key.LeftShift,
			ParagraphDirection.LeftToRight);
		DrainDispatcher();
		AssertFormatting(
			paragraph,
			FlowDirection.LeftToRight,
			TextAlignment.Left,
			"Ctrl+Left Shift logical properties");
		AssertAnchoredLeft(paragraph, editor, host, "Ctrl+Left Shift visual position");
	}

	private static void DirectionShortcutWaitsForCleanKeyUp(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		Paragraph paragraph = Reset(editor, "direction");
		DirectionShortcutGesture gesture = new DirectionShortcutGesture();

		AssertTrue(
			gesture.OnKeyDown(
				Key.RightShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"direction chord key-down is suppressed");
		AssertEqual(
			FlowDirection.LeftToRight,
			paragraph.FlowDirection,
			"direction does not change on key-down");

		DirectionShortcutKeyUpResult completed =
			gesture.OnKeyUp(Key.RightShift, ModifierKeys.Control);
		AssertEqual<ParagraphDirection?>(
			ParagraphDirection.RightToLeft,
			completed.Direction,
			"clean key-up resolves RTL");
		formatting.ApplyDirection(completed.Direction!.Value);
		AssertEqual(
			FlowDirection.RightToLeft,
			paragraph.FlowDirection,
			"clean key-up applies RTL");

		formatting.ApplyDirection(ParagraphDirection.LeftToRight);
		AssertTrue(
			gesture.OnKeyDown(
				Key.RightShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"selection chord starts armed");
		AssertTrue(
			!gesture.OnKeyDown(
				Key.Right,
				ModifierKeys.Control | ModifierKeys.Shift),
			"arrow remains available to native selection");
		DirectionShortcutKeyUpResult cancelled =
			gesture.OnKeyUp(Key.RightShift, ModifierKeys.Control);
		AssertTrue(cancelled.Handled, "cancelled shift key-up remains suppressed");
		AssertEqual<ParagraphDirection?>(
			null,
			cancelled.Direction,
			"Ctrl+Shift+Arrow cancels direction change");
		AssertEqual(
			FlowDirection.LeftToRight,
			paragraph.FlowDirection,
			"word selection preserves direction");

		AssertTrue(
			gesture.OnKeyDown(
				Key.LeftShift,
				ModifierKeys.Control | ModifierKeys.Shift),
			"LTR chord starts armed");
		gesture.OnKeyUp(Key.LeftCtrl, ModifierKeys.Shift);
		DirectionShortcutKeyUpResult wrongReleaseOrder =
			gesture.OnKeyUp(Key.LeftShift, ModifierKeys.None);
		AssertEqual<ParagraphDirection?>(
			null,
			wrongReleaseOrder.Direction,
			"releasing Control first cancels the chord");
	}

	private static void AlignmentCommandsPreserveFlow(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host)
	{
		Paragraph paragraph = Reset(editor, "سلام دنیا");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);

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

		double bulletRight = ListTextRightDistance(
			editor,
			formatting,
			host,
			TextMarkerStyle.Disc);
		double numberRight = ListTextRightDistance(
			editor,
			formatting,
			host,
			TextMarkerStyle.Decimal);
		AssertNear(
			bulletRight,
			numberRight,
			1.0,
			"RTL bullet and numbering text indent");
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

	private static double ListTextRightDistance(
		RichTextBox editor,
		EditorFormattingController formatting,
		Window host,
		TextMarkerStyle markerStyle)
	{
		Paragraph paragraph = ResetAsList(editor, markerStyle, "مورد");
		formatting.ApplyDirection(ParagraphDirection.RightToLeft);
		DrainDispatcher();
		Rect bounds = PhysicalBounds(paragraph, editor, host);
		double editorRight = editor.TransformToAncestor(host)
			.Transform(new Point(editor.ActualWidth, 0.0)).X;
		return editorRight - bounds.Right;
	}

	private static void ListMarkersRenderInsideTheViewport(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		foreach (ParagraphDirection direction in new[]
			{
				ParagraphDirection.LeftToRight,
				ParagraphDirection.RightToLeft
			})
		{
			foreach (TextMarkerStyle markerStyle in new[]
				{
					TextMarkerStyle.Disc,
					TextMarkerStyle.Decimal
				})
			{
				Paragraph paragraph = ResetAsList(
					editor,
					markerStyle,
					direction == ParagraphDirection.RightToLeft
						? "مورد"
						: "Item");
				editor.Background = Brushes.White;
				editor.Foreground = Brushes.Black;
				editor.Document.Foreground = Brushes.Black;
				formatting.ApplyDirection(direction);
				DrainDispatcher();

				AssertMarkerPixels(
					editor,
					paragraph,
					direction,
					$"{direction} {markerStyle} marker");
			}
		}
	}

	private static void AutomaticTextColorTracksTheme(
		RichTextBox editor,
		EditorFormattingController formatting)
	{
		Paragraph paragraph = Reset(editor, "automatic");
		editor.Selection.Select(paragraph.ContentStart, paragraph.ContentEnd);
		formatting.ApplyAutomaticTextColor(ThemePalette.Dark.Text);
		AssertBrushColor(
			ThemePalette.Dark.Text,
			editor.Selection.GetPropertyValue(TextElement.ForegroundProperty),
			"automatic dark color");

		EditorFormattingController.RefreshAutomaticTextColors(
			editor.Document,
			ThemePalette.Light.Text);
		AssertBrushColor(
			ThemePalette.Light.Text,
			editor.Selection.GetPropertyValue(TextElement.ForegroundProperty),
			"automatic color follows light theme");

		string xaml = DocumentSerializer.ToXaml(editor.Document);
		FlowDocument restored = DocumentSerializer.FromXaml(xaml);
		EditorFormattingController.RefreshAutomaticTextColors(
			restored,
			ThemePalette.Dark.Text);
		TextRange restoredText = new TextRange(
			restored.ContentStart,
			restored.ContentEnd);
		AssertBrushColor(
			ThemePalette.Dark.Text,
			restoredText.GetPropertyValue(TextElement.ForegroundProperty),
			"automatic color survives save and reload");

		paragraph = Reset(editor, "manual");
		editor.Selection.Select(paragraph.ContentStart, paragraph.ContentEnd);
		SolidColorBrush manual = new SolidColorBrush(Colors.Red);
		formatting.ApplyTextColor(manual);
		EditorFormattingController.RefreshAutomaticTextColors(
			editor.Document,
			ThemePalette.Light.Text);
		AssertBrushColor(
			manual,
			editor.Selection.GetPropertyValue(TextElement.ForegroundProperty),
			"manual color remains fixed");
	}

	private static void HeaderLogoUsesThemeSvgVariants()
	{
		System.Windows.Shapes.Path logo = HeaderLogo.Create();
		AssertTrue(!logo.Data.Bounds.IsEmpty, "header SVG geometry is renderable");
		AssertBrushColor(
			new SolidColorBrush(
				System.Windows.Media.Color.FromRgb(250, 250, 250)),
			HeaderLogo.BrushFor("Dark"),
			"dark SVG fill");
		AssertBrushColor(
			new SolidColorBrush(
				System.Windows.Media.Color.FromRgb(255, 0, 0)),
			HeaderLogo.BrushFor("Light"),
			"light SVG fill");
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

	private static void CompleteDirectionGesture(
		EditorFormattingController formatting,
		Key shiftKey,
		ParagraphDirection expectedDirection)
	{
		DirectionShortcutGesture gesture = new DirectionShortcutGesture();
		AssertTrue(
			gesture.OnKeyDown(
				shiftKey,
				ModifierKeys.Control | ModifierKeys.Shift),
			$"Ctrl+{shiftKey} key-down handled");
		DirectionShortcutKeyUpResult result =
			gesture.OnKeyUp(shiftKey, ModifierKeys.Control);
		AssertTrue(result.Handled, $"Ctrl+{shiftKey} key-up handled");
		AssertEqual<ParagraphDirection?>(
			expectedDirection,
			result.Direction,
			$"Ctrl+{shiftKey} direction");
		formatting.ApplyDirection(result.Direction!.Value);
	}

	private static void AssertMarkerPixels(
		RichTextBox editor,
		Paragraph paragraph,
		ParagraphDirection direction,
		string label)
	{
		editor.IsReadOnly = true;
		DrainDispatcher();

		int width = Math.Max(1, (int)Math.Ceiling(editor.ActualWidth));
		int height = Math.Max(1, (int)Math.Ceiling(editor.ActualHeight));
		RenderTargetBitmap bitmap = new RenderTargetBitmap(
			width,
			height,
			96.0,
			96.0,
			PixelFormats.Pbgra32);
		bitmap.Render(editor);

		byte[] pixels = new byte[width * height * 4];
		bitmap.CopyPixels(pixels, width * 4, 0);

		Rect textBounds = LogicalBounds(paragraph);
		int top = Math.Clamp((int)Math.Floor(textBounds.Top) - 5, 0, height - 1);
		int bottom = Math.Clamp((int)Math.Ceiling(textBounds.Bottom) + 5, 0, height - 1);
		int left;
		int right;
		if (direction == ParagraphDirection.RightToLeft)
		{
			left = Math.Clamp((int)Math.Ceiling(textBounds.Right) + 1, 0, width - 1);
			right = width - 1;
		}
		else
		{
			left = 0;
			right = Math.Clamp((int)Math.Floor(textBounds.Left) - 1, 0, width - 1);
		}

		int darkPixels = 0;
		for (int y = top; y <= bottom; y++)
		{
			for (int x = left; x <= right; x++)
			{
				int offset = (y * width + x) * 4;
				byte blue = pixels[offset];
				byte green = pixels[offset + 1];
				byte red = pixels[offset + 2];
				byte alpha = pixels[offset + 3];
				if (alpha > 128 && red < 100 && green < 100 && blue < 100)
					darkPixels++;
			}
		}

		editor.IsReadOnly = false;
		AssertTrue(
			darkPixels >= 2,
			$"{label}: expected visible marker pixels inside editor, found {darkPixels}.");
	}

	private static Rect LogicalBounds(Paragraph paragraph)
	{
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

		return rectangles.Aggregate(Rect.Union);
	}

	private static void AssertBrushColor(
		Brush expected,
		object actual,
		string label)
	{
		if (expected is not SolidColorBrush expectedSolid ||
			actual is not SolidColorBrush actualSolid)
		{
			throw new InvalidOperationException(
				$"{label}: expected and actual values must be solid brushes.");
		}

		AssertEqual(expectedSolid.Color, actualSolid.Color, label);
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
		Rect logical = LogicalBounds(paragraph);
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
