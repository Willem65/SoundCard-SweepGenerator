using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using ScottPlot;

using System.Numerics;

using System.Threading;
using Accord.Math;
using System.Diagnostics;
using NAudio.Wave.SampleProviders;
using NAudio.Gui;
using System.IO;

namespace WaveCharting
{
    public partial class Form2 : Form
    {
        public WaveIn wi;
        public BufferedWaveProvider bwp;
        public WaveOut waveOut;
        public bool vlag1 = false;

        double[] Ys; 
        double[] Ys2;
        double[] Xs; 
        double[] Xs2;
        //double[] Ys2C;

        double trackBvalue, trackBvalue2;

        int deviceRec, devicePlayOut;
        Int32 xLengteFFT;

        SignalGenerator signalGenerator = new SignalGenerator();

        // ------Soundcard init for Record-----------------------------------------------------------
        private int SAMPLERATE = 96000; // sample SAMPLERATE of the sound card
        private int BUFFERSIZE = ((int)Math.Pow(2, 15)); // must be a multiple of 2   private int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2
        int BYTES_PER_POINT = 2;
        
        public Form2()
        {
            InitializeComponent();
        }

        // -----Soundcard init for play-----------------------------------------------------------
        public void initSoundCardOut(int soundDevice)
        {
            signalGenerator.Type = SignalGeneratorType.Sweep;
            signalGenerator.Frequency = 10;
            signalGenerator.FrequencyEnd = 30000;
            signalGenerator.SweepLengthSecs = 15; // (trackBvalue / 100000) + (trackBvalue2 / 1000);
            waveOut = new WaveOut();
            waveOut.DeviceNumber = soundDevice;
        }

        // initSoundCard and buffer for Recording
        public void initSoundCard(int soundDevice)
        {
            wi = new WaveIn();
            wi.DeviceNumber = soundDevice;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(SAMPLERATE, 1);
            bwp = new BufferedWaveProvider(wi.WaveFormat);
        }
        

        // Als er ingangs data beschikbaar is, kom dan hier (callback)
        void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            vlag1 = true;
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);

            var frames = new byte[BUFFERSIZE];

            // read the bytes from the stream bwp
            bwp.Read(frames, 0, BUFFERSIZE);

            if (frames.Length == 0) return;      // frames 4096

            

            Ys = new double[frames.Length / BYTES_PER_POINT];
            Xs = new double[frames.Length / BYTES_PER_POINT];
            Ys2 = new double[frames.Length / BYTES_PER_POINT];
            Xs2 = new double[frames.Length / BYTES_PER_POINT];

           // vals = new Int32[frames.Length / BYTES_PER_POINT];   // Bytes per point 2
            Int32[] vals = new Int32[frames.Length / BYTES_PER_POINT];

            xLengteFFT = vals.Length;

