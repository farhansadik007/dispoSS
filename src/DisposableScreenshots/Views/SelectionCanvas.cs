using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DisposableScreenshots.Services;

namespace DisposableScreenshots.Views;

public sealed class SelectionCanvas : FrameworkElement
{
    private static readonly Brush DimBrush = Freeze(new SolidColorBrush(Color.FromArgb(122, 0, 0, 0)));
    private static readonly Brush AccentBrush = Freeze(new SolidColorBrush(Color.FromRgb(255, 122, 69)));
    private static readonly Brush BadgeBrush = Freeze(new SolidColorBrush(Color.FromArgb(232, 22, 22, 27)));
    private static readonly Brush InstructionBrush = Freeze(new SolidColorBrush(Color.FromArgb(236, 30, 30, 36)));
    private static readonly Pen AccentPen = Freeze(new Pen(AccentBrush, 1.5));
    private static readonly Pen AccentSoftPen = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(96, 255, 122, 69)), 1));
    private static readonly Pen CrosshairPen = Freeze(new Pen(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)), 1));
    private static readonly Typeface UiTypeface = new("Segoe UI");

    private Point _dragStart;
    private Point? _cursor;
    private bool _dragging;
    private bool _completed;

    public SelectionCanvas()
    {
        Focusable = true;
        Cursor = Cursors.Cross;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public double PixelScaleX { get; set; } = 1.0;

    public double PixelScaleY { get; set; } = 1.0;

    public Rect Selection { get; private set; }

    public bool HasSelection => !Selection.IsEmpty;

    public event EventHandler<Rect>? SelectionCompleted;

    public event EventHandler? CancelRequested;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        drawingContext.DrawRectangle(Brushes.Transparent, null, bounds);

        if (HasSelection)
        {
            var mask = new GeometryGroup { FillRule = FillRule.EvenOdd };
            mask.Children.Add(new RectangleGeometry(bounds));
            mask.Children.Add(new RectangleGeometry(Selection));
            drawingContext.DrawGeometry(DimBrush, null, mask);

            drawingContext.DrawRectangle(null, AccentSoftPen, Selection);
            drawingContext.DrawRectangle(null, AccentPen, Selection);

            DrawHandles(drawingContext, Selection);
            DrawSizeBadge(drawingContext, Selection);
        }
        else
        {
            drawingContext.DrawRectangle(DimBrush, null, bounds);
        }

        if (!HasSelection)
        {
            DrawInstructions(drawingContext, bounds);
        }

        if (_cursor is { } cursor && !_dragging)
        {
            drawingContext.DrawLine(CrosshairPen, new Point(0, cursor.Y), new Point(ActualWidth, cursor.Y));
            drawingContext.DrawLine(CrosshairPen, new Point(cursor.X, 0), new Point(cursor.X, ActualHeight));
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (_completed)
        {
            return;
        }

        AppLog.Info("Overlay mouse down.");
        Focus();
        CaptureMouse();
        _dragging = true;
        _dragStart = Clamp(e.GetPosition(this));
        _cursor = _dragStart;
        Selection = Rect.Empty;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        _cursor = Clamp(e.GetPosition(this));

        if (_dragging)
        {
            Selection = Normalize(_dragStart, _cursor.Value);
        }

        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();

        var current = Clamp(e.GetPosition(this));
        Selection = Normalize(_dragStart, current);

        if (Selection.Width < 3 || Selection.Height < 3)
        {
            Selection = Rect.Empty;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _completed = true;
        InvalidateVisual();
        SelectionCompleted?.Invoke(this, Selection);
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        CancelRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (!_dragging)
        {
            _cursor = null;
            InvalidateVisual();
        }
    }

    private void DrawInstructions(DrawingContext drawingContext, Rect bounds)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = MakeText("Drag to select  •  Esc to cancel", 14, Brushes.White, pixelsPerDip);
        var paddingX = 16.0;
        var paddingY = 9.0;
        var width = text.Width + paddingX * 2;
        var height = text.Height + paddingY * 2;
        var rect = new Rect(
            Math.Max(16, (bounds.Width - width) / 2),
            Math.Max(22, bounds.Height * 0.08),
            width,
            height);

        drawingContext.DrawRoundedRectangle(InstructionBrush, null, rect, 8, 8);
        drawingContext.DrawText(text, new Point(rect.Left + paddingX, rect.Top + paddingY));
    }

    private void DrawSizeBadge(DrawingContext drawingContext, Rect selection)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var width = Math.Max(1, (int)Math.Round(selection.Width * PixelScaleX));
        var height = Math.Max(1, (int)Math.Round(selection.Height * PixelScaleY));
        var text = MakeText($"{width} × {height}", 12, Brushes.White, pixelsPerDip);
        var paddingX = 8.0;
        var paddingY = 5.0;
        var badgeWidth = text.Width + paddingX * 2;
        var badgeHeight = text.Height + paddingY * 2;

        var x = selection.Right - badgeWidth - 4;
        var y = selection.Bottom + 8;

        if (y + badgeHeight > ActualHeight - 4)
        {
            y = selection.Bottom - badgeHeight - 8;
        }

        if (x < 4)
        {
            x = selection.Left + 4;
        }

        var rect = new Rect(x, y, badgeWidth, badgeHeight);
        drawingContext.DrawRoundedRectangle(BadgeBrush, null, rect, 5, 5);
        drawingContext.DrawText(text, new Point(rect.Left + paddingX, rect.Top + paddingY));
    }

    private static void DrawHandles(DrawingContext drawingContext, Rect selection)
    {
        const double size = 6;
        var half = size / 2;
        var points = new[]
        {
            new Point(selection.Left, selection.Top),
            new Point(selection.Right, selection.Top),
            new Point(selection.Left, selection.Bottom),
            new Point(selection.Right, selection.Bottom)
        };

        foreach (var point in points)
        {
            drawingContext.DrawEllipse(AccentBrush, null, point, half, half);
        }
    }

    private FormattedText MakeText(string value, double size, Brush brush, double pixelsPerDip) =>
        new(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            UiTypeface,
            size,
            brush,
            pixelsPerDip);

    private Point Clamp(Point point) =>
        new(
            Math.Clamp(point.X, 0, Math.Max(0, ActualWidth)),
            Math.Clamp(point.Y, 0, Math.Max(0, ActualHeight)));

    private static Rect Normalize(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        return new Rect(left, top, width, height);
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
