using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ContourAnalysisNS
{
	public static class PointHelper
	{
		public static Point Center(this Rectangle rect)
		{
			return new Point(rect.X + rect.Width/2, rect.Y + rect.Height/2);
		}

		public static int Area(this Rectangle rect)
		{
			return rect.Width*rect.Height;
		}

		public static int Distance(this Point point, Point p)
		{
			return Math.Abs(point.X - p.X) + Math.Abs(point.Y - p.Y);
		}

		public static void NormalizePoints(Point[] points, Rectangle rectangle)
		{
			if (rectangle.Height == 0 || rectangle.Width == 0)
				return;

			var m = new Matrix();
			m.Translate(rectangle.Center().X, rectangle.Center().Y);

			if (rectangle.Width > rectangle.Height)
				m.Scale(1, 1f*rectangle.Width/rectangle.Height);
			else
				m.Scale(1f*rectangle.Height/rectangle.Width, 1);

			m.Translate(-rectangle.Center().X, -rectangle.Center().Y);
			m.TransformPoints(points);
		}

		public static void NormalizePoints2(Point[] points, Rectangle rectangle, Rectangle needRectangle)
		{
			if (rectangle.Height == 0 || rectangle.Width == 0)
				return;

			var k1 = 1f*needRectangle.Width/rectangle.Width;
			var k2 = 1f*needRectangle.Height/rectangle.Height;
			var k = Math.Min(k1, k2);

			var m = new Matrix();
			m.Scale(k, k);
			m.Translate(needRectangle.X/k - rectangle.X, needRectangle.Y/k - rectangle.Y);
			m.TransformPoints(points);
		}

		public static PointF Offset(this PointF p, float dx, float dy)
		{
			return new PointF(p.X + dx, p.Y + dy);
		}
	}
}