            for (int i = 0; i < vals.Length; i++)
            {
                byte hByte = frames[i * 2 + 1];
                byte lByte = frames[i * 2 + 0];

                vals[i] = (int)(short)((hByte << 8) | lByte);

                Ys[i] = vals[i];
                Xs[i] = (i * 2);
                Xs2[i] = Math.Log10((double)i / Ys.Length * 96000 / 1000.0)*10; // units are in kHz

                //if ((i > 100) && (i < 130)) Ys[i] = 8000;  // Markeer puls zichtbaar maken in de grafiek
            }
            timer1.Enabled = true;
        }



        // Get the Amplitude for 512 points
        public double[] SineWaveAmp(double[] amp)
        {

            double[] wave = new double[amp.Length];

            for (int i = 0; i < amp.Length; i++)
            {

                //wave[i] = new Complex(amp[i], 0.0); // make it complex format (imaginary = 0)
                //wave[i] = Math.Abs(amp.Max() - amp.Min());
            }

            //Accord.Math.FourierTransform.SineWaveAmp(1,1);

            return wave;
        }


        ////////public double[] FFT(double[] amp)
        ////////{

        ////////    double[] wave = new double[amp.Length];
        ////////    Complex[] fftComplex = new Complex[data.Length];

        ////////    for (int i = 0; i < amp.Length; i++)
        ////////    {

        ////////        //wave[i] = new Complex(amp[i], 0.0); // make it complex format (imaginary = 0)
        ////////        // wave[i] = Math.Abs(amp.Max() - amp.Min());
        ////////        //wave[i] = amp.Average();
        ////////        fftComplex[i] = new Complex(amp[i], 0.0);
        ////////    }

        ////////    //Accord.Math.FourierTransform.SineWaveAmp(1,1);

        ////////    return wave;

        ////////}

        public double[] FFT(double[] data)
        {
            double[] fft = new double[data.Length]; // this is where we will store the output (fft)
            Complex[] fftComplex = new Complex[data.Length]; // the FFT function requires complex format
            for (int i = 0; i < data.Length; i++)
            {
                fftComplex[i] = new Complex(data[i], 0.0); // make it complex format (imaginary = 0)
            }
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length; i++)
            {
                fft[i] = fftComplex[i].Magnitude; // back to double
                //fft[i] = Math.Log10(fft[i]); // convert to dB
            }
            return fft;
            //todo: this could be much faster by reusing variables
        }


        // Als de uitgangs data afgespeeld is, kom dan hier (callback)
        //void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        //{
        //    waveOut.Play();
        //}


        // Eens in de zo veel tijd de grafiek zichtbaar maken
        private void timer1_Tick(object sender, EventArgs e)
        {
            DisplayChart(Xs, Ys);
            formsPlot1.Render();

            Ys2 = FFT(Ys);
            DisplayChartFFT(Xs2, Ys2);
            formsPlot2.Render();
            timer1.Enabled = false;
            //waveOut.Stop();
        }



        // Start button
        private void button1_Click(object sender, EventArgs e)
        {
            //signalGenerator.SweepLengthSecs = (trackBvalue) + (trackBvalue2);
            //initSoundCard(deviceRec);
            //initSoundCardOut(devicePlayOut);
            //waveOut.Dispose();
            //vlag1 = true;
            timer1.Interval = 10;

            //waveOut.PlaybackStopped += WaveOut_PlaybackStopped;   // event handler
            //wi.DataAvailable += WaveIn_DataAvailable;    // event handler

            if (vlag1 == true)
            {
                vlag1 = false;
                wi.StopRecording();
                //wi.Dispose();
            }
            //initStartUp();
            wi.StartRecording();

            waveOut.Init(signalGenerator);
            waveOut.Play();

        }

        double[] Ys2C;

        private void button2_Click(object sender, EventArgs e)
        {
            //Ys2C = new double[20000];
            Ys2C = new double[xLengteFFT];

            if (Xs2 != null)
            {
                Ys2C = Ys2;

                for (int i = 0; i < xLengteFFT; i++)
                {
                    Ys2C[i] = -(Ys2C[i]);
                    //Debug.Print(Xs2[i].ToString() + "  " + (Ys2C[i]).ToString());

                    //Xs2C[xLengteFFT] = (Int32)Ys2[i];
                }
            }
        }




        void DisplayChart(double[] Xs, double[] Ys)
        {
            //syncEvent.WaitOne();
            if (Xs != null)
            {
                formsPlot1.Plot.Clear();
                formsPlot1.Plot.SetAxisLimits(0 , (BUFFERSIZE / 1.5) , -10000, 10000);
                
                // disable left-click-drag pan
                formsPlot1.Configuration.Pan = false;

                // disable right-click-drag zoom
                formsPlot1.Configuration.Zoom = false;

                // disable scroll wheel zoom
                formsPlot1.Configuration.ScrollWheelZoom = false;

                // disable middle-click-drag zoom window
                formsPlot1.Configuration.MiddleClickDragZoom = false;
                if (Xs != null) formsPlot1.Plot.AddSignalXY(Xs, Ys, color: System.Drawing.Color.Red);
                //formsPlot1.Render();
            }
        }

        void DisplayChartFFT(double[] Xs, double[] Ys)
        {
            //syncEvent.WaitOne();
            if (Xs != null)
            {
                formsPlot2.Plot.Clear();


                // disable left-click-drag pan
                formsPlot2.Configuration.Pan = false;

                // disable right-click-drag zoom
                formsPlot2.Configuration.Zoom = false;

                // disable scroll wheel zoom
                formsPlot2.Configuration.ScrollWheelZoom = false;

                // disable middle-click-drag zoom window
                formsPlot2.Configuration.MiddleClickDragZoom = false;

                Xs = Xs.Take(Xs.Length / 2).ToArray();

                //formsPlot2.Plot.SetAxisLimits(0, 50, -1, 1000);
                //Ys = Ys.Take(Ys.Length / 2).ToArray();

                formsPlot2.Plot.SetAxisLimits(-25, 20, -1, 4);

                if (Ys2C != null)
                {
                    for (int i = 0; i < xLengteFFT; i++)
                    {
                        Ys[i] = (Ys[i]) + (Ys2C[i] + 100);
                    }
                }

                    
                Ys = ScottPlot.Tools.Log10(Ys.Take(Ys.Length / 2).ToArray());

                //formsPlot2.Plot.PlotSignalXY(Xs, Ys, color: System.Drawing.Color.Orange, lineWidth: 1);
                if (Xs != null) formsPlot2.Plot.AddSignalXY(Xs, Ys, color: System.Drawing.Color.Red);
                //formsPlot1.Render();
            }
        }




        //####################################################################################################
        //------------------------------ Hieronder staan de Sliders ----------------------------------------------

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            trackBvalue = trackBar1.Value;
            trackBvalue = trackBvalue / 100000;
            signalGenerator.SweepLengthSecs = trackBvalue + trackBvalue2;
            label1.Text = (trackBvalue + trackBvalue2).ToString();

            StreamWriter wrl = new StreamWriter("TrackBar1Val.TXT");
            if ((trackBvalue + trackBvalue2) > -100) wrl.Write((trackBar1.Value).ToString());            
            wrl.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            initStartUp();
            wi.DataAvailable += WaveIn_DataAvailable;    // event handler
        }

        private void initStartUp()
        {
            formsPlot1.Plot.Style(ScottPlot.Style.Black);
            formsPlot2.Plot.Style(ScottPlot.Style.Black);

            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                var caps = NAudio.Wave.WaveIn.GetCapabilities(i);
                comboBox2.Items.Add(i + ": " + caps.ProductName);
            }

            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                var caps = NAudio.Wave.WaveOut.GetCapabilities(i);
                comboBox1.Items.Add(i + ": " + caps.ProductName);
            }

            StreamReader rdl = new StreamReader("deviceRec.TXT");
            int rec = Convert.ToInt32(rdl.ReadLine());
            rdl.Close();
            initSoundCard(rec);
            comboBox2.SelectedIndex = rec;


            StreamReader rdll = new StreamReader("devicePlayOut.TXT");
            int play = Convert.ToInt32(rdll.ReadLine());
            rdll.Close();
            initSoundCardOut(play);
            comboBox1.SelectedIndex = play;

            StreamReader rdlll = new StreamReader("TrackBar1Val.TXT");
            int temp = Convert.ToInt32(rdlll.ReadLine());
            trackBvalue = temp / 100000.0;
            trackBar1.Value = temp;
            rdlll.Close();

            StreamReader rdllll = new StreamReader("TrackBar2Val.TXT");
            temp = Convert.ToInt32(rdllll.ReadLine());
            trackBvalue2 = temp / 100.0;
            trackBar2.Value = temp;
            rdllll.Close();
            //signalGenerator.SweepLengthSecs = (trackBvalue ) + (trackBvalue2 );
            signalGenerator.SweepLengthSecs = (trackBvalue) + (trackBvalue2);
            label1.Text = ((trackBvalue) + (trackBvalue2)).ToString();
        }



        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            trackBvalue2 = trackBar2.Value;
            trackBvalue2 = (trackBvalue2 / 100);
            signalGenerator.SweepLengthSecs = trackBvalue + trackBvalue2;
            label1.Text = (trackBvalue + trackBvalue2).ToString();

            StreamWriter wrl = new StreamWriter("TrackBar2Val.TXT");
            if( (trackBvalue + trackBvalue2) > 0 ) wrl.Write((trackBar2.Value).ToString());
            wrl.Close();
        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }



        // ---------------------- SELCTEER SOUNDCARD REC DMV COMBOBOX -----------------------------------------
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox2.SelectedItem != null)
                {
                    deviceRec = comboBox2.SelectedIndex;
                    StreamWriter wrl = new StreamWriter("deviceRec.TXT");
                    wrl.Write((deviceRec).ToString());
                    wrl.Close();
                    initSoundCard(deviceRec);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Choose a soundcard device ");
                //comboBox1.SelectedItem = null;
            }
        }

        // ---------------------- SELCTEER SOUNDCARD PLAY DMV COMBOBOX -----------------------------------------
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    //waveOut.Stop();
                    //waveOut.Dispose();
                    devicePlayOut = comboBox1.SelectedIndex;
                    StreamWriter wrl = new StreamWriter("devicePlayOut.TXT");
                    wrl.Write((devicePlayOut).ToString());
                    wrl.Close();
                    //initSoundCardOut(devicePlayOut);
                    //signalGenerator.SweepLengthSecs = trackBvalue + trackBvalue2;
                    //waveOut.Play();

                }
            }
            catch (Exception)
            {
                MessageBox.Show("Choose a soundcard device ");
                //comboBox1.SelectedItem = null;
            }
        }

    }
}
