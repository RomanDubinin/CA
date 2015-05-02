//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE.
//
//  License: GNU General Public License version 3 (GPLv3)
//
//  Email: pavel_torgashov@mail.ru.
//
//  Copyright (C) Pavel Torgashov, 2011. 

using System;
using System.Collections.Generic;
using System.Drawing;

namespace ContourAnalysisNS
{
	[Serializable]
	public class Template
	{
		public string Name;
		public Contour Contour;
		public Contour ACF;
		public Point StartPoint;
		public bool PreferredAngleNoMore90 = false;

		public int AutoCorrDescriptor1;
		public int AutoCorrDescriptor2;
		public int AutoCorrDescriptor3;
		public int AutoCorrDescriptor4;
		public double ContourNorma;
		public double SourceArea;
		[NonSerialized] public object Tag;

		public Template(Point[] points, double sourceArea, int templateSize)
		{
			SourceArea = sourceArea;
			StartPoint = points[0];
			Contour = new Contour(points);
			Contour.Equalization(templateSize);
			ContourNorma = Contour.Norma;
			ACF = Contour.AutoCorrelationFunction(true);

			CalcAutoCorrDescriptions();
		}


		private static readonly int[] Filter1 = {1, 1, 1, 1};
		private static readonly int[] Filter2 = {-1, -1, 1, 1};
		private static readonly int[] Filter3 = {-1, 1, 1, -1};
		private static readonly int[] Filter4 = {-1, 1, -1, 1};

		/// <summary>
		/// Calc wavelets convolution for ACF
		/// </summary>
		public void CalcAutoCorrDescriptions()
		{
			var count = ACF.Count;
			double sum1 = 0;
			double sum2 = 0;
			double sum3 = 0;
			double sum4 = 0;
			for (var i = 0; i < count; i++)
			{
				var ACFNorma = ACF[i].Norma;
				var j = 4*i/count;

				sum1 += Filter1[j]*ACFNorma;
				sum2 += Filter2[j]*ACFNorma;
				sum3 += Filter3[j]*ACFNorma;
				sum4 += Filter4[j]*ACFNorma;
			}

			AutoCorrDescriptor1 = (int) (100*sum1/count);
			AutoCorrDescriptor2 = (int) (100*sum2/count);
			AutoCorrDescriptor3 = (int) (100*sum3/count);
			AutoCorrDescriptor4 = (int) (100*sum4/count);
		}

		public void Draw(Graphics gr, Rectangle rect)
		{
			gr.DrawRectangle(Pens.SteelBlue, rect);
			rect = new Rectangle(rect.Left, rect.Top, rect.Width - 24, rect.Height);

			var contour = Contour.Clone();
			var autoCorr = ACF.Clone();
			//contour.Normalize();
			autoCorr.Normalize();

			//draw contour
			var r = new Rectangle(rect.X, rect.Y, rect.Width/2, rect.Height);
			r.Inflate(-20, -20);
			var points = contour.GetPoints(StartPoint);
			var boundRect = Rectangle.Round(contour.GetBoundsRect());

			double w = boundRect.Width;
			double h = boundRect.Height;
			var k = (float) Math.Min(r.Width/w, r.Height/h);
			var dx = StartPoint.X - contour.SourceBoundingRect.Left;
			var dy = StartPoint.Y - contour.SourceBoundingRect.Top;
			var ddx = -(int) (boundRect.Left*k);
			var ddy = (int) (boundRect.Bottom*k);
			for (var i = 0; i < points.Length; i++)
				points[i] = new Point(r.Left + ddx + (int) ((points[i].X - contour.SourceBoundingRect.Left - dx)*k),
					r.Top + ddy + (int) ((points[i].Y - contour.SourceBoundingRect.Top - dy)*k));
			//
			gr.DrawPolygon(Pens.Red, points);
			//draw ACF
			r = new Rectangle(rect.Width/2 + rect.X, rect.Y, rect.Width/2, rect.Height);
			r.Inflate(-20, -20);

			var angles = new List<Point>();
			for (var i = 0; i < autoCorr.Count; i++)
			{
				var x = r.X + 5 + i*3;
				var v = (int) (autoCorr[i%autoCorr.Count].Norma*r.Height);
				gr.FillRectangle(Brushes.Blue, x, r.Bottom - v, 3, v);
				angles.Add(new Point(x, r.Bottom - (int) (r.Height*(0.5d + autoCorr[i%autoCorr.Count].Angle/2/Math.PI))));
			}

			try
			{
				gr.DrawLines(Pens.Red, angles.ToArray());
			}
			catch (OverflowException)
			{
			}

			var redPen = new Pen(Color.FromArgb(100, Color.Black));
			for (var i = 0; i <= 10; i++)
			{
				gr.DrawLine(redPen, r.X, r.Bottom - i*r.Height/10, r.X + r.Width, r.Bottom - i*r.Height/10);
			}

			//descriptors
			{
				var x = rect.Right;
				var y = r.Bottom - r.Height/2;
				gr.DrawLine(Pens.Gray, x, y, x + 23, y);
				if (AutoCorrDescriptor1 < int.MaxValue && AutoCorrDescriptor1 > int.MinValue)
				{
					gr.FillRectangle(Brushes.Red, x, y - (AutoCorrDescriptor1 < 0 ? 0 : AutoCorrDescriptor1*r.Height/100), 5,
						Math.Abs(AutoCorrDescriptor1)*r.Height/100);
					gr.FillRectangle(Brushes.Red, x + 6, y - (AutoCorrDescriptor2 < 0 ? 0 : AutoCorrDescriptor2*r.Height/100), 5,
						Math.Abs(AutoCorrDescriptor2)*r.Height/100);
					gr.FillRectangle(Brushes.Red, x + 12, y - (AutoCorrDescriptor3 < 0 ? 0 : AutoCorrDescriptor3*r.Height/100), 5,
						Math.Abs(AutoCorrDescriptor3)*r.Height/100);
					gr.FillRectangle(Brushes.Red, x + 18, y - (AutoCorrDescriptor4 < 0 ? 0 : AutoCorrDescriptor4*r.Height/100), 5,
						Math.Abs(AutoCorrDescriptor4)*r.Height/100);
				}
			}
		}
	}

	/// <summary>
	/// List of templates
	/// </summary>
	[Serializable]
	public class Templates : List<Template>
	{
		public int TemplateSize = 30;
	}
}
