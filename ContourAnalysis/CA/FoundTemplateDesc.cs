using System;

namespace ContourAnalysisNS
{
	public class FoundTemplateDesc
	{
		public double Rate;
		public Template Template;
		public Template Sample;
		public double Angle;
		public double Scale
		{
			get
			{
				return Math.Sqrt(Sample.SourceArea / Template.SourceArea);
			}
		}
	}
}