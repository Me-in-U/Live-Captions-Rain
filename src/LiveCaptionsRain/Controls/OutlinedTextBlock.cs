using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MediaBrush = System.Windows.Media.Brush;

namespace LiveCaptionsRain.Controls;

internal sealed class OutlinedTextBlock : FrameworkElement
{
    public string Text { get; set; } = string.Empty;

    public string FontFamilyName { get; set; } = "Malgun Gothic";

    public double FontSizeValue { get; set; } = 22;

    public FontWeight FontWeightValue { get; set; } = FontWeights.SemiBold;

    public MediaBrush Fill { get; set; } = Brushes.White;

    public MediaBrush Stroke { get; set; } = Brushes.Black;

    public double StrokeThickness { get; set; } = 1.5;

    public bool UseFill { get; set; } = true;

    public bool Shadow { get; set; } = true;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(FontFamilyResolver.Create(FontFamilyName), FontStyles.Normal, FontWeightValue, FontStretches.Normal);
        var formatted = new FormattedText(
            Text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            FontSizeValue,
            Brushes.White,
            dpi);

        var geometry = formatted.BuildGeometry(new Point(StrokeThickness + 4, StrokeThickness + 4));
        if (Shadow)
        {
            drawingContext.PushOpacity(0.36);
            drawingContext.DrawGeometry(Brushes.Black, null, geometry.GetOutlinedPathGeometry());
            drawingContext.Pop();
        }

        drawingContext.DrawGeometry(UseFill ? Fill : Brushes.Transparent, new Pen(Stroke, StrokeThickness), geometry);
    }
}
