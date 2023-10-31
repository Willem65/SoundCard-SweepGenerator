using System;
using System.Linq;
using System.Windows.Forms;
using System.Numerics;
using NAudio.Wave;
using System.Threading;
using Accord.Math;
using System.Diagnostics;
using NAudio.Wave.SampleProviders;

namespace WaveCharting
{
    public partial class Form1 : Form
    {

        // Initialize ManualResetEvent wordt wel of niet gebruikt
        ManualResetEvent syncEvent = new ManualResetEvent(false);

        //private const double V = 0.00002;
        public WaveIn wi;
        public BufferedWaveProvider bwp;
        public Int32 envelopeMax;

        int deviceRec, devicePlayOut;

        // 1) Signal Gen -> Speaker
        public WaveOut waveOut;
        SignalGenerator signalGenerator;


        // -------- InitializeComponent -----------------
        public Form1()
        {
            InitializeComponent();
            
            scottPlotUC1.Plot.Style(ScottPlot.Style.Black);
            scottPlotUC2.Plot.Style(ScottPlot.Style.Black);



            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                var caps = NAudio.Wave.WaveIn.GetCapabilities(i);
                comboBox1.Items.Add(i + ": " + caps.ProductName);
            }

            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                var caps = NAudio.Wave.WaveOut.GetCapabilities(i);
                comboBox2.Items.Add(i + ": " + caps.ProductName);
            }


