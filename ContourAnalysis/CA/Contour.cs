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
using System.Linq;

namespace ContourAnalysisNS
{
	[Serializable]
	public class Contour
	{
		private Complex[] Array;
		public Rectangle SourceBoundingRect;

		public Contour(int capacity)
		{
			Array = new Complex[capacity];
		}

		protected Contour()
		{
		}

		public int Count
		{
			get { return Array.Length; }
		}

		public Complex this[int i]
		{
			get { return Array[i]; }
			set { Array[i] = value; }
		}

		public Contour(IList<Point> points)
			: this(points.Count)
		{
			var minX = points[0].X;
			var minY = points[0].Y;
			var maxX = minX;
			var maxY = minY;
			var endIndex = points.Count;

			for (var i = 0; i < endIndex; i++)
			{
				var p1 = points[i];
				var p2 = i == endIndex - 1 ? points[0] : points[i + 1];
				Array[i] = new Complex(p2.X - p1.X, -p2.Y + p1.Y);

				if (p1.X > maxX) maxX = p1.X;
				if (p1.X < minX) minX = p1.X;
				if (p1.Y > maxY) maxY = p1.Y;
				if (p1.Y < minY) minY = p1.Y;
			}

			SourceBoundingRect = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
		}

		public Contour Clone()
		{
			var result = new Contour {Array = (Complex[]) Array.Clone()};
			return result;
		}

		/// <summary>
		/// Returns R^2 of difference of norms
		/// </summary>
		/// <param name="otherContour"></param>
		/// <returns></returns>
		public double DiffR2(Contour otherContour)
		{
			double max1 = 0;
			double max2 = 0;
			double sum = 0;
			for (var i = 0; i < Count; i++)
			{
				var v1 = Array[i].Norma;
				var v2 = otherContour.Array[i].Norma;
				if (v1 > max1) max1 = v1;
				if (v2 > max2) max2 = v2;
				var v = v1 - v2;
				sum += v*v;
			}
			var max = Math.Max(max1, max2);
			return 1 - sum/Count/max/max;
		}

		public double Norma
		{
			get
			{
				var result = Array.Sum(c => c.NormaSquare);
				return Math.Sqrt(result);
			}
		}

		public Complex ScalarProduct(Contour c)
		{
			return ScalarProduct(c, 0);
		}

		public Complex ScalarProduct(Contour otherContour, int shift)
		{
			var count = Count;
			double sumA = 0;
			double sumB = 0;

			int k = shift;
			for (var i = 0; i < count; i++)
			{
				var x1 = Array[i];
				var x2 = otherContour.Array[k];
				//(re; im) * (re; -im) - не обычное произведение комплексных!
				sumA += x1.Re*x2.Re + x1.Im*x2.Im;
				sumB += x1.Im*x2.Re - x1.Re*x2.Im;

				if (k == otherContour.Count - 1)
					k = 0;
				else
					k++;
			}

			return new Complex(sumA, sumB);
		}

		public unsafe Complex ScalarProductUnsafe(Contour otherContour, int shift)
		{
			var count = Count;
			double sumA = 0;
			double sumB = 0;
			fixed (Complex* ptr1 = &Array[0])
			fixed (Complex* ptr2 = &otherContour.Array[shift])
			fixed (Complex* firstVectorInOtherContourPtr = &otherContour.Array[0])
			fixed (Complex* lastVectorInOtherContourPtr = &otherContour.Array[otherContour.Count - 1])
			{
				var p1 = ptr1;
				var p2 = ptr2;
				for (var i = 0; i < count; i++)
				{
					var x1 = *p1;
					var x2 = *p2;
					sumA += x1.Re * x2.Re + x1.Im * x2.Im;
					sumB += x1.Im * x2.Re - x1.Re * x2.Im;

					p1++;
					if (p2 == lastVectorInOtherContourPtr)
						p2 = firstVectorInOtherContourPtr;
					else
						p2++;
				}
			}
			return new Complex(sumA, sumB);
		}

		public Contour InterCorrelationFunction(Contour otherContour)
		{
			var count = Count;
			var result = new Contour(count);
			for (var i = 0; i < count; i++)
				result.Array[i] = ScalarProduct(otherContour, i);

			return result;
		}

		public Contour InterCorrelationFunction(Contour otherContour, int maxShift)
		{
			var result = new Contour(maxShift);
			var i = 0;
			while (i < maxShift/2)
			{
				result.Array[i] = ScalarProduct(otherContour, i);
				result.Array[maxShift - i - 1] = ScalarProduct(otherContour, otherContour.Count - i - 1);
				i++;
			}
			return result;
		}

