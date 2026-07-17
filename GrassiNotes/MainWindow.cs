using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;

namespace GrassiNotes;

public sealed class MainWindow : Window
{
	private readonly bool _startHidden;

	private readonly System.Windows.Media.FontFamily _appFont;

	private readonly List<DocumentTab> _documents = new List<DocumentTab>();

	private DocumentTab? _active;

	private int _untitled = 1;

	private bool _suppress;

	private readonly HashSet<Paragraph> _pendingAlignmentParagraphs = new HashSet<Paragraph>();

	private DispatcherOperation? _alignmentEnforcementOperation;

	private string? _pendingTypingDirection;

	private bool _exitRequested;

	private string _themeName = "Dark";

	private ThemePalette _theme = ThemePalette.Dark;

	private readonly DispatcherTimer _autosave = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(2.0)
	};

	private NotifyIcon? _tray;

	private TrayMenuWindow? _trayMenu;

	private HwndSource? _source;

	private bool _trayHintShown;

	private const int HotkeyShowId = 18254;

	private const int HotkeyTopmostId = 18255;

	private const int HotkeyExitId = 18256;

	private const int WmHotkey = 786;

	private const uint ModAlt = 1u;

	private const uint ModControl = 2u;

	private const uint ModShift = 4u;

	private const uint ModNoRepeat = 16384u;

	private Grid _root;

	private Border _outer;

	private Border _title;

	private Border _toolbar;

	private Border _format;

	private Border _tabs;

	private Border _status;

	private StackPanel _tabsPanel;

	private System.Windows.Controls.RichTextBox _editor;

	private TextBlock _titleText;

	private TextBlock _saveStatus;

	private TextBlock _positionStatus;

	private TextBlock _wordsStatus;

	private TextBlock _formatStatus;

	private TextBlock _zoomStatus;

	private System.Windows.Controls.Button _themeButton;

	private System.Windows.Controls.Button _boldButton;

	private System.Windows.Controls.Button _italicButton;

	private System.Windows.Controls.Button _underlineButton;

	private System.Windows.Controls.Button _bulletButton;

	private System.Windows.Controls.Button _numberButton;

	private System.Windows.Controls.Button _headingButton;

	private System.Windows.Controls.Button _colorButton;

	private System.Windows.Controls.Button _maximizeButton;

	private Popup _contextPopup;

	private Popup _headingPopup;

	private Popup _colorPopup;

	private Popup _findPopup;

	private StackPanel _contextPanel;

	private System.Windows.Controls.TextBox _findBox;

	private TextBlock _findCount;

	private int _findIndex = -1;

	private List<TextRange> _findMatches = new List<TextRange>();

	public MainWindow(bool startHidden, System.Windows.Media.FontFamily appFont)
	{
		_startHidden = startHidden;
		_appFont = appFont;
		base.Title = "GrassiNotes";
		base.Width = 980.0;
		base.Height = 680.0;
		base.MinWidth = 620.0;
		base.MinHeight = 400.0;
		base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		base.WindowStyle = WindowStyle.None;
		base.ResizeMode = ResizeMode.CanResize;
		base.Background = System.Windows.Media.Brushes.Transparent;
		base.UseLayoutRounding = true;
		base.SnapsToDevicePixels = true;
		try
		{
			using MemoryStream bitmapStream = new MemoryStream(EmbeddedAssets.AppIco);
			base.Icon = BitmapFrame.Create(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
		}
		catch
		{
		}
		base.AllowDrop = true;
		base.FontFamily = appFont;
		WindowChrome.SetWindowChrome(this, new WindowChrome
		{
			CaptionHeight = 0.0,
			CornerRadius = new CornerRadius(8.0),
			GlassFrameThickness = new Thickness(0.0),
			ResizeBorderThickness = new Thickness(6.0),
			UseAeroCaptionButtons = false
		});
		BuildUi();
		base.AddHandler(Keyboard.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(OnPreviewKeyDown), true);
		base.AddHandler(Keyboard.PreviewKeyUpEvent, new System.Windows.Input.KeyEventHandler(OnPreviewKeyUp), true);
		base.Closing += OnClosing;
		base.SourceInitialized += OnSourceInitialized;
		base.Drop += OnDrop;
		base.StateChanged += delegate
		{
			UpdateMaximizeGlyph();
		};
		_autosave.Tick += delegate
		{
			_autosave.Stop();
			SaveSession();
			TextBlock saveStatus = _saveStatus;
			DocumentTab? active = _active;
			saveStatus.Text = ((active != null && active.IsDirty) ? "Draft saved" : "Saved");
		};
	}

	public void Initialize()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		StartupService.EnsureEnabled();
		EnsureTrayVisible();
		SessionState sessionState = SessionService.Load();
		if (sessionState != null)
		{
			_themeName = ((sessionState.Theme == "Light") ? "Light" : "Dark");
			base.Topmost = sessionState.AlwaysOnTop;
			var enumerator = sessionState.Documents.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					SessionDocument current = enumerator.Current;
					DocumentTab documentTab = new DocumentTab
					{
						Id = ((current.Id == Guid.Empty) ? Guid.NewGuid() : current.Id),
						Title = current.Title,
						FilePath = current.FilePath,
						IsDirty = current.IsDirty,
						Kind = current.Kind,
						Zoom = current.Zoom,
						Document = DocumentSerializer.FromXaml(current.Xaml)
					};
					ConfigureDocument(documentTab.Document);
					AddDocument(documentTab, select: false);
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			if (_documents.Count > 0)
			{
				SelectDocument(_documents[Math.Clamp(sessionState.ActiveIndex, 0, _documents.Count - 1)]);
			}
		}
		ApplyTheme(_themeName, save: false);
		if (_documents.Count == 0)
		{
			NewDocument();
		}
		if (_startHidden)
		{
			base.ShowInTaskbar = false;
			base.Opacity = 0.0;
		}
		else
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				_editor.Focus();
			}, DispatcherPriority.ContextIdle);
		}
	}

	private void BuildUi()
	{
		_root = new Grid();
		_root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36.0) });
		_root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40.0) });
		_root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38.0) });
		_root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(33.0) });
		_root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		_root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24.0) });

		_outer = new Border
		{
			Child = _root,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(9.0),
			SnapsToDevicePixels = true
		};
		base.Content = _outer;

		_title = MakePanel();
		Grid.SetRow(_title, 0);
		_root.Children.Add(_title);
		Grid titleGrid = new Grid { Margin = new Thickness(10.0, 0.0, 0.0, 0.0) };
		titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_title.Child = titleGrid;
		System.Windows.Controls.Image logo = LoadLogo();
		logo.Width = 20.0;
		logo.Height = 20.0;
		logo.Margin = new Thickness(0.0, 0.0, 9.0, 0.0);
		logo.VerticalAlignment = VerticalAlignment.Center;
		titleGrid.Children.Add(logo);
		_titleText = new TextBlock
		{
			Text = "GrassiNotes",
			FontSize = 13.5,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_titleText, 1);
		titleGrid.Children.Add(_titleText);
		StackPanel chrome = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
		Grid.SetColumn(chrome, 2);
		titleGrid.Children.Add(chrome);
		chrome.Children.Add(ChromeButton("\ue921", delegate { SystemCommands.MinimizeWindow(this); }, "Minimize"));
		_maximizeButton = ChromeButton("\ue922", delegate { ToggleMaximize(); }, "Maximize");
		chrome.Children.Add(_maximizeButton);
		chrome.Children.Add(ChromeButton("\ue8bb", delegate { HideToTray(); }, "Close", "CloseButton"));
		_title.MouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2) ToggleMaximize();
			else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
		};

		_toolbar = MakePanel();
		Grid.SetRow(_toolbar, 1);
		_root.Children.Add(_toolbar);
		Grid toolGrid = new Grid { Margin = new Thickness(8.0, 5.0, 8.0, 5.0) };
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_toolbar.Child = toolGrid;
		StackPanel tools = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
		toolGrid.Children.Add(tools);
		tools.Children.Add(ToolButton("\ue710", delegate { NewDocument(); }, "New  Ctrl+N"));
		tools.Children.Add(ToolButton("\ue8e5", async delegate { await OpenDocumentAsync(); }, "Open  Ctrl+O"));
		tools.Children.Add(ToolButton("\ue74e", async delegate { await SaveActiveAsync(false); }, "Save  Ctrl+S"));
		_themeButton = ToolButton("\ue706", delegate { ApplyTheme(_themeName == "Dark" ? "Light" : "Dark"); }, "Switch theme");
		_themeButton.Margin = new Thickness(0.0);
		Grid.SetColumn(_themeButton, 1);
		toolGrid.Children.Add(_themeButton);

		_format = MakePanel();
		Grid.SetRow(_format, 2);
		_root.Children.Add(_format);
		StackPanel formatTools = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			Margin = new Thickness(8.0, 4.0, 8.0, 4.0)
		};
		_format.Child = formatTools;
		_headingButton = TextToolButton("Normal", delegate { _headingPopup.IsOpen = !_headingPopup.IsOpen; }, "Paragraph style", 82.0);
		formatTools.Children.Add(_headingButton);
		formatTools.Children.Add(Separator());
		_boldButton = TextToolButton("B", delegate { Execute(EditingCommands.ToggleBold); }, "Bold  Ctrl+B", 30.0, FontWeights.Bold);
		formatTools.Children.Add(_boldButton);
		_italicButton = TextToolButton("I", delegate { Execute(EditingCommands.ToggleItalic); }, "Italic  Ctrl+I", 30.0, FontWeights.Normal, FontStyles.Italic);
		formatTools.Children.Add(_italicButton);
		_underlineButton = TextToolButton("U", delegate { Execute(EditingCommands.ToggleUnderline); }, "Underline  Ctrl+U", 30.0, FontWeights.Normal, FontStyles.Normal, true);
		formatTools.Children.Add(_underlineButton);
		formatTools.Children.Add(Separator());
		_bulletButton = TextToolButton("• ≡", delegate { ToggleList(EditingCommands.ToggleBullets); }, "Bulleted list  Ctrl+Shift+L", 42.0);
		formatTools.Children.Add(_bulletButton);
		_numberButton = TextToolButton("1. ≡", delegate { ToggleList(EditingCommands.ToggleNumbering); }, "Numbered list  Ctrl+Shift+N", 48.0);
		formatTools.Children.Add(_numberButton);
		formatTools.Children.Add(Separator());
		_colorButton = TextToolButton("A", delegate { _colorPopup.IsOpen = !_colorPopup.IsOpen; }, "Text color", 34.0, FontWeights.SemiBold);
		formatTools.Children.Add(_colorButton);
		BuildHeadingPopup();
		BuildColorPopup();

		_tabs = MakePanel();
		Grid.SetRow(_tabs, 3);
		_root.Children.Add(_tabs);
		ScrollViewer tabScroll = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal }
		};
		_tabsPanel = (StackPanel)tabScroll.Content;
		_tabs.Child = tabScroll;

		_editor = new System.Windows.Controls.RichTextBox
		{
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			AcceptsTab = true,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			FontFamily = _appFont,
			FontSize = 14.0,
			IsUndoEnabled = true,
			UndoLimit = 200
		};
		ApplyScrollBarStyle(_editor);
		SpellCheck.SetIsEnabled(_editor, false);
		Grid.SetRow(_editor, 4);
		_root.Children.Add(_editor);
		_editor.TextChanged += OnEditorChanged;
		_editor.SelectionChanged += delegate { UpdateStatus(); UpdateFormatState(); };
		_editor.PreviewMouseRightButtonUp += delegate(object _, MouseButtonEventArgs e) { OpenEditorContext(); e.Handled = true; };
		_editor.PreviewMouseWheel += delegate(object _, MouseWheelEventArgs e)
		{
			if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				ChangeZoom(e.Delta > 0 ? 0.1 : -0.1);
				e.Handled = true;
			}
		};
		_editor.PreviewKeyDown += EditorPreviewKeyDown;
		_editor.PreviewTextInput += EditorPreviewTextInput;
		System.Windows.DataObject.AddPastingHandler(_editor, OnPasting);
		BuildContextPopup();
		BuildFindPopup();

		_status = new Border { BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0) };
		Grid.SetRow(_status, 5);
		_root.Children.Add(_status);
		Grid statusGrid = new Grid { Margin = new Thickness(10.0, 0.0, 10.0, 0.0) };
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18.0) });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18.0) });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18.0) });
		statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_status.Child = statusGrid;
		StackPanel savePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
		savePanel.Children.Add(new System.Windows.Shapes.Ellipse { Width = 5.0, Height = 5.0, Margin = new Thickness(0.0, 0.0, 6.0, 0.0), Fill = _theme.Accent });
		_saveStatus = StatusText("Unsaved", System.Windows.HorizontalAlignment.Left);
		savePanel.Children.Add(_saveStatus);
		statusGrid.Children.Add(savePanel);
		_positionStatus = StatusText("Ln 1, Col 1"); Grid.SetColumn(_positionStatus, 2); statusGrid.Children.Add(_positionStatus);
		statusGrid.Children.Add(StatusDivider(3));
		_wordsStatus = StatusText("0 words"); Grid.SetColumn(_wordsStatus, 4); statusGrid.Children.Add(_wordsStatus);
		statusGrid.Children.Add(StatusDivider(5));
		_formatStatus = StatusText("GrassiNotes"); Grid.SetColumn(_formatStatus, 6); statusGrid.Children.Add(_formatStatus);
		statusGrid.Children.Add(StatusDivider(7));
		_zoomStatus = StatusText("100%"); Grid.SetColumn(_zoomStatus, 8); statusGrid.Children.Add(_zoomStatus);
	}

	private Border StatusDivider(int column)
	{
		Border b = new Border { Width = 1.0, Height = 11.0, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(b, column);
		return b;
	}

	private Border MakePanel()
	{
		return new Border { BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0), SnapsToDevicePixels = true };
	}

	private static Separator Separator()
	{
		return new Separator { Width = 1.0, Height = 18.0, Margin = new Thickness(6.0, 6.0, 6.0, 6.0) };
	}

	private TextBlock StatusText(string text, System.Windows.HorizontalAlignment align = System.Windows.HorizontalAlignment.Center)
	{
		return new TextBlock
		{
			Text = text,
			FontSize = 11.5,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = align
		};
	}

	private System.Windows.Controls.Image LoadLogo()
	{
		try
		{
			using MemoryStream streamSource = new MemoryStream(EmbeddedAssets.AppPng);
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = streamSource;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return new System.Windows.Controls.Image { Source = bitmapImage };
		}
		catch { return new System.Windows.Controls.Image(); }
	}

	private System.Windows.Controls.Button ChromeButton(string glyph, RoutedEventHandler click, string tip, string? name = null)
	{
		System.Windows.Controls.Button button = new System.Windows.Controls.Button
		{
			Width = 44.0,
			Height = 36.0,
			Background = System.Windows.Media.Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(0.0),
			Content = Glyph(glyph, 12.0),
			ToolTip = tip,
			Cursor = System.Windows.Input.Cursors.Arrow,
			Template = FlatButtonTemplate(0.0)
		};
		if (name != null) button.Name = name;
		button.Click += click;
		button.MouseEnter += delegate
		{
			button.Background = name == "CloseButton" ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(196, 43, 28)) : _theme.Hover;
			if (name == "CloseButton" && button.Content is TextBlock t) t.Foreground = System.Windows.Media.Brushes.White;
		};
		button.MouseLeave += delegate
		{
			button.Background = System.Windows.Media.Brushes.Transparent;
			if (button.Content is TextBlock t) t.Foreground = _theme.Text;
		};
		button.PreviewMouseLeftButtonDown += delegate { button.Background = name == "CloseButton" ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 38, 24)) : _theme.Pressed; };
		return button;
	}

	private System.Windows.Controls.Button ToolButton(string glyph, RoutedEventHandler click, string tip)
	{
		System.Windows.Controls.Button button = BaseButton(30.0, 30.0);
		button.Margin = new Thickness(0.0, 0.0, 4.0, 0.0);
		button.Content = Glyph(glyph, 16.0);
		button.ToolTip = tip;
		button.Click += click;
		return button;
	}

	private System.Windows.Controls.Button TextToolButton(string text, RoutedEventHandler click, string tip, double width, FontWeight? weight = null, System.Windows.FontStyle? style = null, bool underline = false)
	{
		System.Windows.Controls.Button button = BaseButton(width, 30.0);
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontSize = 12.5,
			FontWeight = weight ?? FontWeights.Normal,
			FontStyle = style ?? FontStyles.Normal,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center
		};
		if (underline) textBlock.TextDecorations = TextDecorations.Underline;
		button.Content = textBlock;
		button.Margin = new Thickness(0.0, 0.0, 4.0, 0.0);
		button.ToolTip = tip;
		button.Click += click;
		return button;
	}

	private System.Windows.Controls.Button BaseButton(double w, double h)
	{
		System.Windows.Controls.Button button = new System.Windows.Controls.Button
		{
			Width = w,
			Height = h,
			Background = System.Windows.Media.Brushes.Transparent,
			BorderBrush = System.Windows.Media.Brushes.Transparent,
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(0.0),
			Cursor = System.Windows.Input.Cursors.Hand,
			HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Template = FlatButtonTemplate(5.0)
		};
		button.MouseEnter += OnBaseButtonMouseEnter;
		button.MouseLeave += OnBaseButtonMouseLeave;
		button.PreviewMouseLeftButtonDown += delegate { button.Background = _theme.Pressed; };
		button.PreviewMouseLeftButtonUp += delegate { button.Background = _theme.Hover; };
		return button;
	}

	private void OnBaseButtonMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button && button.IsEnabled)
		{
			button.Background = _theme.Hover;
			button.BorderBrush = _theme.Border;
		}
	}

	private void OnBaseButtonMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			bool selected = button.Tag is bool b && b;
			button.Background = selected ? _theme.Hover : System.Windows.Media.Brushes.Transparent;
			button.BorderBrush = selected ? _theme.Accent : System.Windows.Media.Brushes.Transparent;
		}
	}

	private TextBlock Glyph(string glyph, double size)
	{
		return new TextBlock
		{
			Text = glyph,
			FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
			FontSize = size,
			Foreground = _theme.Text,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};
	}

	private static ControlTemplate FlatButtonTemplate(double radius)
	{
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BorderThicknessProperty));
		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);
		presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(presenter);
		return new ControlTemplate(typeof(System.Windows.Controls.Button)) { VisualTree = border };
	}

	private void ApplyScrollBarStyle(FrameworkElement owner)
	{
		owner.Resources["ScrollThumbBrush"] = _theme.ScrollThumb;
		owner.Resources["ScrollThumbHoverBrush"] = _theme.ScrollThumbHover;
		owner.Resources["ScrollAccentBrush"] = _theme.Accent;
		const string xaml = @"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='{x:Type ScrollBar}'>
  <Setter Property='Width' Value='10'/>
  <Setter Property='Background' Value='Transparent'/>
  <Setter Property='BorderThickness' Value='0'/>
  <Setter Property='Template'>
    <Setter.Value>
      <ControlTemplate TargetType='{x:Type ScrollBar}'>
        <Grid Background='Transparent'>
          <Track x:Name='PART_Track' IsDirectionReversed='True' Focusable='False'>
            <Track.DecreaseRepeatButton>
              <RepeatButton Command='{x:Static ScrollBar.PageUpCommand}' Opacity='0' Focusable='False'/>
            </Track.DecreaseRepeatButton>
            <Track.Thumb>
              <Thumb Margin='2,3' MinHeight='24' Background='{DynamicResource ScrollThumbBrush}'>
                <Thumb.Template>
                  <ControlTemplate TargetType='{x:Type Thumb}'>
                    <Border x:Name='ThumbRoot' CornerRadius='4' Background='{TemplateBinding Background}'/>
                    <ControlTemplate.Triggers>
                      <Trigger Property='IsMouseOver' Value='True'>
                        <Setter TargetName='ThumbRoot' Property='Background' Value='{DynamicResource ScrollThumbHoverBrush}'/>
                      </Trigger>
                      <Trigger Property='IsDragging' Value='True'>
                        <Setter TargetName='ThumbRoot' Property='Background' Value='{DynamicResource ScrollAccentBrush}'/>
                      </Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate>
                </Thumb.Template>
              </Thumb>
            </Track.Thumb>
            <Track.IncreaseRepeatButton>
              <RepeatButton Command='{x:Static ScrollBar.PageDownCommand}' Opacity='0' Focusable='False'/>
            </Track.IncreaseRepeatButton>
          </Track>
        </Grid>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>";
		owner.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = (Style)XamlReader.Parse(xaml);
	}

	private void BuildHeadingPopup()
	{
		StackPanel panel = new StackPanel();
		(string Label, string Icon, int Level)[] options =
		{
			("Normal", "N", 0),
			("Heading 1", "H1", 1),
			("Heading 2", "H2", 2),
			("Heading 3", "H3", 3),
			("Heading 4", "H4", 4)
		};
		foreach (var option in options)
		{
			System.Windows.Controls.Button item = PopupMenuButton(option.Icon, option.Label, "", delegate
			{
				ApplyHeading(option.Level);
				_headingPopup.IsOpen = false;
			});
			item.Tag = option.Level;
			panel.Children.Add(item);
		}
		_headingPopup = CreatePopup(_headingButton, panel, 198.0);
	}

	private void BuildColorPopup()
	{
		UniformGrid uniformGrid = new UniformGrid
		{
			Columns = 5,
			Margin = new Thickness(6.0)
		};
		string[] array = new string[15]
		{
			"#111827", "#FFFFFF", "#DC2626", "#EA580C", "#CA8A04", "#16A34A", "#0891B2", "#2563EB", "#7C3AED", "#DB2777",
			"#64748B", "#94A3B8", "#0F766E", "#4F46E5", "#9333EA"
		};
		foreach (string value in array)
		{
			SolidColorBrush brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value));
			SolidColorBrush normalBorder = new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 127, 127, 127));
			System.Windows.Controls.Button button = new System.Windows.Controls.Button
			{
				Width = 26.0,
				Height = 26.0,
				Margin = new Thickness(3.0),
				Padding = new Thickness(0.0),
				Tag = "ColorSwatch",
				Background = brush,
				BorderBrush = normalBorder,
				BorderThickness = new Thickness(1.0),
				Cursor = System.Windows.Input.Cursors.Hand,
				Template = FlatButtonTemplate(5.0)
			};
			button.MouseEnter += delegate { button.BorderBrush = _theme.Accent; };
			button.MouseLeave += delegate { button.BorderBrush = normalBorder; };
			button.PreviewMouseLeftButtonDown += delegate { button.Opacity = 0.72; };
			button.PreviewMouseLeftButtonUp += delegate { button.Opacity = 1.0; };
			button.Click += delegate
			{
				_editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
				_colorPopup.IsOpen = false;
				_editor.Focus();
			};
			uniformGrid.Children.Add(button);
		}
		_colorPopup = new Popup
		{
			PlacementTarget = _colorButton,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			AllowsTransparency = true,
			Child = new Border
			{
				Child = uniformGrid,
				CornerRadius = new CornerRadius(7.0),
				BorderThickness = new Thickness(1.0),
				Width = 178.0
			}
		};
	}

	private Popup CreatePopup(UIElement target, UIElement content, double width)
	{
		Border shell = new Border
		{
			Child = content,
			Padding = new Thickness(6.0),
			CornerRadius = new CornerRadius(9.0),
			BorderThickness = new Thickness(1.0),
			Width = width,
			Margin = new Thickness(6.0),
			Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18.0, ShadowDepth = 3.0, Opacity = 0.24 }
		};
		return new Popup
		{
			PlacementTarget = target,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			AllowsTransparency = true,
			PopupAnimation = PopupAnimation.Fade,
			Child = shell
		};
	}

	private System.Windows.Controls.Button PopupMenuButton(string icon, string text, string shortcut, RoutedEventHandler action)
	{
		Grid row = new Grid { Margin = new Thickness(7.0, 0.0, 8.0, 0.0) };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(31.0) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		TextBlock iconBlock = new TextBlock
		{
			Tag = "Icon",
			Text = icon,
			FontFamily = icon.Length > 2 || icon[0] >= '\ue000' ? new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets") : _appFont,
			FontSize = icon.Length <= 2 && icon[0] < '\ue000' ? 11.0 : 14.0,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};
		row.Children.Add(iconBlock);
		TextBlock label = new TextBlock { Text = text, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(label, 1); row.Children.Add(label);
		TextBlock keys = new TextBlock { Tag = "Muted", Text = shortcut, FontSize = 10.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16.0, 0.0, 0.0, 0.0) };
		Grid.SetColumn(keys, 2); row.Children.Add(keys);
		System.Windows.Controls.Button button = BaseButton(double.NaN, 36.0);
		button.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
		button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
		button.BorderThickness = new Thickness(0.0);
		button.Content = row;
		button.Click += action;
		return button;
	}

	private void BuildContextPopup()
	{
		_contextPanel = new StackPanel();
		AddContext("\ue7a7", "Undo", "Ctrl+Z", delegate { _editor.Undo(); }, () => _editor.CanUndo);
		AddContext("\ue7a6", "Redo", "Ctrl+Y", delegate { _editor.Redo(); }, () => _editor.CanRedo);
		_contextPanel.Children.Add(PopupSeparator());
		AddContext("\ue8c6", "Cut", "Ctrl+X", delegate { _editor.Cut(); }, () => !_editor.Selection.IsEmpty);
		AddContext("\ue8c8", "Copy", "Ctrl+C", delegate { _editor.Copy(); }, () => !_editor.Selection.IsEmpty);
		AddContext("\ue8c8", "Copy as Markdown", "", CopyMarkdown, () => !_editor.Selection.IsEmpty);
		AddContext("\ue77f", "Paste", "Ctrl+V", delegate { ApplicationCommands.Paste.Execute(null, _editor); }, () => System.Windows.Clipboard.ContainsText() || System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.Rtf) || System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.XamlPackage));
		_contextPanel.Children.Add(PopupSeparator());
		AddContext("\ue8b3", "Select all", "Ctrl+A", delegate { _editor.SelectAll(); }, () => true);
		_contextPopup = CreatePopup(_editor, _contextPanel, 244.0);
		_contextPopup.Placement = PlacementMode.MousePoint;
	}

	private Border PopupSeparator()
	{
		return new Border { Height = 1.0, Margin = new Thickness(9.0, 5.0, 9.0, 5.0) };
	}

	private void AddContext(string icon, string text, string shortcut, Action action, Func<bool> enabled)
	{
		System.Windows.Controls.Button button = PopupMenuButton(icon, text, shortcut, delegate
		{
			try { action(); } catch { }
			_contextPopup.IsOpen = false;
			_editor.Focus();
		});
		button.Tag = enabled;
		_contextPanel.Children.Add(button);
	}

	private void OpenEditorContext()
	{
		foreach (System.Windows.Controls.Button item in _contextPanel.Children.OfType<System.Windows.Controls.Button>())
		{
			if (item.Tag is Func<bool> func)
			{
				try
				{
					item.IsEnabled = func();
				}
				catch
				{
					item.IsEnabled = false;
				}
				item.Opacity = item.IsEnabled ? 1.0 : 0.42;
			}
		}
		_contextPopup.IsOpen = true;
	}

	private void BuildFindPopup()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(6.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(220.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(70.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(32.0)
		});
		_findBox = new System.Windows.Controls.TextBox
		{
			Height = 30.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
			BorderThickness = new Thickness(1.0)
		};
		_findBox.TextChanged += delegate
		{
			RefreshFind();
		};
		grid.Children.Add(_findBox);
		_findCount = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			FontSize = 11.0
		};
		Grid.SetColumn(_findCount, 1);
		grid.Children.Add(_findCount);
		System.Windows.Controls.Button button = BaseButton(28.0, 28.0);
		button.Content = Glyph("\ue711", 12.0);
		button.Click += delegate
		{
			_findPopup.IsOpen = false;
			_editor.Focus();
		};
		Grid.SetColumn(button, 2);
		grid.Children.Add(button);
		_findPopup = new Popup
		{
			PlacementTarget = _editor,
			Placement = PlacementMode.Top,
			HorizontalOffset = 300.0,
			VerticalOffset = 55.0,
			StaysOpen = true,
			AllowsTransparency = true,
			Child = new Border
			{
				Child = grid,
				CornerRadius = new CornerRadius(7.0),
				BorderThickness = new Thickness(1.0)
			}
		};
	}

	private void NewDocument()
	{
		DocumentTab documentTab = new DocumentTab
		{
			Title = $"Untitled {_untitled++}",
			Kind = DocumentKind.Native,
			Document = DocumentSerializer.NewDocument("")
		};
		ConfigureDocument(documentTab.Document);
		AddDocument(documentTab, select: true);
		_saveStatus.Text = "Unsaved";
	}

	private void AddDocument(DocumentTab d, bool select)
	{
		d.PropertyChanged += delegate
		{
			RebuildTabs();
		};
		_documents.Add(d);
		if (select)
		{
			SelectDocument(d);
		}
		else
		{
			RebuildTabs();
		}
	}

	private void SelectDocument(DocumentTab d)
	{
		if (_active != d)
		{
			_suppress = true;
			if (_active != null)
			{
				_active.IsActive = false;
			}
			_active = d;
			d.IsActive = true;
			_editor.Document = d.Document;
			_editor.FontSize = 15.0 * d.Zoom;
			_suppress = false;
			UpdateTitle();
			RebuildTabs();
			UpdateStatus();
			UpdateFormatState();
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				_editor.Focus();
			});
		}
	}

	private void ConfigureDocument(FlowDocument doc)
	{
		doc.FontFamily = _appFont;
		doc.FontSize = 14.0;
		doc.PagePadding = new Thickness(0.0);
		doc.Foreground = _theme.Text;
		NormalizeDocumentFont(doc);
		foreach (Paragraph paragraph in EnumerateParagraphs(doc.Blocks))
		{
			ApplyAutoDirection(paragraph);
		}
	}

	private void NormalizeDocumentFont(FlowDocument document)
	{
		try
		{
			document.FontFamily = _appFont;
			new TextRange(document.ContentStart, document.ContentEnd)
				.ApplyPropertyValue(TextElement.FontFamilyProperty, _appFont);
		}
		catch { }
	}

	private static bool IsRtlCharacter(char c)
	{
		return (c >= '\u0590' && c <= '\u08FF') ||
		       (c >= '\uFB1D' && c <= '\uFDFF') ||
		       (c >= '\uFE70' && c <= '\uFEFF');
	}

	private static string DetectDirection(Paragraph paragraph)
	{
		string text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
		foreach (char c in text)
		{
			if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c) && !char.IsSymbol(c) && !char.IsDigit(c))
				return IsRtlCharacter(c) ? "RTL" : "LTR";
		}
		return "Auto";
	}

	private void ApplyAutoDirection(Paragraph paragraph)
	{
		string explicitDirection = EditorMetadata.GetExplicitDirection(paragraph);
		if (explicitDirection == "RTL") { ApplyParagraphDirection(paragraph, true, true); return; }
		if (explicitDirection == "LTR") { ApplyParagraphDirection(paragraph, false, true); return; }
		string detected = DetectDirection(paragraph);
		if (detected != "Auto") ApplyParagraphDirection(paragraph, detected == "RTL", false);
	}

	private static string EffectiveDirection(Paragraph paragraph)
	{
		string explicitDirection = EditorMetadata.GetExplicitDirection(paragraph);
		if (explicitDirection == "RTL" || explicitDirection == "LTR") return explicitDirection;
		string detected = DetectDirection(paragraph);
		if (detected != "Auto") return detected;
		return paragraph.FlowDirection == System.Windows.FlowDirection.RightToLeft ? "RTL" : "LTR";
	}

	private void ApplyParagraphDirection(Paragraph paragraph, bool rtl, bool explicitChoice)
	{
		System.Windows.FlowDirection flow = rtl ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
		TextAlignment alignment = rtl ? TextAlignment.Right : TextAlignment.Left;
		ParagraphDirectionFormatter.Apply(
			paragraph,
			flow,
			alignment,
			explicitChoice ? (rtl ? "RTL" : "LTR") : null);
	}

	internal RichTextBox EditorForTesting => _editor;

	internal void InitializeForTesting()
	{
		if (_active == null) NewDocument();
	}

	internal void SetSelectionDirectionForTesting(bool rtl)
	{
		SetSelectionDirection(rtl);
	}

	private void SetSelectionDirection(bool rtl)
	{
		List<Paragraph> paragraphs = SelectedParagraphs().Distinct().ToList();
		if (paragraphs.Count == 0 && _editor.CaretPosition.Paragraph is Paragraph current) paragraphs.Add(current);
		_suppress = true;
		try
		{
			foreach (Paragraph paragraph in paragraphs) ApplyParagraphDirection(paragraph, rtl, true);
		}
		finally { _suppress = false; }
		_pendingTypingDirection = rtl ? "RTL" : "LTR";
		_editor.Focus();
		QueueAutosave();
	}

	private void ScheduleDirectionReapply(Paragraph? paragraph, string? forcedDirection = null, bool makeExplicit = false)
	{
		if (paragraph == null) return;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (paragraph.Parent == null) paragraph = _editor.CaretPosition.Paragraph;
			if (paragraph == null) return;
			string direction = forcedDirection ?? EffectiveDirection(paragraph);
			_suppress = true;
			try { ApplyParagraphDirection(paragraph, direction == "RTL", makeExplicit || EditorMetadata.GetExplicitDirection(paragraph) != "Auto"); }
			finally { _suppress = false; }
		}, DispatcherPriority.Input);
	}

	private void ToggleList(RoutedUICommand command)
	{
		Paragraph? before = _editor.CaretPosition.Paragraph;
		bool rtl = before?.FlowDirection == System.Windows.FlowDirection.RightToLeft;
		string explicitDirection = before == null ? "Auto" : EditorMetadata.GetExplicitDirection(before);
		Execute(command);
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			Paragraph? after = _editor.CaretPosition.Paragraph;
			if (after != null)
			{
				bool direction = explicitDirection == "RTL" || (explicitDirection == "Auto" && rtl);
				ApplyParagraphDirection(after, direction, explicitDirection != "Auto");
			}
			UpdateFormatState();
		}, DispatcherPriority.Input);
	}

	private void OnPasting(object sender, DataObjectPastingEventArgs e)
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			NormalizeDocumentFont(_editor.Document);
			Paragraph? paragraph = _editor.CaretPosition.Paragraph;
			if (paragraph != null) ApplyAutoDirection(paragraph);
			UpdateFormatState();
		}, DispatcherPriority.Background);
	}

	private void RebuildTabs()
	{
		if (_tabsPanel == null) return;
		_tabsPanel.Children.Clear();
		foreach (DocumentTab d in _documents)
		{
			Border tab = new Border
			{
				Width = 164.0,
				Height = 32.0,
				BorderThickness = new Thickness(1.0, 0.0, 1.0, d == _active ? 2.0 : 0.0),
				BorderBrush = d == _active ? _theme.Accent : _theme.Border,
				Background = d == _active ? _theme.SurfaceAlt : System.Windows.Media.Brushes.Transparent,
				CornerRadius = new CornerRadius(5.0, 5.0, 0.0, 0.0),
				Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
				Cursor = System.Windows.Input.Cursors.Hand
			};
			Grid grid = new Grid { Margin = new Thickness(10.0, 0.0, 4.0, 0.0) };
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24.0) });
			tab.Child = grid;
			TextBlock label = new TextBlock
			{
				Text = d.DisplayTitle,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				Foreground = d == _active ? _theme.Text : _theme.Muted,
				FontSize = 12.0,
				FontWeight = d == _active ? FontWeights.SemiBold : FontWeights.Normal
			};
			grid.Children.Add(label);
			System.Windows.Controls.Button close = BaseButton(24.0, 24.0);
			close.Tag = "TabClose";
			close.Content = Glyph("\ue711", 10.0);
			close.Background = System.Windows.Media.Brushes.Transparent;
			close.BorderThickness = new Thickness(0.0);
			close.Click += async delegate(object _, RoutedEventArgs e) { e.Handled = true; await CloseDocumentAsync(d); };
			Grid.SetColumn(close, 1);
			grid.Children.Add(close);
			tab.MouseEnter += delegate { if (d != _active) tab.Background = _theme.Hover; };
			tab.MouseLeave += delegate { tab.Background = d == _active ? _theme.SurfaceAlt : System.Windows.Media.Brushes.Transparent; };
			tab.MouseLeftButtonDown += delegate { SelectDocument(d); };
			_tabsPanel.Children.Add(tab);
		}
		System.Windows.Controls.Button add = BaseButton(32.0, 32.0);
		add.Tag = "NewTab";
		add.Content = Glyph("\ue710", 15.0);
		add.Margin = new Thickness(5.0, 0.0, 0.0, 0.0);
		add.Background = System.Windows.Media.Brushes.Transparent;
		add.BorderBrush = _theme.Border;
		add.Click += delegate { NewDocument(); };
		_tabsPanel.Children.Add(add);
	}

	private async Task OpenDocumentAsync()
	{
		string text = FileDialogs.Open();
		if (text != null)
		{
			await OpenPathAsync(text);
		}
	}

	private async Task OpenPathAsync(string path)
	{
		_ = 2;
		try
		{
			string full = Path.GetFullPath(path);
			DocumentTab documentTab = ((IEnumerable<DocumentTab>)_documents).FirstOrDefault((DocumentTab x) => x.FilePath != null && string.Equals(Path.GetFullPath(x.FilePath), full, StringComparison.OrdinalIgnoreCase));
			if (documentTab != null)
			{
				SelectDocument(documentTab);
				return;
			}
			DocumentKind kind = FileDialogs.KindFromPath(full);
			FlowDocument flowDocument;
			switch (kind)
			{
			case DocumentKind.Native:
				flowDocument = DocumentSerializer.FromXaml(await File.ReadAllTextAsync(full));
				break;
			case DocumentKind.Markdown:
				flowDocument = MarkdownCodec.Parse(await File.ReadAllTextAsync(full), _appFont);
				break;
			case DocumentKind.RichText:
			{
				flowDocument = DocumentSerializer.NewDocument("");
				using (FileStream stream = File.OpenRead(full))
				{
					new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd).Load(stream, System.Windows.DataFormats.Rtf);
				}
				break;
			}
			default:
				flowDocument = DocumentSerializer.NewDocument(await File.ReadAllTextAsync(full));
				break;
			}
			ConfigureDocument(flowDocument);
			DocumentTab d = new DocumentTab
			{
				Title = Path.GetFileName(full),
				FilePath = full,
				Kind = kind,
				Document = flowDocument,
				IsDirty = false
			};
			AddDocument(d, select: true);
			_saveStatus.Text = "Saved";
			SaveSession();
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show(this, "Could not open the file.\n\n" + ex.Message, "GrassiNotes", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private async Task<bool> SaveActiveAsync(bool saveAs)
	{
		bool flag = _active != null;
		if (flag)
		{
			flag = await SaveDocumentAsync(_active, saveAs);
		}
		return flag;
	}

	private async Task<bool> SaveDocumentAsync(DocumentTab d, bool saveAs = false)
	{
		string path = d.FilePath;
		if (saveAs || string.IsNullOrWhiteSpace(path))
		{
			path = FileDialogs.Save((d.FilePath == null) ? (d.Title + ".grass") : Path.GetFileName(d.FilePath));
			if (path == null)
			{
				return false;
			}
		}
		try
		{
			_saveStatus.Text = "Saving…";
			DocumentKind kind = FileDialogs.KindFromPath(path);
			switch (kind)
			{
			case DocumentKind.Native:
				await File.WriteAllTextAsync(path, DocumentSerializer.ToXaml(d.Document), (Encoding)new UTF8Encoding(false));
				break;
			case DocumentKind.Markdown:
				await File.WriteAllTextAsync(path, MarkdownCodec.Export(d.Document), (Encoding)new UTF8Encoding(false));
				break;
			case DocumentKind.RichText:
			{
				using (FileStream stream = File.Create(path))
				{
					new TextRange(d.Document.ContentStart, d.Document.ContentEnd).Save(stream, System.Windows.DataFormats.Rtf);
				}
				break;
			}
			default:
				await File.WriteAllTextAsync(path, DocumentSerializer.PlainText(d.Document), (Encoding)new UTF8Encoding(false));
				break;
			}
			d.FilePath = path;
			d.Title = Path.GetFileName(path);
			d.Kind = kind;
			d.IsDirty = false;
			_saveStatus.Text = "Saved";
			UpdateTitle();
			SaveSession();
			return true;
		}
		catch (Exception ex)
		{
			_saveStatus.Text = "Save failed";
			System.Windows.MessageBox.Show(this, "Could not save the file.\n\n" + ex.Message, "GrassiNotes", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
	}

	private async Task CloseDocumentAsync(DocumentTab d)
	{
		if (d.IsDirty)
		{
			MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(this, "Save changes to " + d.Title + "?", "GrassiNotes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
			if (messageBoxResult == MessageBoxResult.Cancel)
			{
				return;
			}
			bool flag = messageBoxResult == MessageBoxResult.Yes;
			if (flag)
			{
				flag = !(await SaveDocumentAsync(d));
			}
			if (flag)
			{
				return;
			}
		}
		int value = _documents.IndexOf(d);
		_documents.Remove(d);
		if (_documents.Count == 0)
		{
			NewDocument();
		}
		else if (_active == d)
		{
			SelectDocument(_documents[Math.Clamp(value, 0, _documents.Count - 1)]);
		}
		RebuildTabs();
		SaveSession();
	}

	private void OnEditorChanged(object sender, TextChangedEventArgs e)
	{
		if (!_suppress && _active != null)
		{
			Paragraph? paragraph = _editor.CaretPosition.Paragraph;
			string? pendingTypingDirection = _pendingTypingDirection;
			_pendingTypingDirection = null;
			EnforceDirectionAfterChange(paragraph, pendingTypingDirection);
			ScheduleDirectionReapply(paragraph);
			_active.IsDirty = true;
			_saveStatus.Text = "Unsaved";
			UpdateTitle();
			UpdateStatus();
			QueueAutosave();
			if (_findPopup.IsOpen)
			{
				RefreshFind();
			}
		}
	}

	private void EnforceDirectionAfterChange(Paragraph? paragraph, string? forcedDirection)
	{
		if (paragraph == null) return;

		bool previousSuppress = _suppress;
		_suppress = true;
		try
		{
			ParagraphDirectionFormatter.EnforceDirectionAndAlignment(
				paragraph,
				forcedDirection);
		}
		finally
		{
			_suppress = previousSuppress;
		}

		_pendingAlignmentParagraphs.Add(paragraph);
		if (_alignmentEnforcementOperation != null) return;

		_alignmentEnforcementOperation = base.Dispatcher.BeginInvoke((Action)delegate
		{
			Paragraph[] pendingParagraphs = _pendingAlignmentParagraphs.ToArray();
			_pendingAlignmentParagraphs.Clear();
			_alignmentEnforcementOperation = null;

			foreach (Paragraph pendingParagraph in pendingParagraphs)
			{
				Paragraph? target = pendingParagraph.Parent == null
					? _editor.CaretPosition.Paragraph
					: pendingParagraph;
				if (target == null) continue;

				bool wasSuppressed = _suppress;
				_suppress = true;
				try
				{
					ParagraphDirectionFormatter.EnforceDirectionAndAlignment(target);
				}
				finally
				{
					_suppress = wasSuppressed;
				}
			}
		}, DispatcherPriority.ContextIdle);
	}

	private void QueueAutosave()
	{
		_autosave.Stop();
		_autosave.Start();
	}

	private void SaveSession()
	{
		SessionService.Save(new SessionState
		{
			Theme = _themeName,
			AlwaysOnTop = base.Topmost,
			ActiveIndex = ((_active != null) ? Math.Max(0, _documents.IndexOf(_active)) : 0),
			Documents = ((IEnumerable<DocumentTab>)_documents).Select((DocumentTab d) => new SessionDocument
			{
				Id = d.Id,
				Title = d.Title,
				FilePath = d.FilePath,
				IsDirty = d.IsDirty,
				Kind = d.Kind,
				Zoom = d.Zoom,
				Xaml = DocumentSerializer.ToXaml(d.Document)
			}).ToList()
		});
	}

	private void UpdateTitle()
	{
		_titleText.Text = ((_active == null) ? "GrassiNotes" : (_active.Title + " — GrassiNotes"));
		base.Title = _titleText.Text;
	}

	private void UpdateStatus()
	{
		if (_active != null)
		{
			string text = new TextRange(_editor.Document.ContentStart, _editor.Document.ContentEnd).Text;
			string text2 = new TextRange(_editor.Document.ContentStart, _editor.CaretPosition).Text;
			int num = text2.Count((char c) => c == '\n') + 1;
			int num2 = text2.LastIndexOf('\n');
			int val = text2.Length - (num2 + 1) + 1;
			int count = Regex.Matches(text, "[\\p{L}\\p{N}_]+", RegexOptions.CultureInvariant).Count;
			_positionStatus.Text = $"Ln {num}, Col {Math.Max(1, val)}";
			_wordsStatus.Text = $"{count} words";
			TextBlock formatStatus = _formatStatus;
			formatStatus.Text = _active.Kind switch
			{
				DocumentKind.Native => "GrassiNotes", 
				DocumentKind.Markdown => "Markdown", 
				DocumentKind.RichText => "RTF", 
				_ => "UTF-8", 
			};
			_zoomStatus.Text = $"{Math.Round(_active.Zoom * 100.0)}%";
		}
	}

	private void UpdateFormatState()
	{
		SetToggle(_boldButton, IsSelectionValue(TextElement.FontWeightProperty, FontWeights.Bold));
		SetToggle(_italicButton, IsSelectionValue(TextElement.FontStyleProperty, FontStyles.Italic));
		object propertyValue = _editor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
		SetToggle(_underlineButton, propertyValue != DependencyProperty.UnsetValue && propertyValue is TextDecorationCollection decorations && decorations == TextDecorations.Underline);
		Paragraph? paragraph = _editor.CaretPosition.Paragraph;
		int level = paragraph?.Tag?.ToString() switch { "h1" => 1, "h2" => 2, "h3" => 3, "h4" => 4, _ => 0 };
		string label = level == 0 ? "Normal" : "Heading " + level;
		_headingButton.Content = new TextBlock { Text = label, FontSize = 12.0, Foreground = _theme.Text, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
		if (_headingPopup?.Child is Border shell && shell.Child is StackPanel panel)
		{
			foreach (System.Windows.Controls.Button button in panel.Children.OfType<System.Windows.Controls.Button>())
			{
				bool selected = button.Tag is int itemLevel && itemLevel == level;
				button.Background = selected ? _theme.Hover : System.Windows.Media.Brushes.Transparent;
				button.BorderBrush = System.Windows.Media.Brushes.Transparent;
				if (button.Content is Grid grid)
				{
					foreach (TextBlock text in grid.Children.OfType<TextBlock>())
					{
						if (text.Tag as string == "Icon") text.Foreground = selected ? _theme.Accent : _theme.Text;
						else if (text.Tag as string == "Muted") text.Foreground = _theme.Muted;
						else text.Foreground = _theme.Text;
					}
				}
			}
		}
	}

	private bool IsSelectionValue(DependencyProperty p, object target)
	{
		object propertyValue = _editor.Selection.GetPropertyValue(p);
		if (propertyValue != DependencyProperty.UnsetValue)
		{
			return object.Equals(propertyValue, target);
		}
		return false;
	}

	private void SetToggle(System.Windows.Controls.Button b, bool on)
	{
		b.Tag = on;
		b.Background = on ? _theme.Hover : System.Windows.Media.Brushes.Transparent;
		b.BorderBrush = on ? _theme.Accent : System.Windows.Media.Brushes.Transparent;
	}

	private void Execute(RoutedUICommand command)
	{
		command.Execute(null, _editor);
		_editor.Focus();
		UpdateFormatState();
	}

	private void ApplyHeading(int level)
	{
		foreach (Paragraph item in SelectedParagraphs())
		{
			TextRange paragraphRange = new TextRange(item.ContentStart, item.ContentEnd);
			paragraphRange.ApplyPropertyValue(TextElement.FontFamilyProperty, _appFont);
			if (level == 0)
			{
				item.Tag = null;
				item.FontFamily = _appFont;
				item.FontSize = 14.0;
				item.FontWeight = FontWeights.Normal;
				item.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
			}
			else
			{
				item.Tag = "h" + level;
				item.FontFamily = _appFont;
				MarkdownCodec.ApplyHeadingStyle(item, level);
				item.Margin = level switch
				{
					1 => new Thickness(0.0, 16.0, 0.0, 8.0),
					2 => new Thickness(0.0, 14.0, 0.0, 7.0),
					_ => new Thickness(0.0, 11.0, 0.0, 6.0)
				};
			}
		}
		NormalizeDocumentFont(_editor.Document);
		_editor.Focus();
		UpdateFormatState();
	}

	private IEnumerable<Paragraph> SelectedParagraphs()
	{
		TextPointer start = _editor.Selection.Start;
		TextPointer end = _editor.Selection.End;
		foreach (Paragraph item in EnumerateParagraphs(_editor.Document.Blocks))
		{
			if (item.ContentEnd.CompareTo(start) >= 0 && item.ContentStart.CompareTo(end) <= 0)
			{
				yield return item;
			}
		}
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
				foreach (Paragraph item in EnumerateParagraphs(section.Blocks))
				{
					yield return item;
				}
			}
			else
			{
				if (!(block is List list))
				{
					continue;
				}
				foreach (ListItem listItem in list.ListItems)
				{
					foreach (Paragraph item2 in EnumerateParagraphs(listItem.Blocks))
					{
						yield return item2;
					}
				}
			}
		}
	}

	private void ChangeZoom(double delta)
	{
		if (_active != null)
		{
			_active.Zoom = Math.Clamp(_active.Zoom + delta, 0.6, 2.2);
			_editor.FontSize = 15.0 * _active.Zoom;
			UpdateStatus();
			QueueAutosave();
		}
	}

	private void CopyMarkdown()
	{
		try
		{
			if (_editor.Selection.IsEmpty)
			{
				return;
			}
			FlowDocument flowDocument = DocumentSerializer.NewDocument("");
			flowDocument.Blocks.Clear();
			flowDocument.Blocks.Add(new Paragraph());
			using MemoryStream memoryStream = new MemoryStream();
			_editor.Selection.Save(memoryStream, System.Windows.DataFormats.XamlPackage);
			memoryStream.Position = 0L;
			new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd).Load(memoryStream, System.Windows.DataFormats.XamlPackage);
			System.Windows.Clipboard.SetText(MarkdownCodec.Export(flowDocument));
		}
		catch
		{
			System.Windows.Clipboard.SetText(_editor.Selection.Text);
		}
	}

	private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		bool flag = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
		bool flag2 = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
		if (TryHandleDirectionShortcut(e))
		{
			return;
		}
		if (e.Key == Key.Escape)
		{
			if (_findPopup.IsOpen)
			{
				_findPopup.IsOpen = false;
				_editor.Focus();
				e.Handled = true;
			}
		}
		else
		{
			if (!flag)
			{
				return;
			}
			if (e.Key == Key.B)
			{
				Execute(EditingCommands.ToggleBold);
				e.Handled = true;
			}
			else if (e.Key == Key.I)
			{
				Execute(EditingCommands.ToggleItalic);
				e.Handled = true;
			}
			else if (e.Key == Key.U)
			{
				Execute(EditingCommands.ToggleUnderline);
				e.Handled = true;
			}
			else if (flag2 && e.Key == Key.L)
			{
				ToggleList(EditingCommands.ToggleBullets);
				e.Handled = true;
			}
			else if (flag2 && e.Key == Key.N)
			{
				ToggleList(EditingCommands.ToggleNumbering);
				e.Handled = true;
			}
			else if (e.Key == Key.N)
			{
				NewDocument();
				e.Handled = true;
			}
			else if (e.Key == Key.O)
			{
				OpenDocumentAsync();
				e.Handled = true;
			}
			else if (e.Key == Key.S)
			{
				SaveActiveAsync(flag2);
				e.Handled = true;
			}
			else if (e.Key == Key.W)
			{
				if (_active != null)
				{
					CloseDocumentAsync(_active);
				}
				e.Handled = true;
			}
			else if (e.Key == Key.Tab)
			{
				SelectRelativeTab((!flag2) ? 1 : (-1));
				e.Handled = true;
			}
			else if (e.Key == Key.F)
			{
				ShowFind();
				e.Handled = true;
			}
			else if (e.Key == Key.Back)
			{
				EditingCommands.DeletePreviousWord.Execute(null, _editor);
				e.Handled = true;
			}
			else if (e.Key == Key.Delete)
			{
				EditingCommands.DeleteNextWord.Execute(null, _editor);
				e.Handled = true;
			}
			else if (e.Key == Key.OemPlus || e.Key == Key.Add)
			{
				ChangeZoom(0.1);
				e.Handled = true;
			}
			else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
			{
				ChangeZoom(-0.1);
				e.Handled = true;
			}
			else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
			{
				if (_active != null)
				{
					_active.Zoom = 1.0;
					_editor.FontSize = 15.0;
					UpdateStatus();
				}
				e.Handled = true;
			}
		}
	}

	private void EditorPreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		Paragraph? paragraph = _editor.CaretPosition.Paragraph;
		if (paragraph == null) return;
		string explicitDirection = EditorMetadata.GetExplicitDirection(paragraph);
		if (explicitDirection == "RTL" || explicitDirection == "LTR")
		{
			_pendingTypingDirection = explicitDirection;
			ScheduleDirectionReapply(paragraph, explicitDirection, true);
		}
		else
		{
			_pendingTypingDirection = null;
			ScheduleDirectionReapply(paragraph);
		}
	}

	private void OnPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		TryHandleDirectionShortcut(e);
	}

	private bool TryHandleDirectionShortcut(System.Windows.Input.KeyEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return false;

		Key pressedKey = e.Key switch
		{
			Key.System => e.SystemKey,
			Key.ImeProcessed => e.ImeProcessedKey,
			_ => e.Key
		};
		if (pressedKey != Key.RightShift && pressedKey != Key.LeftShift) return false;

		bool rtl = Keyboard.IsKeyDown(Key.RightShift) ||
			(pressedKey == Key.RightShift && !Keyboard.IsKeyDown(Key.LeftShift));
		SetSelectionDirection(rtl);
		e.Handled = true;
		return true;
	}

	private void EditorPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Tab && _editor.CaretPosition.Paragraph?.Parent is ListItem)
		{
			Execute(((Keyboard.Modifiers & ModifierKeys.Shift) != 0) ? EditingCommands.DecreaseIndentation : EditingCommands.IncreaseIndentation);
			e.Handled = true;
			return;
		}

		if (e.Key == Key.Enter || e.Key == Key.Return)
		{
			Paragraph? before = _editor.CaretPosition.Paragraph;
			if (before == null) return;
			bool resetHeading = ParagraphStyleFormatter.IsHeading(before);
			string explicitDirection = EditorMetadata.GetExplicitDirection(before);
			System.Windows.FlowDirection inheritedFlowDirection = before.FlowDirection;
			TextAlignment inheritedTextAlignment = before.TextAlignment;
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				Paragraph? after = _editor.CaretPosition.Paragraph;
				if (after == null) return;
				_suppress = true;
				try
				{
					if (resetHeading)
					{
						ParagraphStyleFormatter.ApplyNormal(after, _appFont);
						_editor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, _appFont);
						_editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 14.0);
						_editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
						_editor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
					}
					ParagraphDirectionFormatter.Apply(
						after,
						inheritedFlowDirection,
						inheritedTextAlignment,
						explicitDirection != "Auto" ? explicitDirection : null);
				}
				finally { _suppress = false; }
				UpdateFormatState();
			}, DispatcherPriority.Input);
		}
	}

	private void SelectRelativeTab(int delta)
	{
		if (_active != null && _documents.Count >= 2)
		{
			int num = _documents.IndexOf(_active);
			SelectDocument(_documents[(num + delta + _documents.Count) % _documents.Count]);
		}
	}

	private void ShowFind()
	{
		_findPopup.IsOpen = true;
		_findBox.Focus();
		_findBox.SelectAll();
		RefreshFind();
	}

	private void RefreshFind()
	{
		_findMatches = new List<TextRange>();
		_findIndex = -1;
		string text = _findBox.Text;
		if (string.IsNullOrEmpty(text))
		{
			_findCount.Text = "";
			return;
		}
		string text2 = new TextRange(_editor.Document.ContentStart, _editor.Document.ContentEnd).Text;
		int startIndex = 0;
		while ((startIndex = text2.IndexOf(text, startIndex, StringComparison.CurrentCultureIgnoreCase)) >= 0)
		{
			TextPointer textPointer = PointerAtOffset(_editor.Document.ContentStart, startIndex);
			TextPointer position = PointerAtOffset(textPointer, text.Length);
			_findMatches.Add(new TextRange(textPointer, position));
			startIndex += Math.Max(1, text.Length);
		}
		if (_findMatches.Count > 0)
		{
			_findIndex = 0;
			SelectFind();
		}
		else
		{
			_findCount.Text = "0 / 0";
		}
	}

	private static TextPointer PointerAtOffset(TextPointer start, int chars)
	{
		TextPointer textPointer = start;
		int num = chars;
		while (textPointer != null && num > 0)
		{
			TextPointer nextContextPosition = textPointer.GetNextContextPosition(LogicalDirection.Forward);
			if (nextContextPosition == null)
			{
				break;
			}
			if (textPointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
			{
				string textInRun = textPointer.GetTextInRun(LogicalDirection.Forward);
				int num2 = Math.Min(num, textInRun.Length);
				textPointer = textPointer.GetPositionAtOffset(num2, LogicalDirection.Forward) ?? nextContextPosition;
				num -= num2;
			}
			else
			{
				textPointer = nextContextPosition;
			}
		}
		return textPointer ?? start;
	}

	private void SelectFind()
	{
		if (_findIndex >= 0 && _findIndex < _findMatches.Count)
		{
			TextRange textRange = _findMatches[_findIndex];
			_editor.Selection.Select(textRange.Start, textRange.End);
			_editor.CaretPosition = textRange.End;
			_editor.Focus();
			_findCount.Text = $"{_findIndex + 1} / {_findMatches.Count}";
		}
	}

	private void ApplyTheme(string name, bool save = true)
	{
		_themeName = name;
		_theme = name == "Light" ? ThemePalette.Light : ThemePalette.Dark;
		base.Background = System.Windows.Media.Brushes.Transparent;
		_outer.Background = _theme.Window;
		_outer.BorderBrush = _theme.Border;
		_root.Background = _theme.Window;
		foreach (Border panel in new[] { _title, _toolbar, _format, _tabs })
		{
			panel.Background = _theme.Panel;
			panel.BorderBrush = _theme.Border;
		}
		_status.Background = _theme.SurfaceAlt;
		_status.BorderBrush = _theme.Border;
		_editor.Background = _theme.Editor;
		_editor.Foreground = _theme.Text;
		_editor.CaretBrush = _theme.Caret;
		_editor.SelectionBrush = _theme.Selection;
		_titleText.Foreground = _theme.Text;
		foreach (TextBlock status in new[] { _saveStatus, _positionStatus, _wordsStatus, _formatStatus, _zoomStatus, _findCount })
			status.Foreground = _theme.Muted;

		foreach (Separator sep in VisualChildren<Separator>(_root))
		{
			sep.Background = _theme.Border;
			sep.Foreground = _theme.Border;
		}
		foreach (Border b in VisualChildren<Border>(_status))
		{
			if (Math.Abs(b.Width - 1.0) < 0.01) b.Background = _theme.Border;
		}
		foreach (System.Windows.Shapes.Ellipse ellipse in VisualChildren<System.Windows.Shapes.Ellipse>(_status)) ellipse.Fill = _theme.Accent;

		foreach (System.Windows.Controls.Button button in VisualChildren<System.Windows.Controls.Button>(_root))
		{
			button.Foreground = _theme.Text;
			bool selected = button.Tag is bool flag && flag;
			button.Background = selected ? _theme.Hover : System.Windows.Media.Brushes.Transparent;
			button.BorderBrush = selected ? _theme.Accent : System.Windows.Media.Brushes.Transparent;
			if (button.Tag is string tag && tag == "NewTab") button.BorderBrush = _theme.Border;
			if (button.Content is TextBlock tb) tb.Foreground = _theme.Text;
		}

		foreach (Popup popup in new[] { _contextPopup, _headingPopup, _colorPopup, _findPopup })
		{
			if (popup?.Child is Border shell)
			{
				shell.Background = _theme.SurfaceAlt;
				shell.BorderBrush = _theme.Border;
				foreach (Border divider in VisualChildren<Border>(shell))
					if (Math.Abs(divider.Height - 1.0) < 0.01) divider.Background = _theme.Border;
				foreach (System.Windows.Controls.Button button in VisualChildren<System.Windows.Controls.Button>(shell))
				{
					button.Foreground = _theme.Text;
					if (button.Tag as string != "ColorSwatch")
					{
						if (!(button.Tag is int)) button.Background = System.Windows.Media.Brushes.Transparent;
						button.BorderBrush = System.Windows.Media.Brushes.Transparent;
					}
					foreach (TextBlock tb in VisualChildren<TextBlock>(button))
					{
						if (tb.Tag as string == "Muted") tb.Foreground = _theme.Muted;
						else tb.Foreground = _theme.Text;
					}
				}
			}
		}
		_findBox.Background = _theme.Editor;
		_findBox.Foreground = _theme.Text;
		_findBox.BorderBrush = _theme.Border;
		_themeButton.Content = Glyph(name == "Dark" ? "\ue706" : "\ue708", 16.0);
		foreach (DocumentTab document in _documents)
		{
			document.Document.Foreground = _theme.Text;
			NormalizeDocumentFont(document.Document);
		}
		ApplyScrollBarStyle(_editor);
		RebuildTabs();
		UpdateFormatState();
		_trayMenu?.ApplyTheme(_theme);
		if (save) SaveSession();
	}

	private static IEnumerable<T> VisualChildren<T>(DependencyObject d) where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
		{
			DependencyObject c = VisualTreeHelper.GetChild(d, i);
			if (c is T val)
			{
				yield return val;
			}
			foreach (T item in VisualChildren<T>(c))
			{
				yield return item;
			}
		}
	}

	public void EnsureTrayVisible()
	{
		if (_tray != null)
		{
			return;
		}
		_tray = new NotifyIcon
		{
			Visible = true,
			Text = "GrassiNotes"
		};
		try
		{
			using MemoryStream stream = new MemoryStream(EmbeddedAssets.AppIco);
			_tray.Icon = new Icon(stream);
		}
		catch
		{
		}
		_tray.MouseUp += delegate(object? _, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ToggleTray();
			}
			else if (e.Button == MouseButtons.Right)
			{
				ShowTrayMenu();
			}
		};
	}

	private void ShowTrayMenu()
	{
		base.Dispatcher.Invoke(delegate
		{
			_trayMenu?.Close();
			_trayMenu = new TrayMenuWindow(_theme, base.Topmost, _appFont, ShowFromTray, delegate
			{
				base.Topmost = !base.Topmost;
				SaveSession();
				_trayMenu?.SetTopmost(base.Topmost);
			}, delegate
			{
				RequestExitAsync();
			});
			_trayMenu.ShowAtCursor();
		});
	}

	private void ToggleTray()
	{
		base.Dispatcher.Invoke(delegate
		{
			if (base.IsVisible && base.WindowState != WindowState.Minimized)
			{
				HideToTray(hint: false);
			}
			else
			{
				ShowFromTray();
			}
		});
	}

	public void ShowFromTray()
	{
		_trayMenu?.Close();
		if (!base.IsVisible)
		{
			Show();
		}
		base.ShowInTaskbar = true;
		if (base.WindowState == WindowState.Minimized)
		{
			base.WindowState = WindowState.Normal;
		}
		Activate();
		base.Topmost = base.Topmost;
		_editor.Focus();
	}

	public void HideToTray(bool hint = true)
	{
		_contextPopup.IsOpen = false;
		_headingPopup.IsOpen = false;
		_colorPopup.IsOpen = false;
		_findPopup.IsOpen = false;
		SaveSession();
		base.ShowInTaskbar = false;
		Hide();
		base.Opacity = 1.0;
		if (hint && !_trayHintShown && _tray != null)
		{
			_tray.ShowBalloonTip(2500, "GrassiNotes is ready", "Press Ctrl+Alt+Shift+G or click the tray icon to open it.", ToolTipIcon.Info);
			_trayHintShown = true;
		}
	}

	private async Task RequestExitAsync()
	{
		ShowFromTray();
		var enumerator = ((IEnumerable<DocumentTab>)_documents).ToList().GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				DocumentTab current = enumerator.Current;
				if (current.IsDirty)
				{
					MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(this, "Save changes to " + current.Title + "?", "GrassiNotes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
					if (messageBoxResult == MessageBoxResult.Cancel)
					{
						return;
					}
					bool flag = messageBoxResult == MessageBoxResult.Yes;
					if (flag)
					{
						flag = !(await SaveDocumentAsync(current));
					}
					if (flag)
					{
						return;
					}
				}
			}
		}
		finally
		{
			((IDisposable)enumerator).Dispose();
		}
		SaveSession();
		_exitRequested = true;
		if (_source != null)
		{
			UnregisterHotKey(_source.Handle, HotkeyShowId);
			UnregisterHotKey(_source.Handle, HotkeyTopmostId);
			UnregisterHotKey(_source.Handle, HotkeyExitId);
		}
		if (_tray != null)
		{
			_tray.Visible = false;
			_tray.Dispose();
		}
		_trayMenu?.Close();
		System.Windows.Application.Current.Shutdown();
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (!_exitRequested)
		{
			e.Cancel = true;
			HideToTray();
		}
	}

	private void OnSourceInitialized(object? sender, EventArgs e)
	{
		_source = (HwndSource)PresentationSource.FromVisual(this);
		_source.AddHook(WndProc);
		RegisterHotKey(_source.Handle, HotkeyShowId, ModControl | ModAlt | ModShift | ModNoRepeat, 71u);
		RegisterHotKey(_source.Handle, HotkeyTopmostId, ModControl | ModAlt | ModShift | ModNoRepeat, 84u);
		RegisterHotKey(_source.Handle, HotkeyExitId, ModControl | ModAlt | ModShift | ModNoRepeat, 81u);
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
	{
		if (msg == WmHotkey)
		{
			switch (w.ToInt32())
			{
				case HotkeyShowId:
					ToggleTray();
					handled = true;
					break;
				case HotkeyTopmostId:
					base.Topmost = !base.Topmost;
					SaveSession();
					handled = true;
					break;
				case HotkeyExitId:
					_ = RequestExitAsync();
					handled = true;
					break;
			}
		}
		return IntPtr.Zero;
	}

	private void OnDrop(object sender, System.Windows.DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) || !(e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] source))
		{
			return;
		}
		foreach (string item in source.Where(File.Exists))
		{
			OpenPathAsync(item);
		}
	}

	private void ToggleMaximize()
	{
		base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	private void UpdateMaximizeGlyph()
	{
		if (_maximizeButton != null)
		{
			_maximizeButton.Content = Glyph((base.WindowState == WindowState.Maximized) ? "\ue923" : "\ue922", 13.0);
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
