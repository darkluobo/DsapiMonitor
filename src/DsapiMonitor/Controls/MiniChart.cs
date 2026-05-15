using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DsapiMonitor.Controls;

public sealed class MiniChart : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(IList<double>), typeof(MiniChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(MiniChart),
            new FrameworkPropertyMetadata(1000.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(MiniChart),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillBrushProperty =
        DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(MiniChart),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x33, 0x42, 0xA5, 0xF5)),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public IList<double>? Values
    {
        get => (IList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public Brush StrokeBrush
    {
        get => (Brush)GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public Brush FillBrush
    {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var values = Values;
        if (values == null || values.Count < 2)
            return;

        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var max = Math.Max(MaxValue, values.Max());
        if (max <= 0) max = 1;

        var step = w / (values.Count - 1);
        var points = new Point[values.Count];

        for (var i = 0; i < values.Count; i++)
        {
            var x = i * step;
            var y = h - (values[i] / max * h * 0.85); // 留 15% 顶部间距
            points[i] = new Point(x, Math.Max(0, y));
        }

        // 填充区域
        var fillGeo = new StreamGeometry();
        using (var ctx = fillGeo.Open())
        {
            ctx.BeginFigure(points[0], true, true);
            ctx.PolyLineTo(points.Skip(1).ToArray(), true, true);
            ctx.LineTo(new Point(points[^1].X, h), true, false);
            ctx.LineTo(new Point(0, h), true, false);
        }
        fillGeo.Freeze();
        dc.DrawGeometry(FillBrush, null, fillGeo);

        // 折线
        var lineGeo = new StreamGeometry();
        using (var ctx = lineGeo.Open())
        {
            ctx.BeginFigure(points[0], false, false);
            ctx.PolyLineTo(points.Skip(1).ToArray(), true, true);
        }
        lineGeo.Freeze();
        dc.DrawGeometry(null, new Pen(StrokeBrush, 1.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, lineGeo);
    }
}
