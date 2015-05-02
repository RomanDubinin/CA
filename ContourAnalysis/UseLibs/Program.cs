using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using Emgu.CV;
using Emgu.CV.Structure;
using ContourAnalysisNS;

namespace UseLibs
{
	class Program
	{
		private static Capture Capture;
		static Image<Bgr, Byte> Frame;
		static ImageProcessor Processor;


		private const int CamWidth = 640;
		private const int CamHeight = 480;


		static void Main(string[] args)
		{
			Processor = new ImageProcessor();
			var templateFile = AppDomain.CurrentDomain.BaseDirectory + "\\TemplateStar.bin";
			LoadTemplates(templateFile);


			StartCapture();
			ApplySettings();

			var timer = new Timer(100);
			timer.Elapsed += TimerTick;
			timer.Start();
			Console.ReadLine();
		}

		private static void LoadTemplates(string fileName)
		{
			using (var fs = new FileStream(fileName, FileMode.Open))
				Processor.Templates = (Templates) new BinaryFormatter().Deserialize(fs);
		}

		private static void StartCapture()
		{
			Capture = new Capture();
			ApplyCamSettings();
		}

		private static void ApplyCamSettings()
		{
			Capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, CamWidth);
			Capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, CamHeight);
		}

		private static void TimerTick(object sender, ElapsedEventArgs e)
		{
			ProcessFrame();
		}

		private static void ProcessFrame()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			Frame = Capture.QueryFrame();
			Processor.ProcessImage(Frame);

			foreach (var found in Processor.FoundTemplates)
			{
				Rectangle foundRect = found.Sample.Contour.SourceBoundingRect;
				Console.WriteLine(foundRect.Location);
			}
			Console.WriteLine(stopWatch.Elapsed);
		}


		private static void ApplySettings()
		{
			Processor.EqualizeHist = false;
			Processor.Finder.MaxRotateAngle = Math.PI;
			Processor.MinContourArea = 10;
			Processor.MinContourLength = 15;
			Processor.Finder.MaxAcfDescriptorDeviation = 4;
			Processor.Finder.MinACF = 0.96;
			Processor.Finder.MinICF = 0.85;
			Processor.Blur = true;
			Processor.NoiseFilter = false;
			Processor.CannyThreshold = 50;
			Processor.AdaptiveThresholdBlockSize = 4;
			Processor.AdaptiveThresholdParameter =  1.5;
			
		}
	}
}
