using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NeoEdit.Common;
using Newtonsoft.Json;

namespace NeoEdit.UI.Controls
{
	public class EnhancedWindow : Window
	{
		const double ResizeBorder = 10;
		const double DragDetect = 10;

		[DepProp]
		public bool IsMainWindow { get { return UIHelper<EnhancedWindow>.GetPropValue<bool>(this); } set { UIHelper<EnhancedWindow>.SetPropValue(this, value); } }

		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(32, 32, 32));
		static readonly Brush OuterBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
		static readonly Brush ActiveBrush = Brushes.White;
		static readonly Brush InactiveBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));

		Borders saveBorder;
		Point savePoint;
		Rect saveWindowPosition;

		static EnhancedWindow()
		{
			UIHelper<EnhancedWindow>.Register();
			BackgroundBrush.Freeze();
			OuterBrush.Freeze();
			ActiveBrush.Freeze();
			InactiveBrush.Freeze();
		}

		public EnhancedWindow()
		{
			WindowStyle = WindowStyle.None;
			Visibility = Visibility.Visible;
			ResizeMode = ResizeMode.CanResizeWithGrip;
			AllowsTransparency = true;
			Template = GetTemplate();
		}

		public new bool ShowDialog()
		{
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			var result = base.ShowDialog() == true;
			Owner?.Focus();
			return result;
		}

		ControlTemplate GetTemplate()
		{
			var template = new ControlTemplate(typeof(EnhancedWindow));

			var outerGrid = new FrameworkElementFactory(typeof(Grid));

			var rect = new FrameworkElementFactory(typeof(Rectangle)) { Name = "PART_rect" };
			rect.SetValue(Rectangle.FillProperty, Brushes.Black);
			rect.SetValue(Rectangle.VisibilityProperty, Visibility.Hidden);
			outerGrid.AppendChild(rect);

			var outerBorder = new FrameworkElementFactory(typeof(Border)) { Name = "blackBorder" };
			outerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
			outerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(2));
			outerBorder.SetValue(Border.BackgroundProperty, OuterBrush);
			outerBorder.SetValue(Border.BorderBrushProperty, OuterBrush);
			outerBorder.AddHandler(Border.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnBorderMouseLeftButtonDown));
			outerBorder.AddHandler(Border.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnBorderMouseLeftButtonUp));
			outerBorder.AddHandler(Border.MouseMoveEvent, new MouseEventHandler(OnBorderMouseMove));

			var innerBorder = new FrameworkElementFactory(typeof(Border)) { Name = "innerBorder" };
			innerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
			innerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(2));
			innerBorder.SetValue(Border.BackgroundProperty, BackgroundBrush);
			innerBorder.SetValue(Border.BorderBrushProperty, ActiveBrush);

			var grid = new FrameworkElementFactory(typeof(Grid));

			var gridItem = new FrameworkElementFactory(typeof(ColumnDefinition));
			gridItem.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
			grid.AppendChild(gridItem);
			gridItem = new FrameworkElementFactory(typeof(ColumnDefinition));
			gridItem.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
			grid.AppendChild(gridItem);
			gridItem = new FrameworkElementFactory(typeof(ColumnDefinition));
			gridItem.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
			grid.AppendChild(gridItem);

			gridItem = new FrameworkElementFactory(typeof(RowDefinition));
			gridItem.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
			grid.AppendChild(gridItem);
			gridItem = new FrameworkElementFactory(typeof(RowDefinition));
			gridItem.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
			grid.AppendChild(gridItem);

			var decoder = BitmapDecoder.Create(new Uri($"pack://application:,,,/NeoEdit;component/NeoEdit.ico"), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
			var iconImage = decoder.Frames.Where(f => f.Width == 16).OrderByDescending(f => f.Width).First();
			var icon = new FrameworkElementFactory(typeof(Image)) { Name = "enhancedWindowIcon" };
			icon.SetValue(Image.StretchProperty, Stretch.None);
			icon.SetValue(Image.SourceProperty, iconImage);
			icon.SetValue(Image.MarginProperty, new Thickness(5, 0, 0, 0));
			icon.SetValue(Grid.RowProperty, 0);
			icon.SetValue(Grid.ColumnProperty, 0);
			grid.AppendChild(icon);

			var textBlock = new FrameworkElementFactory(typeof(TextBlock)) { Name = "enhancedWindowTitle" };
			textBlock.SetValue(TextBlock.FontSizeProperty, 14d);
			textBlock.SetValue(TextBlock.ForegroundProperty, ActiveBrush);
			textBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
			textBlock.SetValue(Image.MarginProperty, new Thickness(10, 0, 10, 0));
			textBlock.AddHandler(TextBlock.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnTitleMouseLeftButtonDown));
			textBlock.AddHandler(TextBlock.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnTitleMouseLeftButtonUp));
			textBlock.AddHandler(TextBlock.MouseMoveEvent, new MouseEventHandler(OnTitleMouseMove));
			textBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Title)) { Source = this });
			textBlock.SetValue(Grid.RowProperty, 0);
			textBlock.SetValue(Grid.ColumnProperty, 1);
			grid.AppendChild(textBlock);

			var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
			stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
			stackPanel.SetValue(Grid.RowProperty, 0);
			stackPanel.SetValue(Grid.ColumnProperty, 2);

			var minimizeButton = new FrameworkElementFactory(typeof(Button)) { Name = "enhancedWindowMinimize" };
			minimizeButton.SetValue(Button.ContentProperty, "🗕");
			minimizeButton.SetValue(Button.FontSizeProperty, 14d);
			minimizeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			minimizeButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			minimizeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			minimizeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			minimizeButton.SetValue(Button.FocusableProperty, false);
			minimizeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) => WindowState = WindowState.Minimized));
			stackPanel.AppendChild(minimizeButton);

			var shrinkButton = new FrameworkElementFactory(typeof(Button)) { Name = "shrinkButton" };
			shrinkButton.SetValue(Button.ContentProperty, "🗗");
			shrinkButton.SetValue(Button.FontSizeProperty, 14d);
			shrinkButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			shrinkButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			shrinkButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			shrinkButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			shrinkButton.SetValue(Button.FocusableProperty, false);
			shrinkButton.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) => WindowState = WindowState.Normal));
			stackPanel.AppendChild(shrinkButton);

			var growButton = new FrameworkElementFactory(typeof(Button)) { Name = "growButton" };
			growButton.SetValue(Button.ContentProperty, "🗖");
			growButton.SetValue(Button.FontSizeProperty, 14d);
			growButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			growButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			growButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			growButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			growButton.SetValue(Button.FocusableProperty, false);
			growButton.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) => WindowState = WindowState.Maximized));
			stackPanel.AppendChild(growButton);

			var closeButton = new FrameworkElementFactory(typeof(Button)) { Name = "enhancedWindowClose" };
			closeButton.SetValue(Button.ContentProperty, "🗙");
			closeButton.SetValue(Button.FontSizeProperty, 14d);
			closeButton.SetValue(Button.MarginProperty, new Thickness(5, -2, 5, 0));
			closeButton.SetValue(Button.ForegroundProperty, ActiveBrush);
			closeButton.SetValue(Button.BackgroundProperty, Brushes.Transparent);
			closeButton.SetValue(Button.BorderBrushProperty, Brushes.Transparent);
			closeButton.SetValue(Button.FocusableProperty, false);
			closeButton.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) => Close()));

			stackPanel.AppendChild(closeButton);
			grid.AppendChild(stackPanel);

			var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
			contentPresenter.SetValue(ContentPresenter.FocusVisualStyleProperty, null);
			contentPresenter.SetValue(Grid.RowProperty, 1);
			contentPresenter.SetValue(Grid.ColumnProperty, 0);
			contentPresenter.SetValue(Grid.ColumnSpanProperty, 3);
			grid.AppendChild(contentPresenter);

			innerBorder.AppendChild(grid);
			outerBorder.AppendChild(innerBorder);
			outerGrid.AppendChild(outerBorder);

			template.VisualTree = outerGrid;

			var windowStateTrigger = new Trigger { Property = WindowStateProperty, Value = WindowState.Maximized };
			windowStateTrigger.Setters.Add(new Setter { TargetName = outerBorder.Name, Property = Border.CornerRadiusProperty, Value = new CornerRadius(0) });
			windowStateTrigger.Setters.Add(new Setter { TargetName = outerBorder.Name, Property = Border.BorderThicknessProperty, Value = new Thickness(7) });
			windowStateTrigger.Setters.Add(new Setter { TargetName = outerBorder.Name, Property = Border.BackgroundProperty, Value = Brushes.Black });
			windowStateTrigger.Setters.Add(new Setter { TargetName = rect.Name, Property = VisibilityProperty, Value = Visibility.Visible });
			template.Triggers.Add(windowStateTrigger);

			var isMainWindowTrigger = new Trigger { Property = UIHelper<EnhancedWindow>.GetProperty(x => IsMainWindow), Value = false };
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = icon.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = minimizeButton.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = shrinkButton.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			isMainWindowTrigger.Setters.Add(new Setter { TargetName = growButton.Name, Property = Image.VisibilityProperty, Value = Visibility.Collapsed });
			template.Triggers.Add(isMainWindowTrigger);

			var isActiveTrigger = new Trigger { Property = IsActiveProperty, Value = false };
			isActiveTrigger.Setters.Add(new Setter { TargetName = innerBorder.Name, Property = Border.BorderBrushProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = textBlock.Name, Property = TextBlock.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = minimizeButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = shrinkButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = growButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			isActiveTrigger.Setters.Add(new Setter { TargetName = closeButton.Name, Property = Button.ForegroundProperty, Value = InactiveBrush });
			template.Triggers.Add(isActiveTrigger);

			return template;
		}

		[Flags]
		enum Borders
		{
			None = 0,
			Left = 1,
			Right = 2,
			Top = 4,
			Bottom = 8,
			TopLeft = Top | Left,
			TopRight = Top | Right,
			BottomLeft = Bottom | Left,
			BottomRight = Bottom | Right,
		}

		Borders GetMouseBorder(Point pos)
		{
			if (WindowState == WindowState.Maximized)
				return Borders.None;

			var moveBorder = Borders.None;
			if (pos.X <= ResizeBorder)
				moveBorder |= Borders.Left;
			else if (pos.X >= ActualWidth - ResizeBorder)
				moveBorder |= Borders.Right;
			if (pos.Y <= ResizeBorder)
				moveBorder |= Borders.Top;
			else if (pos.Y >= ActualHeight - ResizeBorder)
				moveBorder |= Borders.Bottom;
			return moveBorder;
		}

		void OnBorderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var border = sender as Border;
			var pos = e.GetPosition(border);
			saveBorder = GetMouseBorder(pos);
			if (saveBorder == Borders.None)
				return;

			savePoint = border.PointToScreen(pos);
			saveWindowPosition = new Rect(Left, Top, ActualWidth, ActualHeight);
			border.CaptureMouse();
			e.Handled = true;
		}

		void OnBorderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var border = sender as Border;
			if (border.IsMouseCaptured)
			{
				border.ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		void OnBorderMouseMove(object sender, MouseEventArgs e)
		{
			var border = sender as Border;
			var pos = e.GetPosition(border);

			if (border.IsMouseCaptured)
			{
				e.Handled = true;
				var diff = border.PointToScreen(pos) - savePoint;
				if (saveBorder.HasFlag(Borders.Left))
				{
					Left = saveWindowPosition.Left + diff.X;
					Width = saveWindowPosition.Width - diff.X;
				}
				if (saveBorder.HasFlag(Borders.Right))
					Width = saveWindowPosition.Width + diff.X;
				if (saveBorder.HasFlag(Borders.Top))
				{
					Top = saveWindowPosition.Top + diff.Y;
					Height = saveWindowPosition.Height - diff.Y;
				}
				if (saveBorder.HasFlag(Borders.Bottom))
					Height = saveWindowPosition.Height + diff.Y;
			}
			else
			{
				switch (GetMouseBorder(pos))
				{
					case Borders.Left: case Borders.Right: border.Cursor = Cursors.SizeWE; break;
					case Borders.Top: case Borders.Bottom: border.Cursor = Cursors.SizeNS; break;
					case Borders.TopLeft: case Borders.BottomRight: border.Cursor = Cursors.SizeNWSE; break;
					case Borders.TopRight: case Borders.BottomLeft: border.Cursor = Cursors.SizeNESW; break;
					default: border.Cursor = null; break;
				}
			}
		}

		void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				if (WindowState == WindowState.Maximized)
					WindowState = WindowState.Normal;
				else
					WindowState = WindowState.Maximized;
			}
			else if (WindowState == WindowState.Maximized)
			{
				savePoint = e.GetPosition(this);
				(sender as TextBlock).CaptureMouse();
			}
			else
				DragMove();
			e.Handled = true;
		}

		void OnTitleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var title = sender as TextBlock;
			if (title.IsMouseCaptured)
			{
				title.ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		void OnTitleMouseMove(object sender, MouseEventArgs e)
		{
			var title = sender as TextBlock;
			if (!title.IsMouseCaptured)
				return;

			var newPoint = e.GetPosition(this);
			if ((newPoint - savePoint).Length >= DragDetect)
			{
				title.ReleaseMouseCapture();

				var startPos = PointToScreen(newPoint);
				if (WindowState == WindowState.Maximized)
					WindowState = WindowState.Normal;
				var dist = PointToScreen(savePoint) - startPos;
				Left = startPos.X - ActualWidth / 2;
				Top -= dist.Y;
				DragMove();
			}
			e.Handled = true;
		}

		class ScreenPosition
		{
			public Rect Position { get; set; }
			public WindowState State { get; set; }

			public override string ToString() => JsonConvert.SerializeObject(this);
			public static ScreenPosition FromString(string str) => JsonConvert.DeserializeObject<ScreenPosition>(str);
		}

		public string GetPosition() => new ScreenPosition { Position = new Rect(Left, Top, Width, Height), State = WindowState }.ToString();

		public void SetPosition(string position)
		{
			var pos = ScreenPosition.FromString(position);
			Left = pos.Position.Left;
			Top = pos.Position.Top;
			Width = pos.Position.Width;
			Height = pos.Position.Height;
			WindowState = pos.State;
			if (WindowState == WindowState.Minimized)
				WindowState = WindowState.Maximized;
		}
	}
}
