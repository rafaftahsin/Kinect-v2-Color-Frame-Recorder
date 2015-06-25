using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Kinect_Video_Recorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor ks;
        ColorFrameReader cfr;
        byte[] colorData;
        ColorImageFormat format;
        WriteableBitmap wbmp;
        BitmapSource bmpSource;
        int imageSerial;
        bool recordStarted;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Initializing variables
            ks = KinectSensor.GetDefault();
            ks.Open();
            var fd = ks.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            uint frameSize = fd.BytesPerPixel * fd.LengthInPixels;
            colorData = new byte[frameSize];
            format = ColorImageFormat.Bgra;
            recordStarted = false;

            // Deleting all previous image in ./img directory
            System.IO.DirectoryInfo directory = new DirectoryInfo("./img/");
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }


            cfr = ks.ColorFrameSource.OpenReader();
            cfr.FrameArrived += cfr_FrameArrived;
            start.Click += start_Click;
            stop.Click += stop_Click;

        }

        void stop_Click(object sender, RoutedEventArgs e)
        {
            recordStarted = false;
            Process.Start("ffmpeg.exe", "-framerate 10 -i ./img/%d.jpeg -c:v libx264 -r 30 -pix_fmt yuv420p kinect_video.mp4");
        }

        void start_Click(object sender, RoutedEventArgs e)
        {
            

            imageSerial = 0;
            recordStarted = true;
        }

        void cfr_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {    
            if (e.FrameReference == null) return;

            using (ColorFrame cf = e.FrameReference.AcquireFrame())
            {
                if(cf == null) return;
                cf.CopyConvertedFrameDataToArray( colorData, format);
                var fd = cf.FrameDescription;

                // Creating BitmapSource
                var bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel) / 8;
                var stride = bytesPerPixel * cf.FrameDescription.Width;

                bmpSource = BitmapSource.Create(fd.Width, fd.Height, 96.0, 96.0, PixelFormats.Bgr32, null, colorData, stride);
                
                // WritableBitmap to show on UI
                wbmp = new WriteableBitmap(bmpSource);
                kinectImage.Source = wbmp;

                // if record started start saving frames
                if (recordStarted)
                {
                    // JpegBitmapEncoder to save BitmapSource to file
                    // imageSerial is the serial of the sequential image
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                    using (var fs = new FileStream("./img/" + (imageSerial++) + ".jpeg", FileMode.Create, FileAccess.Write))
                    {
                        encoder.Save(fs);
                    }
                }                
            }
        }
    }
}
