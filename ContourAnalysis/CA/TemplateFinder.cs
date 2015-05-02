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

namespace ContourAnalysisNS
{
	public class TemplateFinder
	{
		public double MinACF = 0.96d;
		public double MinICF = 0.85d;
		public bool CheckICF = true;
		public bool CheckACF = true;
		public double MaxRotateAngle = Math.PI;
		public int MaxAcfDescriptorDeviation = 4;
		public string AntiPatternName = "antipattern";

		public FoundTemplateDesc FindTemplate(Templates templates, Template sample)
		{
			double rate = 0;
			double angle = 0;
			var interCorr = new Complex(0, 0);
			Template foundTemplate = null;
			foreach (var template in templates)
			{
				if (Math.Abs(sample.AutoCorrDescriptor1 - template.AutoCorrDescriptor1) > MaxAcfDescriptorDeviation) continue;
				if (Math.Abs(sample.AutoCorrDescriptor2 - template.AutoCorrDescriptor2) > MaxAcfDescriptorDeviation) continue;
				if (Math.Abs(sample.AutoCorrDescriptor3 - template.AutoCorrDescriptor3) > MaxAcfDescriptorDeviation) continue;
				if (Math.Abs(sample.AutoCorrDescriptor4 - template.AutoCorrDescriptor4) > MaxAcfDescriptorDeviation) continue;
				//
				double r = 0;
				if (CheckACF)
				{
					r = template.ACF.NotmalizedScalarProduct(sample.ACF).Norma;
					if (r < MinACF)
						continue;
				}
				if (CheckICF)
				{
					interCorr = template.Contour.InterCorrelationFunction(sample.Contour).FindMaxNormaItem();
					r = interCorr.Norma/(template.ContourNorma*sample.ContourNorma);
					if (r < MinICF)
						continue;
					if (Math.Abs(interCorr.Angle) > MaxRotateAngle)
						continue;
				}
				if (template.PreferredAngleNoMore90 && Math.Abs(interCorr.Angle) >= Math.PI/2)
					continue; //unsuitable angle
				//find max rate
				if (r >= rate)
				{
					rate = r;
					foundTemplate = template;
					angle = interCorr.Angle;
				}
			}
			//ignore antipatterns
			if (foundTemplate != null && foundTemplate.Name == AntiPatternName)
				foundTemplate = null;
			//
			if (foundTemplate != null)
				return new FoundTemplateDesc {Template = foundTemplate, Rate = rate, Sample = sample, Angle = angle};
			return null;
		}
	}
}
