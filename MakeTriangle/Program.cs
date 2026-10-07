using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

internal static class Program
{
    private readonly record struct Point(double X, double Y);

    public static int Main(string[] args)
    {
        if (args.Length != 3 ||
            !TryNumber(args[0], out double leg1) || leg1 <= 0 ||
            !TryNumber(args[1], out double leg2) || leg2 <= 0 ||
            !TryNumber(args[2], out double degrees))
        {
            Console.Error.WriteLine("Usage: TriangleSvg <leg1> <leg2> <rotationDegrees>");
            Console.Error.WriteLine("Legs must be positive finite numbers. Use a dot for decimals.");
            return 1;
        }

        // Scale by the longer leg to preserve proportions without huge coordinates.
        double longer = Math.Max(leg1, leg2);
        double width = 500;
        double height = 500 * (Math.Min(leg1, leg2) / longer);
        double radians = (degrees % 360) * Math.PI / 180;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        // Work in ordinary Cartesian coordinates (Y upwards), then invert Y for SVG.
        Point Rotate(double x, double y) =>
            new Point(x * cos - y * sin, -(x * sin + y * cos));

        Point a = Rotate(0, 0);
        Point b = Rotate(width, 0);
        Point c = Rotate(0, height);
        double markerSize = Math.Min(20, height / 4);
        Point[] marker = {
            Rotate(markerSize, 0),
            Rotate(markerSize, markerSize),
            Rotate(0, markerSize)
        };

        // Offsets rotate with the triangle; text itself remains upright.
        Point labelA = Rotate(-22, -22);
        Point labelB = Rotate(width + 22, -12);
        Point labelC = Rotate(-12, height + 22);
        Point labelAB = Rotate(width / 2, -30);
        Point labelAC = Rotate(-30, height / 2);
        string lengthAB = longer.ToString("G", CultureInfo.InvariantCulture);
        string lengthAC = Math.Min(leg1, leg2).ToString("G", CultureInfo.InvariantCulture);
        // Include conservative text extents in the bounds to avoid clipping long numbers.
        Point[] TextBounds(Point p, string text) => new[] {
            new Point(p.X - text.Length * 8, p.Y - 16),
            new Point(p.X + text.Length * 8, p.Y + 16)
        };
        Point[] bounds = new[] { a, b, c, labelA, labelB, labelC }
            .Concat(TextBounds(labelAB, lengthAB))
            .Concat(TextBounds(labelAC, lengthAC)).ToArray();
        const double padding = 30;
        double minX = bounds.Min(p => p.X) - padding;
        double minY = bounds.Min(p => p.Y) - padding;
        double svgWidth = bounds.Max(p => p.X) - minX + padding;
        double svgHeight = bounds.Max(p => p.Y) - minY + padding;

        string F(double n) => n.ToString("0.########", CultureInfo.InvariantCulture);
        string XY(Point p) => $"{F(p.X)},{F(p.Y)}";
        string Label(string name, Point p) =>
            $"  <text x=\"{F(p.X)}\" y=\"{F(p.Y + 7)}\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"22\" fill=\"black\">{name}</text>";

        string svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{F(svgWidth)}" height="{F(svgHeight)}" viewBox="{F(minX)} {F(minY)} {F(svgWidth)} {F(svgHeight)}">
              <title>Right-angled triangle ABC</title>
              <polygon points="{XY(a)} {XY(b)} {XY(c)}" fill="none" stroke="black" stroke-width="2" stroke-linejoin="round" />
              <polyline points="{string.Join(" ", marker.Select(XY))}" fill="none" stroke="black" stroke-width="1.5" />
            {Label("A", labelA)}
            {Label("B", labelB)}
            {Label("C", labelC)}
            {Label(lengthAB, labelAB)}
            {Label(lengthAC, labelAC)}
            </svg>
            """;

        string html = $"""
            <!doctype html>
            <html>
            <head><meta charset="utf-8"><title>Triangle</title></head>
            <body>
            {svg}
            </body>
            </html>
            """;

        try
        {
            File.WriteAllText("triangle.html", html, new UTF8Encoding(false));
            File.WriteAllText("triangle.svg", svg, new UTF8Encoding(false));
            Console.WriteLine($"Created {Path.GetFullPath("triangle.html")}");
            Console.WriteLine($"Created {Path.GetFullPath("triangle.svg")}");
            return 0;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not write output: {ex.Message}");
            return 2;
        }
    }

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value);
}
