using System.Drawing;
using System.Drawing.Drawing2D;

namespace BetterScreenshots.Editor;

internal enum ToolKind { Arrow, Rectangle, Ellipse, Line, Pen, Highlighter, Text, Number, Crop, Blur, Pixelate, Redact }
internal abstract record Annotation(Color Color, float Thickness) { public abstract void Draw(Graphics graphics); }
internal sealed record ShapeAnnotation(ToolKind Tool, PointF Start, PointF End, Color Stroke, float StrokeThickness) : Annotation(Stroke, StrokeThickness)
{
    public override void Draw(Graphics graphics)
    {
        using var pen = new Pen(Color, Thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var rect = RectangleF.FromLTRB(Math.Min(Start.X, End.X), Math.Min(Start.Y, End.Y), Math.Max(Start.X, End.X), Math.Max(Start.Y, End.Y));
        switch (Tool)
        {
            case ToolKind.Rectangle: graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height); break;
            case ToolKind.Ellipse: graphics.DrawEllipse(pen, rect); break;
            case ToolKind.Arrow: DrawArrow(graphics, pen, Start, End); break;
            default: graphics.DrawLine(pen, Start, End); break;
        }
    }
    private static void DrawArrow(Graphics graphics, Pen pen, PointF start, PointF end)
    {
        graphics.DrawLine(pen, start, end);
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var length = Math.Max(10, pen.Width * 4);
        var a = new PointF((float)(end.X - length * Math.Cos(angle - Math.PI / 6)), (float)(end.Y - length * Math.Sin(angle - Math.PI / 6)));
        var b = new PointF((float)(end.X - length * Math.Cos(angle + Math.PI / 6)), (float)(end.Y - length * Math.Sin(angle + Math.PI / 6)));
        graphics.DrawLine(pen, end, a); graphics.DrawLine(pen, end, b);
    }
}
internal sealed record PenAnnotation(IReadOnlyList<PointF> Points, Color Stroke, float StrokeThickness, bool IsHighlighter) : Annotation(Stroke, StrokeThickness)
{
    public override void Draw(Graphics graphics)
    {
        if (Points.Count < 2) return;
        using var pen = new Pen(Color.FromArgb(IsHighlighter ? 90 : 255, Color), IsHighlighter ? Math.Max(Thickness * 3, 8) : Thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        graphics.DrawLines(pen, Points.ToArray());
    }
}
internal sealed record TextAnnotation(string Text, PointF Location, Color Stroke, float FontSize) : Annotation(Stroke, FontSize)
{
    public override void Draw(Graphics graphics)
    {
        using var font = new Font(FontFamily.GenericSansSerif, Math.Max(9, FontSize * 5), FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color);
        graphics.DrawString(Text, font, brush, Location);
    }
}
internal sealed record NumberAnnotation(int Number, PointF Location, Color Stroke, float Size) : Annotation(Stroke, Size)
{
    public override void Draw(Graphics graphics)
    {
        var diameter = Math.Max(20, Size * 7); var rect = new RectangleF(Location.X - diameter / 2, Location.Y - diameter / 2, diameter, diameter);
        using var brush = new SolidBrush(Color); using var textBrush = new SolidBrush(Color.GetBrightness() < .5 ? Color.White : Color.Black);
        using var font = new Font(FontFamily.GenericSansSerif, diameter * .55f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.FillEllipse(brush, rect); graphics.DrawString(Number.ToString(), font, textBrush, rect, format);
    }
}
