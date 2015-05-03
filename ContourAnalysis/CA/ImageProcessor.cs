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
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ContourAnalysisNS
{
	public class ImageProcessor
	{
		//settings
		public bool EqualizeHist = false;
		public bool NoiseFilter = false;
		public int CannyThreshold = 50;
		public bool Blur = true;
		public int AdaptiveThresholdBlockSize = 4;
		public double AdaptiveThresholdParameter = 1.2d;
		public bool AddCanny = true;
		public bool FilterContoursBySize = true;
		public bool OnlyFindContours = false;
		public int MinContourLength = 15;
		public int MinContourArea = 10;
		public double MinFormFactor = 0.5;
		//
		public List<Contour<Point>> Contours;
		public Templates Templates = new Templates();
		public Templates Samples = new Templates();
		public List<FoundTemplateDesc> FoundTemplates = new List<FoundTemplateDesc>();
		public TemplateFinder Finder = new TemplateFinder();
		public Image<Gray, byte> BinarizedFrame;


		public void ProcessImage(Image<Bgr, byte> frame)
		{
			ProcessImage(frame.Convert<Gray, Byte>());
		}

		public void ProcessImage(Image<Gray, byte> grayFrame)
		{
			if (EqualizeHist)
				grayFrame._EqualizeHist(); //autocontrast
			//smoothed
			var smoothedGrayFrame = grayFrame.PyrDown();
			smoothedGrayFrame = smoothedGrayFrame.PyrUp();
			//canny
			Image<Gray, byte> cannyFrame = null;
			if (NoiseFilter)
				cannyFrame = smoothedGrayFrame.Canny(new Gray(CannyThreshold), new Gray(CannyThreshold));
			//smoothing
			if (Blur)
				grayFrame = smoothedGrayFrame;
			//binarize
			CvInvoke.cvAdaptiveThreshold(grayFrame, grayFrame, 255,
				Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY,
				AdaptiveThresholdBlockSize + AdaptiveThresholdBlockSize%2 + 1, AdaptiveThresholdParameter);
			//
			grayFrame._Not();
			//
			if (AddCanny)
				if (cannyFrame != null)
					grayFrame._Or(cannyFrame);
			//
			BinarizedFrame = grayFrame;

			//dilate canny contours for filtering
			if (cannyFrame != null)
				cannyFrame = cannyFrame.Dilate(3);

			//find contours
			var sourceContours = grayFrame.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE,
				Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
			//filter contours
			Contours = FilterContours(sourceContours, cannyFrame, grayFrame.Width, grayFrame.Height);
			//find templates
			lock (FoundTemplates)
				FoundTemplates.Clear();
			Samples.Clear();

			lock (Templates)
				Parallel.ForEach(Contours, contour =>
				{
					var arr = contour.ToArray();
					var sample = new Template(arr, contour.Area, Samples.TemplateSize);
					lock (Samples)
						Samples.Add(sample);

					if (!OnlyFindContours)
					{
						var desc = Finder.FindTemplate(Templates, sample);

						if (desc != null)
							lock (FoundTemplates)
								FoundTemplates.Add(desc);
					}
				}
					);
			//
			FilterByIntersection(ref FoundTemplates);
		}

		private static void FilterByIntersection(ref List<FoundTemplateDesc> templates)
		{
			//sort by area
			templates.Sort(
				(t1, t2) => -t1.Sample.Contour.SourceBoundingRect.Area().CompareTo(t2.Sample.Contour.SourceBoundingRect.Area()));
			//exclude templates inside other templates
			var toDel = new HashSet<int>();
			for (var i = 0; i < templates.Count; i++)
			{
				if (toDel.Contains(i))
					continue;
				var bigRect = templates[i].Sample.Contour.SourceBoundingRect;
				var bigArea = templates[i].Sample.Contour.SourceBoundingRect.Area();
				bigRect.Inflate(4, 4);
				for (var j = i + 1; j < templates.Count; j++)
				{
					if (bigRect.Contains(templates[j].Sample.Contour.SourceBoundingRect))
					{
						double includedRectArea = templates[j].Sample.Contour.SourceBoundingRect.Area();
						if (includedRectArea/bigArea > 0.9)
						{
							if (templates[i].Rate > templates[j].Rate)
								toDel.Add(j);
							else
								toDel.Add(i);
						}
						else
							toDel.Add(j);
					}
				}
			}
			templates = templates.Where((t, i) => !toDel.Contains(i)).ToList();
		}

		private List<Contour<Point>> FilterContours(Contour<Point> contour, Image<Gray, byte> cannyFrame, int frameWidth,
			int frameHeight)
		{
			var maxArea = frameWidth*frameHeight/5;
			var result = new List<Contour<Point>>();
			while (contour != null)
			{
				if (FilterContoursBySize)
					if (contour.Total < MinContourLength ||
						contour.Area < MinContourArea || contour.Area > maxArea ||
						contour.Area/contour.Total <= MinFormFactor)
						goto next;

				if (NoiseFilter)
				{
					var p1 = contour[0];
					var p2 = contour[(contour.Total/2)%contour.Total];
					if (cannyFrame[p1].Intensity <= double.Epsilon && cannyFrame[p2].Intensity <= double.Epsilon)
						goto next;
				}
				result.Add(contour);

				next:
					contour = contour.HNext;
			}

			return result;
		}
	}
}
