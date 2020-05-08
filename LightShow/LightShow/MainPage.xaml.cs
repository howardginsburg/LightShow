using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LightShow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int _state = STATE_STOPPED;
        private static int STATE_STOPPED = 0;
        private static int STATE_PARTY = 1;
        private static int STATE_GLOW = 2;

        private static string MQTT_BROKER_ADDRESS = "";
        private MqttClient _client;

        private static ulong BYTES_TO_READ = 1024;
        private ulong _position = 0;
        private ulong _currentSize = 0;
        
        private MediaCapture _mediaCapture;
        private LowLagMediaRecording _mediaRecording;
        private IRandomAccessStream _stream;
        private DispatcherTimer _timer;
        private static int RATE = 44100;
        private static int BUFFER_SAMPLES = 1024;
        private static double audioValueMax = 0;
        private static double audioValueLast = 0;

        private DispatcherTimer _restartTimer;
        public MainPage()
        {
            this.InitializeComponent();
            _restartTimer = new DispatcherTimer();
            _restartTimer.Tick += restartTimer_Tick;
            _restartTimer.Interval = new TimeSpan(0, 5, 0); //5 minutes
            _restartTimer.Start();

        }

        private async Task Start()
        {
            try
            {
                if (_state == STATE_PARTY)
                {
                    return;
                }

                

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();
                _mediaCapture.Failed += MediaCapture_Failed;

                _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

                _stream = new InMemoryRandomAccessStream();
                _position = 0;

                _mediaRecording = await _mediaCapture.PrepareLowLagRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.High), _stream);

                await _mediaRecording.StartAsync();

                _timer = new DispatcherTimer();
                _timer.Tick += timer_Tick;
                _timer.Interval = new TimeSpan(0, 0, (int)((double)BUFFER_SAMPLES / (double)RATE));
                _timer.Start();

                MQTT_BROKER_ADDRESS = txtMQTTServer.Text;
                // create client instance 
                _client = new MqttClient(MQTT_BROKER_ADDRESS);

                string clientId = "publisher";//Guid.NewGuid().ToString();
                _client.Connect(clientId);

                _state = STATE_PARTY;
                
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }
        }

        private async Task Stop()
        {
            if (_state != STATE_PARTY)
            {
                return;
            }
            await _mediaRecording.StopAsync();
            _timer.Stop();
            UpdateStatus("Ready!");

            _state = STATE_STOPPED;
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            await Start();
        }

        private async void timer_Tick(object sender, object e)
        {
            CalculateLights();
        }

        private async void restartTimer_Tick(object sender, object e)
        {
            if (_state == STATE_PARTY)
            {
                await Stop();
                await Start();
            }
        }

        private async void CalculateLights()
        {
            try
            {
                //Debug.WriteLine("Calculating");
                //Calculate how many bytes of sound were captured since we last processed.
                _currentSize = _stream.Size - _currentSize;
                
                _position = _stream.Size - BYTES_TO_READ;
                // This is where the byteArray to be stored.
                var buffer = new byte[BYTES_TO_READ];
                //Debug.WriteLine("Creating data reader.");
                //await _stream.ReadAsync(buffer.AsBuffer(), (uint)buffer.Length, InputStreamOptions.None);
                DataReader reader = new DataReader(_stream.GetInputStreamAt(_position));
                //Debug.WriteLine("Loading data reader.");
                await reader.LoadAsync((uint)buffer.Length);
                //Debug.WriteLine("reading bytes.");
                reader.ReadBytes(buffer);
                //List<byte> bytes = new List<byte>();
                _position += _currentSize;

                float max = 0;

                // interpret as 16 bit audio
                for (int index = 0; index < buffer.Length; index += 2)
                {
                    //Debug.WriteLine("Shifting bytes");
                    short sample = (short)((buffer[index + 1] << 8) |
                                            buffer[index + 0]);
                    //Debug.WriteLine("Converting to floating point.");
                    var sample32 = sample / 32768f; // to floating point
                    if (sample32 < 0) sample32 = -sample32; // absolute value 
                    if (sample32 > max) max = sample32; // is this the max value?
                }

                //Debug.WriteLine("Determining max");
                // calculate what fraction this peak is of previous peaks
                if (max > audioValueMax)
                {
                    audioValueMax = (double)max;
                }
                audioValueLast = max;

                //Debug.WriteLine("Figuring out pct");
                double pct = audioValueLast / audioValueMax * 100;
                lblMeasure.Text = pct + "";

                pct = pct % 10;

                _client.Publish("/lights", System.Text.Encoding.UTF8.GetBytes(pct + ""), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }
        }

        private double RootMeanSquare(byte[] values)
        {
            double s = 0;
            int i;
            for (i = 0; i < values.Length; i++)
            {
                s += values[i] * values[i];
            }
            return Math.Sqrt(s / values.Length);
        }

        private void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            UpdateStatus("Record limitation exceeded.");
            //await sender.StopRecordAsync(); ;
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            UpdateStatus("Media capture failed.");
            UpdateStatus(errorEventArgs.Message);
        }

        
        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private async void BtnSample_Click(object sender, RoutedEventArgs e)
        {
            CalculateLights();
        }

        private async void UpdateStatus(string text)
        {
            lblMessages.Text = lblMessages + "\r\n" + text;
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void BtnTestMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MQTT_BROKER_ADDRESS = txtMQTTServer.Text;
                // create client instance 
                _client = new MqttClient(MQTT_BROKER_ADDRESS);

                string clientId = "publisher";//Guid.NewGuid().ToString();
                _client.Connect(clientId);
                _client.Publish("/lights", System.Text.Encoding.UTF8.GetBytes(80 + ""), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
                UpdateStatus("Test message sent.");

            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }

        }

        private void BtnGlow_Click(object sender, RoutedEventArgs e)
        {
            MQTT_BROKER_ADDRESS = txtMQTTServer.Text;
            // create client instance 
            _client = new MqttClient(MQTT_BROKER_ADDRESS);

            string clientId = "publisher";//Guid.NewGuid().ToString();
            _client.Connect(clientId);
            _client.Publish("/dinner", System.Text.Encoding.UTF8.GetBytes(80 + ""), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

            _state = STATE_GLOW;
        }
    }
}
