using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BetterScreenshots.Capture;

namespace BetterScreenshots.Editor;

public partial class AnnotationWindow : Window
{
    private Bitmap _bitmap;
    private readonly Action<Bitmap> _save;
    private readonly List<Annotation> _annotations = [];
    private readonly Stack<EditorState> _undo = new();
    private readonly Stack<EditorState> _redo = new();
    private ToolKind _tool = ToolKind.Arrow;
    private System.Windows.Point _start;
    private System.Windows.Point _current;
    private List<System.Windows.Point>? _penPoints;
    private Annotation? _preview;
    private int _nextNumber = 1;

    public AnnotationWindow(Bitmap bitmap, Action<Bitmap> save)
    {
        InitializeComponent();
        _bitmap = (Bitmap)bitmap.Clone();
        _save = save;
        Closed += (_, _) => DisposeState();
        RefreshCanvas();
    }

    private System.Drawing.Color ActiveColor => System.Drawing.Color.FromName(((ComboBoxItem)ColorBox.SelectedItem).Content.ToString()!);
    private float ActiveThickness => (float)Thickness.Value;

    private void ToolClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string name } && Enum.TryParse<ToolKind>(name, out var tool)) _tool = tool;
    }
    private void ColorChanged(object sender, SelectionChangedEventArgs e) { }

    private void CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = _current = e.GetPosition(DrawingCanvas);
        if (_tool == ToolKind.Text) { CreateTextBox(_start); return; }
        if (_tool == ToolKind.Number)
        {
            PushUndo(); _annotations.Add(new NumberAnnotation(_nextNumber++, ToDrawing(_start), ActiveColor, ActiveThickness)); RefreshCanvas(); return;
        }
        _penPoints = _tool is ToolKind.Pen or ToolKind.Highlighter ? new List<System.Windows.Point> { _start } : null;
        DrawingCanvas.CaptureMouse();
    }

    private void CanvasMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!DrawingCanvas.IsMouseCaptured) return;
        _current = Clamp(e.GetPosition(DrawingCanvas));
        if (_penPoints is not null)
        {
            _penPoints.Add(_current);
            _preview = new PenAnnotation(_penPoints.Select(ToDrawing).ToArray(), ActiveColor, ActiveThickness, _tool == ToolKind.Highlighter);
        }
        else _preview = new ShapeAnnotation(_tool, ToDrawing(_start), ToDrawing(_current), ActiveColor, ActiveThickness);
        RenderAnnotations(_preview);
    }

    private void CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!DrawingCanvas.IsMouseCaptured) return;
        DrawingCanvas.ReleaseMouseCapture();
        var end = Clamp(e.GetPosition(DrawingCanvas));
        var rect = ToRectangle(_start, end);
        if (_tool is ToolKind.Blur or ToolKind.Pixelate or ToolKind.Redact or ToolKind.Crop)
        {
            if (rect.Width > 1 && rect.Height > 1)
            {
                PushUndo();
                if (_tool == ToolKind.Blur) ImageOperations.Blur(_bitmap, rect);
                else if (_tool == ToolKind.Pixelate) ImageOperations.Pixelate(_bitmap, rect);
                else if (_tool == ToolKind.Redact) ImageOperations.Redact(_bitmap, rect, ActiveColor);
                else Crop(rect);
            }
        }
        else if (_penPoints is { Count: > 1 })
        {
            PushUndo(); _annotations.Add(new PenAnnotation(_penPoints.Select(ToDrawing).ToArray(), ActiveColor, ActiveThickness, _tool == ToolKind.Highlighter));
        }
        else if (Math.Abs(end.X - _start.X) > 1 || Math.Abs(end.Y - _start.Y) > 1)
        {
            PushUndo(); _annotations.Add(new ShapeAnnotation(_tool, ToDrawing(_start), ToDrawing(end), ActiveColor, ActiveThickness));
        }
        _preview = null; _penPoints = null; RefreshCanvas();
    }

    private void CreateTextBox(System.Windows.Point point)
    {
        var input = new System.Windows.Controls.TextBox { Width = 220, FontSize = Math.Max(13, ActiveThickness * 5), Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(ActiveColor.A, ActiveColor.R, ActiveColor.G, ActiveColor.B)), Background = System.Windows.Media.Brushes.White, BorderBrush = System.Windows.Media.Brushes.DodgerBlue };
        Canvas.SetLeft(input, point.X); Canvas.SetTop(input, point.Y); DrawingCanvas.Children.Add(input); input.Focus();
        var committed = false;
        void Commit()
        {
            if (committed) return; committed = true; DrawingCanvas.Children.Remove(input);
            if (!string.IsNullOrWhiteSpace(input.Text)) { PushUndo(); _annotations.Add(new TextAnnotation(input.Text.Trim(), ToDrawing(point), ActiveColor, ActiveThickness)); RefreshCanvas(); }
        }
        input.KeyDown += (_, args) => { if (args.Key == Key.Enter) { Commit(); args.Handled = true; } if (args.Key == Key.Escape) { committed = true; DrawingCanvas.Children.Remove(input); } };
        input.LostKeyboardFocus += (_, _) => Commit();
    }

    private void Crop(System.Drawing.Rectangle rectangle)
    {
        using var flattened = Flatten();
        var cropped = flattened.Clone(rectangle, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        _bitmap.Dispose(); _bitmap = cropped; _annotations.Clear(); _nextNumber = 1;
    }

    private void UndoClick(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) return;
        _redo.Push(TakeState()); Restore(_undo.Pop()); RefreshCanvas();
    }
    private void RedoClick(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0) return;
        _undo.Push(TakeState()); Restore(_redo.Pop()); RefreshCanvas();
    }
    private void SaveClick(object sender, RoutedEventArgs e)
    {
        using var flattened = Flatten(); _save(flattened); Close();
    }
    private void CancelClick(object sender, RoutedEventArgs e) => Close();

    private void PushUndo()
    {
        _undo.Push(TakeState());
        if (_undo.Count > 25)
        {
            var states = _undo.ToArray();
            _undo.Clear();
            foreach (var state in states.Skip(25)) state.Bitmap.Dispose();
            for (var i = Math.Min(24, states.Length - 1); i >= 0; i--) _undo.Push(states[i]);
        }
        DisposeStack(_redo);
    }
    private EditorState TakeState() => new((Bitmap)_bitmap.Clone(), _annotations.ToList(), _nextNumber);
    private void Restore(EditorState state)
    {
        _bitmap.Dispose(); _bitmap = state.Bitmap; _annotations.Clear(); _annotations.AddRange(state.Annotations); _nextNumber = state.NextNumber;
    }
    private Bitmap Flatten()
    {
        var result = (Bitmap)_bitmap.Clone();
        using var g = Graphics.FromImage(result); g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        foreach (var annotation in _annotations) annotation.Draw(g);
        return result;
    }
    private void RefreshCanvas()
    {
        BaseImage.Source = BitmapInterop.ToBitmapSource(_bitmap);
        BaseImage.Width = DrawingCanvas.Width = _bitmap.Width; BaseImage.Height = DrawingCanvas.Height = _bitmap.Height;
        RenderAnnotations();
    }
    private void RenderAnnotations(Annotation? preview = null)
    {
        DrawingCanvas.Children.Clear();
        foreach (var annotation in _annotations) AddVisual(annotation);
        if (preview is not null) AddVisual(preview);
    }
    private void AddVisual(Annotation annotation)
    {
        var color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(annotation.Color.A, annotation.Color.R, annotation.Color.G, annotation.Color.B));
        if (annotation is ShapeAnnotation shape)
        {
            var x = Math.Min(shape.Start.X, shape.End.X); var y = Math.Min(shape.Start.Y, shape.End.Y); var w = Math.Abs(shape.Start.X - shape.End.X); var h = Math.Abs(shape.Start.Y - shape.End.Y);
            Shape visual = shape.Tool switch { ToolKind.Rectangle => new System.Windows.Shapes.Rectangle(), ToolKind.Ellipse => new System.Windows.Shapes.Ellipse(), _ => new System.Windows.Shapes.Line { X1 = shape.Start.X, Y1 = shape.Start.Y, X2 = shape.End.X, Y2 = shape.End.Y } };
            visual.Stroke = color; visual.StrokeThickness = shape.Thickness; if (visual is not Line) { Canvas.SetLeft(visual, x); Canvas.SetTop(visual, y); visual.Width = w; visual.Height = h; } DrawingCanvas.Children.Add(visual);
        }
        else if (annotation is PenAnnotation pen && pen.Points.Count > 1)
        {
            var visual = new Polyline { Points = new PointCollection(pen.Points.Select(p => new System.Windows.Point(p.X, p.Y))), Stroke = color, StrokeThickness = pen.IsHighlighter ? Math.Max(pen.Thickness * 3, 8) : pen.Thickness, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Opacity = pen.IsHighlighter ? .45 : 1 };
            DrawingCanvas.Children.Add(visual);
        }
        else if (annotation is TextAnnotation text)
        {
            var visual = new TextBlock { Text = text.Text, Foreground = color, FontSize = Math.Max(13, text.FontSize * 5) }; Canvas.SetLeft(visual, text.Location.X); Canvas.SetTop(visual, text.Location.Y); DrawingCanvas.Children.Add(visual);
        }
        else if (annotation is NumberAnnotation number)
        {
            var size = Math.Max(20, number.Size * 7); var grid = new Grid { Width = size, Height = size }; grid.Children.Add(new System.Windows.Shapes.Ellipse { Fill = color }); grid.Children.Add(new TextBlock { Text = number.Number.ToString(), Foreground = number.Color.GetBrightness() < .5 ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center }); Canvas.SetLeft(grid, number.Location.X - size / 2); Canvas.SetTop(grid, number.Location.Y - size / 2); DrawingCanvas.Children.Add(grid);
        }
    }
    private static System.Drawing.PointF ToDrawing(System.Windows.Point point) => new((float)point.X, (float)point.Y);
    private System.Drawing.Rectangle ToRectangle(System.Windows.Point start, System.Windows.Point end) => System.Drawing.Rectangle.FromLTRB((int)Math.Floor(Math.Min(start.X, end.X)), (int)Math.Floor(Math.Min(start.Y, end.Y)), (int)Math.Ceiling(Math.Max(start.X, end.X)), (int)Math.Ceiling(Math.Max(start.Y, end.Y)));
    private System.Windows.Point Clamp(System.Windows.Point point) => new(Math.Clamp(point.X, 0, _bitmap.Width), Math.Clamp(point.Y, 0, _bitmap.Height));
    private static void DisposeStack(Stack<EditorState> stack) { while (stack.TryPop(out var item)) item.Bitmap.Dispose(); }
    private void DisposeState() { _bitmap.Dispose(); DisposeStack(_undo); DisposeStack(_redo); }
    private sealed record EditorState(Bitmap Bitmap, List<Annotation> Annotations, int NextNumber);
}