		public Contour AutoCorrelationFunction(bool normalize)
		{
			var count = Count/2;
			var result = new Contour(count);

			double maxNormaSq = 0;
			for (var i = 0; i < count; i++)
			{
				result.Array[i] = ScalarProduct(this, i);
				var normaSq = (result.Array[i]).NormaSquare;
				if (normaSq > maxNormaSq)
					maxNormaSq = normaSq;

			}
			if (normalize)
			{
				maxNormaSq = Math.Sqrt(maxNormaSq);
				for (var i = 0; i < count; i++)
					result.Array[i] = new Complex((result.Array[i]).Re/maxNormaSq, (result.Array[i]).Im/maxNormaSq);
			}

			return result;
		}

		public unsafe Contour AutoCorrelationFunctionUnsafe(bool normalize)
		{
			var count = Count/2;
			var result = new Contour(count);
			fixed (Complex* ptr = &result.Array[0])
			{
				var p = ptr;
				double maxNormaSq = 0;
				for (var i = 0; i < count; i++)
				{
					*p = ScalarProduct(this, i);
					var normaSq = (*p).NormaSquare;
					if (normaSq > maxNormaSq)
						maxNormaSq = normaSq;
					p++;
				}
				if (normalize)
				{
					maxNormaSq = Math.Sqrt(maxNormaSq);
					p = ptr;
					for (var i = 0; i < count; i++)
					{
						*p = new Complex((*p).Re/maxNormaSq, (*p).Im/maxNormaSq);
						p++;
					}
				}
			}

			return result;
		}


		public void Normalize()
		{
			var max = FindMaxNormaItem().Norma;
			if (max > double.Epsilon)
				Scale(1/max);
		}

		public Complex FindMaxNormaItem()
		{
			var max = 0.0;
			var res = new Complex(0, 0);
			foreach (var c in Array)
				if (c.Norma > max)
				{
					max = c.Norma;
					res = c;
				}
			return res;
		}


		public void Scale(double scale)
		{
			for (var i = 0; i < Count; i++)
				this[i] = scale*this[i];
		}

		public void Mult(Complex c)
		{
			for (var i = 0; i < Count; i++)
				this[i] = c*this[i];
		}

		public void Rotate(double angle)
		{
			var cosA = Math.Cos(angle);
			var sinA = Math.Sin(angle);
			for (var i = 0; i < Count; i++)
				this[i] = this[i].Rotate(cosA, sinA);
		}

		/// <summary>
		/// Normalized Scalar Product
		/// </summary>
		public Complex NotmalizedScalarProduct(Contour otherContour)
		{
			var res = ScalarProduct(otherContour)*(1/(Norma * otherContour.Norma));
			return res;
		}

		/// <summary>
		/// Discrete Fourier Transform
		/// </summary>
		public Contour Fourier()
		{
			var count = Count;
			var result = new Contour(count);
			for (var m = 0; m < count; m++)
			{
				var sum = new Complex(0, 0);
				var k = -2d*Math.PI*m/count;
				for (var n = 0; n < count; n++)
					sum += this[n].Rotate(k*n);

				result.Array[m] = sum;
			}

			return result;
		}


		public double Distance(Contour c)
		{
			var n1 = Norma;
			var n2 = c.Norma;
			return n1*n1 + n2*n2 - 2*(ScalarProduct(c).Re);
		}

		/// <summary>
		/// Changes length of contour (equalization)
		/// </summary>
		public void Equalization(int newCount)
		{
			if (newCount > Count)
				EqualizationUp(newCount);
			else
				EqualizationDown(newCount);
		}

		private void EqualizationUp(int newCount)
		{
			var newPoint = new Complex[newCount];

			for (var i = 0; i < newCount; i++)
			{
				var index = 1d*i*Count/newCount;
				var j = (int) index;
				var k = index - j;
				if (j == Count - 1)
					newPoint[i] = this[j];
				else
					newPoint[i] = this[j]*(1 - k) + this[j + 1]*k;
			}

			Array = newPoint;
		}

		private void EqualizationDown(int newCount)
		{
			var newPoint = new Complex[newCount];

			for (var i = 0; i < Count; i++)
				newPoint[i*newCount/Count] += this[i];

			Array = newPoint;
		}

		public Point[] GetPoints(Point startPoint)
		{
			var result = new Point[Count + 1];
			PointF sum = startPoint;
			result[0] = Point.Round(sum);
			for (var i = 0; i < Count; i++)
			{
				sum = sum.Offset((float) Array[i].Re, -(float) Array[i].Im);
				result[i + 1] = Point.Round(sum);
			}

			return result;
		}

		public RectangleF GetBoundsRect()
		{
			double minX = 0, maxX = 0, minY = 0, maxY = 0;
			double sumX = 0, sumY = 0;
			for (var i = 0; i < Count; i++)
			{
				var v = Array[i];
				sumX += v.Re;
				sumY += v.Im;
				if (sumX > maxX) maxX = sumX;
				if (sumX < minX) minX = sumX;
				if (sumY > maxY) maxY = sumY;
				if (sumY < minY) minY = sumY;
			}

			return new RectangleF((float) minX, (float) minY, (float) (maxX - minX), (float) (maxY - minY));
		}
	}
}
