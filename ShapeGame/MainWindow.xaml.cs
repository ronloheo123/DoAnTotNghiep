//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// This module contains code to do Kinect NUI initialization,
// processing, displaying players on screen, and sending updated player
// positions to the game portion for hit testing.



using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;

namespace ShapeGame
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Media;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        #region Private State
        private const int TimerResolution = 2;  // ms
        private const int NumIntraFrames = 3;
        private const double MaxFramerate = 70;
        private const double MinFramerate = 15;
        private const double MinShapeSize = 12;
        private const double MaxShapeSize = 90;

        private readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
        /*private readonly SoundPlayer popSound = new SoundPlayer();
        private readonly SoundPlayer hitSound = new SoundPlayer();
        private readonly SoundPlayer squeezeSound = new SoundPlayer();*/

        private DateTime lastFrameDrawn = DateTime.MinValue;
        private DateTime predNextFrame = DateTime.MinValue;
        private double actualFrameTime;

        private Skeleton[] skeletonData;
        Model3DGroup models;
        // Player(s) placement in scene (z collapsed):
        private Rect playerBounds;
        private double targetFramerate = MaxFramerate;
        private int frameCount;
        private bool runningGameThread;
        private int playersAlive;

        //private SpeechRecognizer mySpeechRecognizer;
        //Viewport3D myViewport3D;

        #endregion Private State

        #region ctor + Window Events
        private KinectSensor kSensor;
        private WriteableBitmap colorBitmap;

        public MainWindow()
        {
            InitializeComponent();
            this.RestoreWindowState();
            this.Loaded += (s, e) => { Discover(); };
            this.Unloaded += (s, e) => { };

        }
        private void Discover()
        {
            KinectSensorCollection kinectCollection = KinectSensor.KinectSensors;

            foreach (KinectSensor kTemp in kinectCollection)
            {
                kSensor = kTemp;
            }

            if (kSensor == null)
            {
                return;
            }

            kSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kSensor.SkeletonStream.Enable();
            kSensor.DepthStream.Enable();

            kSensor.AllFramesReady += All_Frames_Ready;

            // Khởi tạo WriteableBitmap cho hiển thị hình ảnh màu
            colorBitmap = new WriteableBitmap(kSensor.ColorStream.FrameWidth, kSensor.ColorStream.FrameHeight, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr32, null);

            // Gán source cho kinectImage
            kinectImage.Source = colorBitmap;

            // Set kích thước và vị trí của kinectImage
            kinectImage.Width = 200; // Thay đổi kích thước theo nhu cầu
            kinectImage.Height = 150; // Thay đổi kích thước theo nhu cầu
            kSensor.Start();
        }


        private void All_Frames_Ready(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy dữ liệu hình ảnh từ colorFrame vào WriteableBitmap
                    colorBitmap.WritePixels(
                        new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                        colorFrame.GetRawPixelData(),
                        colorBitmap.PixelWidth * colorFrame.BytesPerPixel, 0
                    );
                }
            }
        }

        // Trong MainWindow
        public static MainWindow Instance { get; private set; }


        // Since the timer resolution defaults to about 10ms precisely, we need to
        // increase the resolution to get framerates above between 50fps with any
        // consistency.
        [DllImport("Winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(uint period);


        private void RestoreWindowState()
        {
            // Restore window state to that last used
            Rect bounds = Properties.Settings.Default.PrevWinPosition;
            if (bounds.Right != bounds.Left)
            {
                this.Top = bounds.Top;
                this.Left = bounds.Left;
                this.Height = bounds.Height;
                this.Width = bounds.Width;
            }

            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {

            this.setup();

            SensorChooser.KinectSensorChanged += this.SensorChooserKinectSensorChanged;

            /*this.popSound.Stream = Properties.Resources.Pop_5;
            this.hitSound.Stream = Properties.Resources.Hit_2;
            this.squeezeSound.Stream = Properties.Resources.Squeeze;

            this.popSound.Play();*/

            TimeBeginPeriod(TimerResolution);
            var myGameThread = new Thread(this.GameThread);
            myGameThread.SetApartmentState(ApartmentState.STA);
            myGameThread.Start();

            this.setup();

            SensorChooser.KinectSensorChanged += this.SensorChooserKinectSensorChanged;

            // Gán giá trị cho Instance
            Instance = this;

            //FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Shapes!");
        }

        public void setup()
        {
            this.myViewport3D.Width = 800;
            this.myViewport3D.Height = 600;
            this.models = new Model3DGroup();
            GeometryModel3D myGeometryModel = new GeometryModel3D();
            ModelVisual3D myModelVisual3D = new ModelVisual3D();
            // Defines the camera used to view the 3D object. In order to view the 3D object,
            // the camera must be positioned and pointed such that the object is within view 
            // of the camera.
            PerspectiveCamera myPCamera = new PerspectiveCamera();

            // Specify where in the 3D scene the camera is.
            myPCamera.Position = new Point3D(0, 0, 0);

            // Specify the direction that the camera is pointing.
            myPCamera.LookDirection = new Vector3D(0, 0, 1);
            myPCamera.UpDirection = new Vector3D(0, 1, 0);
            // Define camera's horizontal field of view in degrees.
            myPCamera.FieldOfView = 90;

            // Asign the camera to the viewport
            myViewport3D.Camera = myPCamera;
            // Define the lights cast in the scene. Without light, the 3D object cannot 
            // be seen. Note: to illuminate an object from additional directions, create 
            // additional lights.
            DirectionalLight myDirectionalLight = new DirectionalLight();
            myDirectionalLight.Color = Colors.White;
            myDirectionalLight.Direction = new Vector3D(-0.61, -0.5, -0.61);
            models.Children.Add(myDirectionalLight);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            this.runningGameThread = false;
            Properties.Settings.Default.PrevWinPosition = this.RestoreBounds;
            Properties.Settings.Default.WindowState = (int)this.WindowState;
            Properties.Settings.Default.Save();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            SensorChooser.Kinect = null;
        }

        #endregion ctor + Window Events

        #region Kinect discovery + setup

        private void SensorChooserKinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                this.UninitializeKinectServices((KinectSensor)e.OldValue);
            }

            // Only enable this checkbox if we have a sensor
            //enableAec.IsEnabled = e.NewValue != null;

            if (e.NewValue != null)
            {
                this.InitializeKinectServices((KinectSensor)e.NewValue);
            }
        }

        // Kinect enabled apps should customize which Kinect services it initializes here.
        private KinectSensor InitializeKinectServices(KinectSensor sensor)
        {
            // Application should enable all streams first.
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            sensor.SkeletonFrameReady += this.SkeletonsReady;
            sensor.SkeletonStream.Enable(new TransformSmoothParameters()
                {
                    Smoothing = 0.5f,
                    Correction = 0.5f,
                    Prediction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                });

            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                SensorChooser.AppConflictOccurred();
                return null;
            }

            // Start speech recognizer after KinectSensor.Start() is called
            // returns null if problem with speech prereqs or instantiation.
            /*this.mySpeechRecognizer = SpeechRecognizer.Create();
            this.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            this.mySpeechRecognizer.Start(sensor.AudioSource);*/
            //enableAec.Visibility = Visibility.Visible;
            //this.UpdateEchoCancellation(this.enableAec);

            return sensor;
        }

        // Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.Stop();

            sensor.SkeletonFrameReady -= this.SkeletonsReady;

            /*if (this.mySpeechRecognizer != null)
            {
                this.mySpeechRecognizer.Stop();
                this.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
                this.mySpeechRecognizer.Dispose();
                this.mySpeechRecognizer = null;
            }*/

        }

        #endregion Kinect discovery + setup

        #region Kinect Skeleton processing
        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    int skeletonSlot = 0;

                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                    foreach (Skeleton skeleton in this.skeletonData)
                    {
                        if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                        {
                            Player player;
                            if (this.players.ContainsKey(skeletonSlot))
                            {
                                player = this.players[skeletonSlot];
                            }
                            else
                            {
                                player = new Player(skeletonSlot, myViewport3D);
                                player.SetBounds(this.playerBounds);
                                this.players.Add(skeletonSlot, player);
                            }

                            player.LastUpdated = DateTime.Now;

                            // Update player's bone and joint positions
                            if (skeleton.Joints.Count > 0)
                            {
                                player.IsAlive = true;

                                player.UpdateAllJoints(skeleton.Joints);
                            }
                        }

                        skeletonSlot++;
                    }
                }
            }
        }

        private void CheckPlayers()
        {
            foreach (var player in this.players)
            {
                if (!player.Value.IsAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    this.players.Remove(player.Value.GetId());
                    break;
                }
            }

            // Count alive players
            int alive = this.players.Count(player => player.Value.IsAlive);
            
            if (alive != this.playersAlive)
            {

                if ((this.playersAlive == 0))// && (this.mySpeechRecognizer != null))
                {
                    /*BannerText.NewBanner(
                        Properties.Resources.Vocabulary,
                        this.screenRect,
                        true,
                        System.Windows.Media.Color.FromArgb(200, 255, 255, 255));*/
                }

                this.playersAlive = alive;
            }
        }

        private void PlayfieldSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //this.UpdatePlayfieldSize();
        }

        /*private void UpdatePlayfieldSize()
        {
            // Size of player wrt size of playfield, putting ourselves low on the screen.
            this.screenRect.X = 0;
            this.screenRect.Y = 0;
            this.screenRect.Width = this.playfield.ActualWidth;
            this.screenRect.Height = this.playfield.ActualHeight;

            BannerText.UpdateBounds(this.screenRect);

            this.playerBounds.X = 0;
            this.playerBounds.Width = this.playfield.ActualWidth;
            this.playerBounds.Y = this.playfield.ActualHeight * 0.2;
            this.playerBounds.Height = this.playfield.ActualHeight * 0.75;

            foreach (var player in this.players)
            {
                player.Value.SetBounds(this.playerBounds);
            }

        }*/
        #endregion Kinect Skeleton processing

        #region GameTimer/Thread
        private void GameThread()
        {
            this.runningGameThread = true;
            this.predNextFrame = DateTime.Now;
            this.actualFrameTime = 1000.0 / this.targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (this.runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (this.lastFrameDrawn == DateTime.MinValue)
                {
                    this.lastFrameDrawn = now;
                }

                double ms = now.Subtract(this.lastFrameDrawn).TotalMilliseconds;
                this.actualFrameTime = (this.actualFrameTime * 0.95) + (0.05 * ms);
                this.lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                this.frameCount++;
                if ((this.frameCount % 100 == 0) && (1000.0 / this.actualFrameTime < this.targetFramerate * 0.92))
                {
                    this.targetFramerate = Math.Max(MinFramerate, (this.targetFramerate + (1000.0 / this.actualFrameTime)) / 2);
                }

                if (now > this.predNextFrame)
                {
                    this.predNextFrame = now;
                }
                else
                {
                    double milliseconds = this.predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                    {
                        Thread.Sleep((int)(milliseconds + 0.5));
                    }
                }

                this.predNextFrame += TimeSpan.FromMilliseconds(1000.0 / this.targetFramerate);

                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<int>(this.HandleGameTimer), 0);
            }
        }

        private void HandleGameTimer(int param)
        {

            foreach (var player in this.players)
            {
                player.Value.Draw();
            }

            this.CheckPlayers();
        }
        #endregion GameTimer/Thread

       

        private void EnableAecChecked(object sender, RoutedEventArgs e)
        {
            CheckBox enableAecCheckBox = (CheckBox)sender;
            this.UpdateEchoCancellation(enableAecCheckBox);
        }

        private void UpdateEchoCancellation(CheckBox aecCheckBox)
        {
            /*
            this.mySpeechRecognizer.EchoCancellationMode = aecCheckBox.IsChecked != null && aecCheckBox.IsChecked.Value
                ? EchoCancellationMode.CancellationAndSuppression
                : EchoCancellationMode.None;*/
        }

        // Trong MainWindow
        public void UpdateDistanceText(string status, double distance)
        {
            // Chắc chắn rằng bạn có một TextBlock có tên "distanceTextBlock" trong XAML
            if (distanceTextBlock != null)
            {
                // Cập nhật giá trị khoảng cách và trạng thái (ngồi/đứng)
                distanceTextBlock.Text = $"Distance: {distance:F2} meters\nStatus: {status}";
            }
        }
        public void UpdateDistanceTextFromBodyToCamera( double distance)
        {
            // Hiển thị khoảng cách từ điểm đầu của người đến tâm của camera
            distanceTextBox.Text = $"Distance: {distance:F2} meters";
        }
        public void CheckHandAccelerationText(string direction)
        {
            //Hiển thị ra Action tương ứng với tay
            CheckHandAcceleration.Text= $"Action :{direction}" ;
        }


    }
}