            initSoundCard(12);
            comboBox1.SelectedIndex = 12;
            initSoundCardOut(9);
            comboBox2.SelectedIndex = 9;
        }



        // -----Soundcard init for play-----------------------------------------------------------
        public void initSoundCardOut(int soundDevice)
        {
            signalGenerator = new SignalGenerator();
            signalGenerator.Type = SignalGeneratorType.Sweep;
            //signalGenerator.Type = SignalGeneratorType.Sin;

            signalGenerator.Frequency = 1;
            signalGenerator.FrequencyEnd = 20000;
            signalGenerator.SweepLengthSecs = .1;
            

            waveOut = new WaveOut();
            waveOut.DeviceNumber = soundDevice;

            //wi.DataAvailable += new EventHandler<WaveInEventArgs>(wi_DataAvailable);
            //waveOut.Init(signalGenerator);
        }



        // ------Soundcard init for Record-----------------------------------------------------------

        // 96000 X 24 X 2 = BitRate per seconden    96000 X 24 X 2 = 4608000 / 8 = 576 Kb/s
        // 96000 X 16 X 2 = BitRate per seconden    96000 X 16 X 2 = 3072000 / 8 = 384 Kb/s

        private int SAMPLERATE = 96000; // sample SAMPLERATE of the sound card
        private int BUFFERSIZE = ((int)Math.Pow(2, 15))*2; // must be a multiple of 2   private int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2
        int BYTES_PER_POINT = 2;

        // 2^13 X 96000 X 2 = 393,216 Kb
        // 2^13 X 96000 X 1 = 196,608 Kb

        // initSoundCard and buffer for Recording
        public void initSoundCard(int soundDevice)
        {

            wi = new WaveIn();

            wi.DeviceNumber = soundDevice;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(SAMPLERATE, 1);

            wi.BufferMilliseconds = (int)( (double)BUFFERSIZE / (double)SAMPLERATE * 1000.0 ); // 21  // BUFFERSIZE = 2048

            //  2^13 = 8192

            // 8192 / 96000 * 1000 = 85,333333 mSec

            //Debug.Print( ((int)((double)8196 / (double)96000 * 1000.0)).ToString());

            //create a wave buffer and start the recording
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(wi_DataAvailable);

            bwp = new BufferedWaveProvider(wi.WaveFormat);
            bwp.BufferLength = BUFFERSIZE * BYTES_PER_POINT;

            bwp.DiscardOnBufferOverflow = true;
            //wi.StartRecording();
        }





        // adds data to the audio recording buffer  bwp
        void wi_DataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }



        //-------------------------------------------------
        // --- SUB ROUTINE FOR UPDATE GRAPH ---------------
        //-------------------------------------------------
        public void UpdateAudioGraph()
        {

            var frames = new byte[BUFFERSIZE];

            // read the bytes from the stream bwp
            bwp.Read(frames, 0, BUFFERSIZE);

            if (frames.Length == 0) return;      // frames 4096

            double[] Ys = new double[frames.Length / BYTES_PER_POINT];
            double[] Xs = new double[frames.Length / BYTES_PER_POINT];

            Int32[] vals = new Int32[frames.Length / BYTES_PER_POINT];   // Bytes per point 2

            for (int i = 0; i < vals.Length; i++)
            {
                byte hByte = frames[i * 2 + 1];
                byte lByte = frames[i * 2 + 0];

                vals[i] = (int)(short)((hByte << 8) | lByte);
                
                Ys[i] = vals[i];
                Xs[i] = i/1;
            }

            scottPlotUC1.Plot.AddSignalXY(Xs, Ys, color: System.Drawing.Color.Red);

            scottPlotUC1.Render();

            // Set axis limits to control the view
           // scottPlotUC1.Plot.SetAxisLimits(0, BUFFERSIZE / 4, -10000, 10000);
          //  scottPlotUC2.Plot.SetAxisLimits(0, 512, -1, 5);
        }


        private void formsPlot2_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //////wi.StartRecording();
            ////////syncEvent.WaitOne();
            //////waveOut.Init(signalGenerator);
            //////waveOut.Play();
            ////////syncEvent.Set();
            ////////Thread.Sleep(200);
            ////////waveOut.Stop();
            ////////bwp.ClearBuffer();
            //////Thread.Sleep(600);
            //////wi.StopRecording();
            ////////wi.Dispose();
            ////////waveOut.Dispose();
            ////////Thread.Sleep(200);
            //////scottPlotUC1.Plot.Clear();
            //////Thread.Sleep(200);
            //////scottPlotUC1.Plot.SetAxisLimits(0, BUFFERSIZE / 3, -10000, 10000);
            
            //////UpdateAudioGraph();
            
            //////Thread.Sleep(600);

        }







        //################################ END ##################################################

 //------------------------------ Hieronder staan de BUTTONS ----------------------------------------------
        //button stop recording
        private void button1_Click(object sender, EventArgs e)
        {
            wi.StopRecording();
            wi.Dispose();
            Thread.Sleep(250);
            bwp.ClearBuffer();
        }

        // button UpdateAudioGraph
        private void button2_Click(object sender, EventArgs e)
        {
            scottPlotUC1.Plot.Clear();
            UpdateAudioGraph();            
            //Thread.Sleep(250);            
        }

        //Button Play
        private void button3_Click(object sender, EventArgs e)
        {
            wi.StartRecording();
            waveOut.Init(signalGenerator);
            waveOut.Play();
            Thread.Sleep(200);
            waveOut.Stop();
            bwp.ClearBuffer();
            wi.StopRecording();
            timer1.Enabled = true;
        }










        // ---------------------- SELCTEER SOUNDCARD PLAY DMV COMBOBOX -----------------------------------------
        // Soundcard for play
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox2.SelectedItem != null)
                {
                    devicePlayOut = comboBox2.SelectedIndex;
                    initSoundCardOut(devicePlayOut);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Choose a soundcard device ");
                //comboBox1.SelectedItem = null;
            }
        }

        // ---------------------- SELCTEER SOUNDCARD REC DMV COMBOBOX -----------------------------------------
        // Soundcard for Rec
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    deviceRec = comboBox1.SelectedIndex;
                    initSoundCard(deviceRec);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Choose a soundcard device ");
                //comboBox1.SelectedItem = null;
            }
        }

        private void scottPlotUC2_Load(object sender, EventArgs e)
        {

        }

        //--------------------- Dingen die gebruikt worden ----------------------------------------
        private void openWaveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

    }
}
