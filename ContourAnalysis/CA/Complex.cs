using System;

namespace ContourAnalysisNS
{
	[Serializable]
	public struct Complex
	{
		public double Re;
		public double Im;

		public Complex(double re, double im)
		{
			Re = re;
			Im = im;
		}

		public static Complex FromExp(double r, double angle)
		{
			return new Complex(r*Math.Cos(angle), r*Math.Sin(angle));
		}

		public double Angle
		{
			get { return Math.Atan2(Im, Re); }
		}

		public override string ToString()
		{
			return Re + "+i" + Im;
		}

		public double Norma
		{
			get { return Math.Sqrt(Re*Re + Im*Im); }
		}

		public double NormaSquare
		{
			get { return Re*Re + Im*Im; }
		}

		public static Complex operator +(Complex x1, Complex x2)
		{
			return new Complex(x1.Re + x2.Re, x1.Im + x2.Im);
		}

		public static Complex operator -(Complex x1, Complex x2)
		{
			return new Complex(x1.Re - x2.Re, x1.Im - x2.Im);
		}

		public static Complex operator *(double k, Complex x)
		{
			return new Complex(k*x.Re, k*x.Im);
		}

		public static Complex operator *(Complex x, double k)
		{
			return new Complex(k*x.Re, k*x.Im);
		}

		public static Complex operator *(Complex x1, Complex x2)
		{
			return new Complex(x1.Re*x2.Re - x1.Im*x2.Im, x1.Im*x2.Re + x1.Re*x2.Im);
		}

		public double CosAngle()
		{
			return Re/Math.Sqrt(Re*Re + Im*Im);
		}

		public Complex Rotate(double cosAngle, double sinAngle)
		{
			return new Complex(cosAngle*Re - sinAngle*Im, sinAngle*Re + cosAngle*Im);
		}

		public Complex Rotate(double angle)
		{
			var cosAngle = Math.Cos(angle);
			var sinAngle = Math.Sin(angle);
			return new Complex(cosAngle*Re - sinAngle*Im, sinAngle*Re + cosAngle*Im);
		}
	}
}