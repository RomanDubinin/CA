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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using ContourAnalysisNS;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ContourAnalysisDemo
{
	public partial class MainForm : Form
	{
		private Capture _capture;
		private Image<Bgr, Byte> Frame;
		private readonly ImageProcessor Processor;
		private readonly Dictionary<string, Image> AugmentedRealityImages = new Dictionary<string, Image>();

		private bool CaptureFromCam = true;
		private int FrameCount;
		private int OldFrameCount;
		private bool ShowAngle;
		private int CamWidth = 640;
		private int CamHeight = 480;
		private string TemplateFile;

		public MainForm()
		{
			InitializeComponent();
			//create image preocessor
			Processor = new ImageProcessor();
			//load default templates
			TemplateFile = "C:\\Study\\CA\\ContourAnalysis\\libs\\TemplateStar.bin";
			LoadTemplates(TemplateFile);
			//start capture from cam
			StartCapture();
			//apply settings
			ApplySettings();
			//
			Application.Idle += Application_Idle;
		}

		private void LoadTemplates(string fileName)
		{
			try
			{
				using (var fs = new FileStream(fileName, FileMode.Open))
					Processor.Templates = (Templates) new BinaryFormatter().Deserialize(fs);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void SaveTemplates(string fileName)
		{
			try
			{
				using (var fs = new FileStream(fileName, FileMode.Create))
					new BinaryFormatter().Serialize(fs, Processor.Templates);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void StartCapture()
		{
			try
			{
				_capture = new Capture();
				ApplyCamSettings();
			}
			catch (NullReferenceException ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void ApplyCamSettings()
		{
			try
			{
				_capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, CamWidth);
				_capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, CamHeight);
				cbCamResolution.Text = CamWidth + "x" + CamHeight;
			}
			catch (NullReferenceException ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			ProcessFrame();
		}

		private void ProcessFrame()
		{
			try
			{
				if (CaptureFromCam)
					Frame = _capture.QueryFrame();
				FrameCount++;
				//
				Processor.ProcessImage(Frame);
				//
				if (cbShowBinarized.Checked)
					ibMain.Image = Processor.BinarizedFrame;
				else
					ibMain.Image = Frame;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private void tmUpdateState_Tick(object sender, EventArgs e)
		{
			lbFPS.Text = (FrameCount - OldFrameCount) + " fps";
			OldFrameCount = FrameCount;
			if (Processor.Contours != null)
				lbContoursCount.Text = "Contours: " + Processor.Contours.Count;
			if (Processor.FoundTemplates != null)
				lbRecognized.Text = "Recognized contours: " + Processor.FoundTemplates.Count;
		}

		private void ibMain_Paint(object sender, PaintEventArgs e)
		{
			if (Frame == null) return;

			var font = new Font(Font.FontFamily, 24); //16

			e.Graphics.DrawString(lbFPS.Text, new Font(Font.FontFamily, 16), Brushes.Yellow, new PointF(1, 1));

			Brush bgBrush = new SolidBrush(Color.FromArgb(255, 0, 0, 0));
			Brush foreBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
			var borderPen = new Pen(Color.FromArgb(150, 0, 255, 0));
			//
			if (cbShowContours.Checked)
				foreach (var contour in Processor.Contours)
					if (contour.Total > 1)
						e.Graphics.DrawLines(Pens.Red, contour.ToArray());
			//
			lock (Processor.FoundTemplates)
				foreach (var found in Processor.FoundTemplates)
				{
					if (found.Template.Name.EndsWith(".png") || found.Template.Name.EndsWith(".jpg"))
					{
						DrawAugmentedReality(found, e.Graphics);
						continue;
					}

					var foundRect = found.Sample.Contour.SourceBoundingRect;
					var p1 = new Point((foundRect.Left + foundRect.Right)/2, foundRect.Top);
					var text = found.Template.Name;
					if (ShowAngle)
						text += string.Format("\r\nangle={0:000}°\r\nscale={1:0.0000}", 180*found.Angle/Math.PI, found.Scale);
					e.Graphics.DrawRectangle(borderPen, foundRect);
					e.Graphics.DrawString(text, font, bgBrush, new PointF(p1.X + 1 - font.Height/3, p1.Y + 1 - font.Height));
					e.Graphics.DrawString(text, font, foreBrush, new PointF(p1.X - font.Height/3, p1.Y - font.Height));
				}
		}

		private void DrawAugmentedReality(FoundTemplateDesc found, Graphics gr)
		{
			var fileName = Path.GetDirectoryName(TemplateFile) + "\\" + found.Template.Name;
			if (!AugmentedRealityImages.ContainsKey(fileName))
			{
				if (!File.Exists(fileName)) return;
				AugmentedRealityImages[fileName] = Image.FromFile(fileName);
			}
			var img = AugmentedRealityImages[fileName];
			var p = found.Sample.Contour.SourceBoundingRect.Center();
			var state = gr.Save();
			gr.TranslateTransform(p.X, p.Y);
			gr.RotateTransform((float) (180f*found.Angle/Math.PI));
			gr.ScaleTransform((float) (found.Scale), (float) (found.Scale));
			gr.DrawImage(img, new Point(-img.Width/2, -img.Height/2));
			gr.Restore(state);
		}

		private void cbAutoContrast_CheckedChanged(object sender, EventArgs e)
		{
			ApplySettings();
		}

		private void ApplySettings()
		{
			try
			{
				Processor.EqualizeHist = cbAutoContrast.Checked;
				ShowAngle = cbShowAngle.Checked;
				CaptureFromCam = cbCaptureFromCam.Checked;
				btLoadImage.Enabled = !CaptureFromCam;
				cbCamResolution.Enabled = CaptureFromCam;
				Processor.Finder.MaxRotateAngle = cbAllowAngleMore45.Checked ? Math.PI : Math.PI/4;
				Processor.MinContourArea = (int) nudMinContourArea.Value;
				Processor.MinContourLength = (int) nudMinContourLength.Value;
				Processor.Finder.MaxAcfDescriptorDeviation = (int) nudMaxACFdesc.Value;
				Processor.Finder.MinACF = (double) nudMinACF.Value;
				Processor.Finder.MinICF = (double) nudMinICF.Value;
				Processor.Blur = cbBlur.Checked;
				Processor.NoiseFilter = cbNoiseFilter.Checked;
				Processor.CannyThreshold = (int) nudMinDefinition.Value;
				nudMinDefinition.Enabled = Processor.NoiseFilter;
				Processor.AdaptiveThresholdBlockSize = (int) nudAdaptiveThBlockSize.Value;
				Processor.AdaptiveThresholdParameter = cbAdaptiveNoiseFilter.Checked ? 1.5 : 0.5;
				//cam resolution
				var parts = cbCamResolution.Text.ToLower().Split('x');
				if (parts.Length == 2)
				{
					var camWidth = int.Parse(parts[0]);
					var camHeight = int.Parse(parts[1]);
					if (CamHeight != camHeight || CamWidth != camWidth)
					{
						CamWidth = camWidth;
						CamHeight = camHeight;
						ApplyCamSettings();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void btLoadImage_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			ofd.Filter = "Image|*.bmp;*.png;*.jpg;*.jpeg";
			if (ofd.ShowDialog(this) == DialogResult.OK)
				try
				{
					Frame = new Image<Bgr, byte>((Bitmap) Image.FromFile(ofd.FileName));
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
		}

		private void btCreateTemplate_Click(object sender, EventArgs e)
		{
			if (Frame != null)
				new ShowContoursForm(Processor.Templates, Processor.Samples, Frame).ShowDialog();
		}

		private void btNewTemplates_Click(object sender, EventArgs e)
		{
			if (
				MessageBox.Show("Do you want to create new template database?", "Create new template database",
					MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
				Processor.Templates.Clear();
		}

		private void btOpenTemplates_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			ofd.Filter = "Templates(*.bin)|*.bin";
			if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				TemplateFile = ofd.FileName;
				LoadTemplates(TemplateFile);
			}
		}

		private void btSaveTemplates_Click(object sender, EventArgs e)
		{
			var sfd = new SaveFileDialog();
			sfd.Filter = "Templates(*.bin)|*.bin";
			if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				TemplateFile = sfd.FileName;
				SaveTemplates(TemplateFile);
			}
		}

		private void btTemplateEditor_Click(object sender, EventArgs e)
		{
			new TemplateEditor(Processor.Templates).Show();
		}

		private void btAutoGenerate_Click(object sender, EventArgs e)
		{
			new AutoGenerateForm(Processor).ShowDialog();
		}
	}
}
