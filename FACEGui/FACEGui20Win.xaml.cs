using System;
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
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Globalization;
using System.Collections;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using System.Timers;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Configuration;

using FACEBodyControl;
using ControllersLibrary;
using StatusMonitor;
using FACEConfig;

using FACELibrary;
using YarpManagerCS;

namespace FACEGui20
{
    public enum FaceTrackingControls { EyeTurnLeft = 11, EyesUpDown = 13, UpperNod = 14, Turn = 28, EyeTurnRight = 30, Tilt = 31 };
    public enum BlinkingControls { EyeLidsLower = 21, EyeLidsUpper = 22 };

    /// <summary>
    /// View: send expression
    /// Edit: create new expression
    /// Config: set max/min --> OLD
    /// Net: UDP or YARP connection
    /// ECS: ECS model
    /// Gamepad: control robot with joystick
    /// </summary>
    public enum Mode { View, Edit, Net, ECS, Gamepad, Test };

    //public enum FACEActions
    //{
    //    None = -1, Neutral = 0, Happiness = 1, Anger = 2, Fear = 3, Sadness = 4, Disgust = 5, Amazement = 6, Reset = 7,
    //    StartBlinking = 8, StopBlinking = 9, StartFaceTracking = 10, StopFaceTracking = 11, Yes = 12, No = 13, Afraid = 14, Surprise = 15
    //};
    public enum FACEActions
    {
        None = -1, Anger = 1, Disgust = 2, Fear = 3, Happiness = 4, Neutral = 5, Sadness = 6, Surprise = 7, Reset = 8
    };

    /// <summary>
    /// Interaction logic for FACEGui20Win.xaml
    /// </summary>
    public partial class FACEGui20Win : Window
    {
        private string motionsPath = @"Motions\";
        private string expressionsPath = @"Expressions\";
        private string animationsPath = @"Animations\";
        private string logsPath = @"Logs\";
        private string testPath = @"Expressions\";
        private string NewSetExpressionPath = @"NewSetExpressions\";

        private List<ServoMotor> currentSmState;
        private List<FACEMotion> ecsMotions;

        private Color grayColor;
        private String lastOpenFilename;
        private int expressionTime;
        private int NeckTime;

        private Mode visualMode;


        private SettingsDialog setDialog;
        private SliderController[] sliders;
        private double startTimeUI; //time for log
        private Dictionary<KeyGesture, RoutedEventHandler> gests = new Dictionary<KeyGesture, RoutedEventHandler>();

        /* Saved indexes for comboboxes */
        private string savedDeviceType = "SSC32";
        private string savedPortName = "COM5";
        private int savedBitRate = 115200;
        private int savedDataBits = 8;
        private Parity savedParity = Parity.None;
        private StopBits savedStopBits = StopBits.One;
        private Handshake savedHandshake = Handshake.None;
        private int savedWebcamId = -1;

        private delegate void UpdateProgressBarTimeDelegate(DependencyProperty dp, Object value);

        public static readonly RoutedEvent SetMinMaxButtonClickedEvent = EventManager.RegisterRoutedEvent("MinMaxButtonClicked", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(FACEGui20Win));

        public event RoutedEventHandler SetMinMaxButtonClicked
        {
            add { AddHandler(SetMinMaxButtonClickedEvent, value); }
            remove { RemoveHandler(SetMinMaxButtonClickedEvent, value); }
        }

        public delegate void DialogYesButtonEventHandler(object sender, EventArgs e);
        public delegate void DialogNoButtonEventHandler(object sender, EventArgs e);
        public delegate void DialogCancelButtonEventHandler(object sender, EventArgs e);

        //private List<FACExpression> ecsExpressions;
        private ECS ecs;
        private ECSWin ecsWin;

        private string colYarp;// Ellipse color Yarp
        private string colMot;// Ellipse color Yarp Set Motor
      

        private YarpPort yarpPortSetMotors;
        private YarpPort yarpPortFeedback;


        private string setmotors_in = ConfigurationManager.AppSettings["YarpPortSetMotors_IN"].ToString();
        private string setmotors_out = ConfigurationManager.AppSettings["YarpPortSetMotors_OUT"].ToString();
        private string feedback_out = ConfigurationManager.AppSettings["YarpPortfeedback_OUT"].ToString(); 


        string receivedSetMotors = "";

        private System.Timers.Timer yarpReceverSetMotorTimer;
        private System.Timers.Timer TimerCheckStatusYarp;
        private System.Threading.Thread senderThreadFeedBack = null;


        private string path_yarp = Environment.ExpandEnvironmentVariables(@"C:\Users\%USERNAME%\AppData\Roaming") + "\\yarp\\conf\\yarp.conf";

        public FACEGui20Win()
        {


           
            InitializeComponent();

            InitFACETool();           
            //InitLog();
        }


        #region Init

        /// <summary>
        /// Initialize FACEGui window
        /// </summary>
        private void InitFACETool()
        {
            visualMode = Mode.Edit;
            grayColor = Color.FromRgb(109, 109, 109);

            FACEBody.LoadConfigFile("Config.xml");
            currentSmState = FACEBody.CurrentMotorState;

            sliders = new SliderController[32];

            expressionTime = Convert.ToInt32(SBTimeBox.Text);
            NeckTime = Convert.ToInt32(SBTimeNeckBox.Text);
            InitViewMode();
            InitEditMode(RightSlidersPanel);
            InitEditMode(LeftSlidersPanel);
            InitECSMode();
            InitYarp();

            gests.Add(new KeyGesture(Key.T, ModifierKeys.Control), SettingsButton_Click);
            //FACEBody.VideoCamProblem += new EventHandler<WarningEventArgs>(Body_VideoCamProblem);
            //updatePbTimeDelegate = new UpdateProgressBarTimeDelegate(SBProgressBar.SetValue);

            Show();
        }


        /// <summary>
        /// Initialize View mode panel
        /// </summary>
        private void InitViewMode()
        {
            int index = Convert.ToInt32(EyeLidsLowerMB.Uid);
            string name = EyeLidsLowerMB.Name.Substring(0, EyeLidsLowerMB.Name.Length - 2);
            EyeLidsLowerMB.SliderLabel.Foreground = new SolidColorBrush(grayColor);
            EyeLidsLowerMB.SliderLabel.Content = name + " (" + index + ")";
            EyeLidsLowerMB.SliderCheckbox.IsChecked = false;
            EyeLidsLowerMB.SliderControl.Value = FACEBody.CurrentMotorState[index].PulseWidthNormalized;
            EyeLidsLowerMB.SliderValueChanged += new RoutedEventHandler(SliderCtlr_SliderValueChanged);
            EyeLidsLowerMB.CheckboxChecked += new RoutedEventHandler(SliderCtrl_CheckboxChecked);
            EyeLidsLowerMB.CheckboxUnchecked += new RoutedEventHandler(SliderCtrl_CheckboxUnchecked);

            index = Convert.ToInt32(EyeLidsUpperMB.Uid);
            name = EyeLidsLowerMB.Name.Substring(0, EyeLidsUpperMB.Name.Length - 2);
            EyeLidsUpperMB.SliderLabel.Foreground = new SolidColorBrush(grayColor);
            EyeLidsUpperMB.SliderLabel.Content = name + " (" + index + ")";
            EyeLidsUpperMB.SliderCheckbox.IsChecked = false;
            EyeLidsUpperMB.SliderControl.Value = FACEBody.CurrentMotorState[index].PulseWidthNormalized;
            EyeLidsUpperMB.SliderValueChanged += new RoutedEventHandler(SliderCtlr_SliderValueChanged);
            EyeLidsUpperMB.CheckboxChecked += new RoutedEventHandler(SliderCtrl_CheckboxChecked);
            EyeLidsUpperMB.CheckboxUnchecked += new RoutedEventHandler(SliderCtrl_CheckboxUnchecked);
        }


        /// <summary>
        /// Initialize Edit mode panel
        /// </summary>
        /// <param name="panel"></param>
        private void InitEditMode(StackPanel panel)
        {
            foreach (Control ctrl in panel.Children)
            {
                if (ctrl.GetType() == typeof(SliderController))
                {
                    SliderController sliderCtrl = ctrl as SliderController;

                    int index = Convert.ToInt32(sliderCtrl.Uid);
                    string name = sliderCtrl.Name.Substring(0, sliderCtrl.Name.Length - 2);

                    DockPanel dp = sliderCtrl.Content as DockPanel;
                    dp.Uid = index.ToString();
                    dp.Name = name + dp.Name;

                    CheckBox cb = dp.Children[0] as CheckBox;
                    if (!String.IsNullOrEmpty(FACEBody.CurrentMotorState[index].SerialSC))
                        cb.IsChecked = true;
                    else
                        cb.IsChecked = false;

                    StackPanel sp = dp.Children[1] as StackPanel;
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sp); i++)
                    {
                        Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, i);

                        if (childVisual.GetType() == typeof(Label))
                        {
                            Label label = childVisual as Label;
                            label.Uid = index.ToString();
                            label.Content = name + " (" + index + ")";
                        }
                        else if (childVisual.GetType() == typeof(Slider))
                        {
                            Slider slider = childVisual as Slider;
                            slider.Uid = index.ToString();
                            slider.Value = FACEBody.CurrentMotorState[index].PulseWidthNormalized;
                        }
                        else if (childVisual.GetType() == typeof(TextBox))
                        {
                            TextBox textbox = childVisual as TextBox;
                            textbox.Uid = index.ToString();
                        }
                    }

                    if(!String.IsNullOrEmpty(FACEBody.CurrentMotorState[index].SerialSC))
                        sp.IsEnabled = true;
                    else
                        sp.IsEnabled = false;
                    sliderCtrl.SliderValueChanged += new RoutedEventHandler(SliderCtlr_SliderValueChanged);
                    sliderCtrl.CheckboxChecked += new RoutedEventHandler(SliderCtrl_CheckboxChecked);
                    sliderCtrl.CheckboxUnchecked += new RoutedEventHandler(SliderCtrl_CheckboxUnchecked);
                    sliders[index] = sliderCtrl;
                }
            }
        }


        /// <summary>
        /// Initialize ECS mode panel
        /// </summary>
        private void InitECSMode()
        {
            ecs = ECS.LoadFromXmlFormat("ECS.xml");
            foreach (ECSMotor ecsM in ecs.ECSMotorList)
            {
                ecsM.FillMap();
            }

            ecsMotions = new List<FACEMotion>();
            DirectoryInfo dir = new DirectoryInfo(expressionsPath);
            foreach (FileInfo fi in dir.GetFiles("*.xml"))
            {
                FACEMotion f = FACEMotion.LoadFromXmlFormat(expressionsPath + fi.Name);
                ecsMotions.Add(f);
            }
        }


        /// <summary>
        /// Initialize Timer for Test Yarp
        /// </summary>
        /// <param name="panel"></param>
        private void InitYarp()
        {
          

            yarpPortSetMotors = new YarpPort();
            yarpPortSetMotors.openReceiver(setmotors_out, setmotors_in);

            yarpPortFeedback = new YarpPort();
            yarpPortFeedback.openSender(feedback_out);

            // controllo se la connessione con le porte sono attive(unico metodo funzionante)
            colYarp = "red";
            colMot = "red";

            TimerCheckStatusYarp = new System.Timers.Timer();
            TimerCheckStatusYarp.Elapsed += new ElapsedEventHandler(CheckStatusYarp);
            TimerCheckStatusYarp.Interval = (1000) * (5);
            TimerCheckStatusYarp.Enabled = true;
            TimerCheckStatusYarp.Start();

            senderThreadFeedBack = new System.Threading.Thread(SendFeedBack);
            senderThreadFeedBack.Start();

            yarpReceverSetMotorTimer = new System.Timers.Timer();
            yarpReceverSetMotorTimer.Interval = 30;
            //yarpReceverSetMotorTimer.Elapsed += new ElapsedEventHandler(ReceiveDataSetMotors);
            //yarpReceverSetMotorTimer.Start();

            ThreadPool.QueueUserWorkItem(ReceiveDataSetMotors);

       
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

           // OpenClosePort();

            if (AnimationEngine.Running)
            {
                AnimationEngine.StopAnimation();
            }

            if (ecsWin != null)
                ecsWin.Close();

            if (gamepadWin != null)
                gamepadWin.Close();

            if (statusWin != null)
                statusWin.Close();

            YarpDisconnect();

            

            Close();
        }


        #endregion


        #region Logging

        private void InitLog()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd", new CultureInfo("it-IT", false).DateTimeFormat);
            string time = String.Format("{0:HH.mm.ss}", DateTime.Now);
            logsPath += "log_" + date + "_" + time + ".xml";

            if (!File.Exists(logsPath))
            {
                XDocument xDoc = new XDocument(new XDeclaration("1.0", System.Text.Encoding.UTF8.WebName, "yes"));

                XElement xRoot = new XElement(XName.Get("Session"));
                xRoot.SetAttributeValue("Date", date);
                DateTime dt = DateTime.Now;
                xRoot.SetAttributeValue("Time", String.Format("{0:HH:mm:ss.ffff}", dt));
                xRoot.SetAttributeValue("Timestamp", String.Format("{0:0.000000}", FromDateToDouble(dt)).Replace(",", "."));
                xDoc.Add(xRoot);

                startTimeUI = FromDateToDouble(dt);

                XElement enums = new XElement(XName.Get("Enums"));
                foreach (FACEActions item in Enum.GetValues(typeof(FACEActions)))
                {
                    XElement xmlElem = new XElement("FACEActions");
                    xmlElem.SetAttributeValue("Type", Enum.GetName(typeof(FACEActions), item));
                    xmlElem.SetAttributeValue("Value", (int)item);
                    enums.Add(xmlElem);
                }
                xRoot.Add(enums);

                XElement actions = new XElement(XName.Get("Actions"));
                xRoot.Add(actions);

                xDoc.Save(logsPath);
            }
            else
            {
                XDocument xDoc = XDocument.Load(logsPath);
                startTimeUI = Double.Parse(xDoc.Root.Attribute(XName.Get("Timestamp")).Value.Replace(".", ","));
            }
        }

        private void LogEvents(FACEActions actionType)
        {
            try
            {
                XDocument xDoc = XDocument.Load(logsPath);

                XElement xmlElem = new XElement("Action");
                xmlElem.SetAttributeValue("Type", actionType);
                xmlElem.SetAttributeValue("Value", (int)actionType);
                xmlElem.SetAttributeValue("Delta", String.Format("{0:0}", (FromDateToDouble(DateTime.Now) - startTimeUI) * Math.Pow(10, 3)).Replace(",", "."));
                xmlElem.SetAttributeValue("Timestamp", String.Format("{0:0.000000}", FromDateToDouble(DateTime.Now)).Replace(",", "."));

                xDoc.Root.Element("Actions").Add(xmlElem);
                xDoc.Save(logsPath);
            }
            catch
            {
                ErrorDialog errDialog = new ErrorDialog();
                errDialog.tbInstructionText.Text = "Some problems occurred writing the log file.";
                errDialog.Show();
            }
        }

        private double FromDateToDouble(DateTime time)
        {
            DateTime dTime = time - TimeZone.CurrentTimeZone.GetUtcOffset(time);
            TimeSpan span = dTime - new DateTime(1970, 1, 1);
            return Math.Round((span.Ticks / 10) * Math.Pow(10, -6), 4);
        }

        private DateTime FromDoubleToDate(double time)
        {
            TimeSpan v = new TimeSpan((long)((time * 10) / Math.Pow(10, -6)));
            DateTime res = new DateTime(1970, 1, 1) + v; //ora legale di Greenwich
            TimeSpan currentOffset = TimeZone.CurrentTimeZone.GetUtcOffset(res);
            return res + currentOffset;
        }



        //// Log for Win7 into registry
        //private void InitLogOnRegistry()
        //{            
        //    //if (EventLog.SourceExists("FACETool"))
        //    //{
        //    //    //An event log source should not be created and immediately used.
        //    //    //There is a latency time to enable the source, it should be created
        //    //    //prior to executing the application that uses the source.
        //    //    //Execute this sample a second time to use the new source.
        //    //    //EventLog.CreateEventSource("FACETool", "FACETool_Log");
        //    //    EventLog.DeleteEventSource("FACETool");
        //    //    EventLog.Delete("FACETool_Log");
        //    //}

        //    // Create an EventLog instance and assign its source.
        //    //EventLog myLog = new EventLog();
        //    //myLog.Source = "FACETool";

        //    // Write an informational entry to the event log.    
        //    EventLog.WriteEntry("FACETool", "Writing to event log.");

        //    EventLog[] logs = EventLog.GetEventLogs();
        //    EventLog selected = null;
        //    foreach (EventLog l in logs)
        //    {
        //        if (l.Log == "Application")
        //            selected = l;
        //    }

        //    List<string> messages = new List<string>();
        //    foreach (EventLogEntry entry in selected.Entries)
        //    {
        //        if (entry.Source == "FACETool")
        //            messages.Add(entry.Message);
        //    }

        //    //// To delete an entry in the Event Viewer list
        //    //string logName = "";
        //    //if (EventLog.SourceExists("FACETool_Log"))
        //    //{
        //    //    // Find the log associated with this source.    
        //    //    logName = EventLog.LogNameFromSourceName("FACETool_Log", ".");
        //    //    // Delete the source and the log.
        //    //    //EventLog.DeleteEventSource("FACETool_Log");
        //    //    EventLog.Delete(logName);
        //    //}
        //}

        //private void LogEventsOnRegistry(FACEActions actionType)
        //{
        //    //EventLog.WriteEntry("FACETool", fileName.Substring(0, fileName.Length - 4));
        //}

        #endregion


        #region Utilities for UI design

        protected override void OnPreviewKeyDown(KeyEventArgs args)
        {
            foreach (KeyGesture gest in gests.Keys)
            {
                if (gest.Matches(null, args))
                {
                    gests[gest](this, args);
                    args.Handled = true;
                }
            }
        }

        #endregion


        #region Settings Dialog

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            setDialog = new SettingsDialog();

            setDialog.UIPortSettings += new SettingsDialog.UIPortSettingsEventHandler(SaveAndDisplayCurrentPortSettings);
            setDialog.UIMessage += new SettingsDialog.UIMessageEventHandler(DisplayCurrentMessage);

            setDialog.UIDeviceSettings += new SettingsDialog.UIDeviceSettingsEventHandler(SaveAndDisplayCurrentDeviceSettings);
            setDialog.UIWebcamSettings += new SettingsDialog.UIWebcamSettingsEventHandler(SaveAndDisplayCurrentWebcamSettings);

            SetPreferences();
            setDialog.ShowDialog();
        }

        /// <summary>
        /// Set the saved settings on the combo boxes.
        /// </summary>
        private void SetPreferences()
        {
            setDialog.standardCombo.SelectedValue = savedDeviceType;
            setDialog.comPortCombo.SelectedValue = savedPortName;
            setDialog.bitRateCombo.SelectedValue = savedBitRate;
            setDialog.dataBitsCombo.SelectedValue = savedDataBits;
            setDialog.parityCombo.SelectedValue = savedParity;
            setDialog.stopBitsCombo.SelectedValue = savedStopBits;
            setDialog.handshakingCombo.SelectedValue = savedHandshake;
            setDialog.webcamCombo.SelectedIndex = savedWebcamId;
        }

        #endregion


        #region Statusbar

        /// <summary>
        /// Display the current device settings on the statusbar.
        /// </summary>
        /// <param name="selectedDeviceType"></param>
        private void SaveAndDisplayCurrentDeviceSettings(string selectedDeviceType)
        {
            if (savedDeviceType != selectedDeviceType)
            {
                savedDeviceType = selectedDeviceType;
            }
        }


        /// <summary>
        /// Display the current port settings on the statusbar.
        /// </summary>
        /// <param name="selectedPort"></param>
        /// <param name="selectedBitRate"></param>
        /// <param name="selectedDataBits"></param>
        /// <param name="selectedParity"></param>
        /// <param name="selectedStopBits"></param>
        /// <param name="selectedHandshake"></param>        
        private void SaveAndDisplayCurrentPortSettings(string selectedPort, int selectedBitRate, int selectedDataBits, Parity selectedParity, StopBits selectedStopBits, Handshake selectedHandshake)
        {
            int start = selectedPort.IndexOf("(");
            int end = selectedPort.IndexOf(")");
            string result = selectedPort.Substring(start, end - start);
            result = result.Replace("(", "");

            savedPortName = result;
            savedBitRate = (int)setDialog.bitRateCombo.SelectedItem;
            savedDataBits = (int)setDialog.dataBitsCombo.SelectedItem;
            savedParity = (Parity)setDialog.parityCombo.SelectedItem;
            savedStopBits = (StopBits)setDialog.stopBitsCombo.SelectedItem;
            savedHandshake = (Handshake)setDialog.handshakingCombo.SelectedItem;

            //TextNamePort.Text = result;

            //if (selectedPort != "")
            //{
            //    if (ComPorts.SelectedPort.IsOpen)
            //    {
            //        TextStatusPort.Text = "Opened";
            //    }
            //    else
            //    {
            //        TextStatusPort.Text = "Closed";
            //    }

            //    TextBitrate.Text = Convert.ToString(selectedBitRate);
            //}
            //else
            //{
            //    TextStatusPort.Text = "";
            //    TextBitrate.Text = "";
            //}
        }


        /// <summary>
        /// Display the current webcam settings on the statusbar.
        /// </summary>
        /// <param name="webcamId"></param>
        private void SaveAndDisplayCurrentWebcamSettings(int webcamId)
        {
            if (savedWebcamId != webcamId)
            {
                savedWebcamId = webcamId;
            }
        }


        /// <summary>
        /// Display the current message on the statusbar.
        /// </summary>
        /// <param name="msg">The message to be set on the statusbar</param>
        private void DisplayCurrentMessage(string msg)
        {
            TextError.Text = msg;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenClosePort_Click(object sender, RoutedEventArgs e)
        {
            //OpenClosePort();
            //if (ComPorts.IsSelected)
            //{
            //    if (ComPorts.OpenedPort)
            //    {
            //        FACEMotion motion = FACEMotion.LoadFromXmlFormat(motionsPath + "AU_Neutral.xml");
            //        // FACEMotion motionToTest = new FACEMotion(FACEBody.CurrentMotorState.Count);
            //        motion.Duration = 120;
            //        motion.Priority = 10;
            //        FACEBody.ExecuteMotion(motion);

            //        System.Threading.Thread.Sleep(1000); //cosi tutte le comicazioni con la porta sono finite

            //        ComPorts.CloseComPort();
            //        System.Windows.Controls.Image img = OnOffButton.Content as System.Windows.Controls.Image;
            //        img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Statusbar/Off-30.png")));
            //        CheckboxYarpExp.IsChecked = false;
            //        CheckboxAnimator.IsChecked = false;
            //        CheckboxYarpExp.IsEnabled = false;
            //        CheckboxAnimator.IsEnabled = false;
            //        TextStatusPort.Text = "Closed";
            //    }
            //    else
            //    {
            //        //FACEMotion motion = FACEMotion.LoadFromXmlFormat(motionsPath+"ConfigOut.xml");
            //        ////FACEMotion motionToTest = new FACEMotion(FACEBody.CurrentMotorState.Count);
            //        //motion.Duration = lookAtDuration;
            //        //motion.Priority = 10;
            //        //FACEBody.ExecuteMotion(motion);

            //        ComPorts.OpenComPort();
            //        System.Windows.Controls.Image img = OnOffButton.Content as System.Windows.Controls.Image;
            //        img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Statusbar/On-30.png")));
            //        CheckboxYarpExp.IsEnabled = true;
            //        CheckboxAnimator.IsEnabled = true;

            //        TextStatusPort.Text = "Opened";
            //    }
            //}
            //else
            //{
            //    WarningDialog warningDialog = new WarningDialog();
            //    warningDialog.tbInstructionText.Text = "There are not opened ports!";
            //    warningDialog.Show();
            //}
        }

        //private void OpenClosePort() 
        //{
        //    if (ComPorts.IsSelected)
        //    {
        //        if (ComPorts.OpenedPort)
        //        {
        //            FACEMotion motion = FACEMotion.LoadFromXmlFormat(motionsPath + "AU_Neutral.xml");
        //            // FACEMotion motionToTest = new FACEMotion(FACEBody.CurrentMotorState.Count);
        //            motion.Duration = 1000;
        //            motion.Priority = 10;
        //            FACEBody.ExecuteMotion(motion);

        //            System.Threading.Thread.Sleep(3000); //cosi tutte le comicazioni con la porta sono finite

        //            ComPorts.CloseComPort();
        //            System.Windows.Controls.Image img = OnOffButton.Content as System.Windows.Controls.Image;
        //            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Statusbar/Off-30.png")));
        //            TextStatusPort.Text = "Closed";
        //        }
        //        else
        //        {
        //            //FACEMotion motion = FACEMotion.LoadFromXmlFormat(motionsPath+"ConfigOut.xml");
        //            ////FACEMotion motionToTest = new FACEMotion(FACEBody.CurrentMotorState.Count);
        //            //motion.Duration = lookAtDuration;
        //            //motion.Priority = 10;
        //            //FACEBody.ExecuteMotion(motion);

        //            ComPorts.OpenComPort();
        //            System.Windows.Controls.Image img = OnOffButton.Content as System.Windows.Controls.Image;
        //            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Statusbar/On-30.png")));
        //            CheckboxYarpExp.IsEnabled = true;
        //            CheckboxAnimator.IsEnabled = true;

        //            TextStatusPort.Text = "Opened";
        //        }
        //    }
        //    else
        //    {
        //        WarningDialog warningDialog = new WarningDialog();
        //        warningDialog.tbInstructionText.Text = "There are not opened ports!";
        //        warningDialog.Show();
        //    }
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SBTimeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                expressionTime = Convert.ToInt32(SBTimeBox.Text, NumberFormatInfo.InvariantInfo);
                if (expressionTime < 0)
                {
                    expressionTime = 0;
                    SBTimeBox.Text = String.Format(expressionTime.ToString("", CultureInfo.InvariantCulture));
                }
            }
            catch
            {
                expressionTime = 0;
                SBTimeBox.Text = String.Format(expressionTime.ToString("0", CultureInfo.InvariantCulture));
            }
        }


        private void SBTimeBoxNeck_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                NeckTime = Convert.ToInt32(SBTimeNeckBox.Text, NumberFormatInfo.InvariantInfo);
                if (NeckTime < 0)
                {
                    NeckTime = 0;
                    SBTimeNeckBox.Text = String.Format(NeckTime.ToString("", CultureInfo.InvariantCulture));
                }
            }
            catch
            {
                NeckTime = 0;
                SBTimeNeckBox.Text = String.Format(NeckTime.ToString("0", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxTime"></param>
        private void StartProgressbarTime(double maxTime)
        {
            //Configure the ProgressBar
            //SBProgressBar.Minimum = 0;
            //SBProgressBar.Maximum = maxTime;
            //SBProgressBar.Value = 0;

            ////Stores the value of the ProgressBar
            //double timeValue = 0;

            //// Create a new instance of our ProgressBar Delegate that points
            //// to the ProgressBar's SetValue method.
            //updatePbTimeDelegate = new UpdateProgressBarTimeDelegate(SBProgressBar.SetValue);

            ////Tight Loop:  Loop until the ProgressBar.Value reaches the max
            //do
            //{
            //    timeValue += 1;

            //    /*Update the Value of the ProgressBar:
            //      1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
            //      2)  Set the DispatcherPriority to "Background"
            //      3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
            //    Dispatcher.Invoke(updatePbTimeDelegate,
            //        System.Windows.Threading.DispatcherPriority.Background,
            //        new object[] { ProgressBar.ValueProperty, timeValue });
            //}
            //while (SBProgressBar.Value != SBProgressBar.Maximum);

            //SBProgressBar.Value = SBProgressBar.Minimum;
            SBInfoBox.Text = "";
            TextError.Text = "";
        }



        #endregion


        #region Toolbar button

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            switch (visualMode)
            {
                case Mode.View:
                    break;

                case Mode.Edit:
                    try
                    {
                        foreach (SliderController sliderCtrl in sliders)
                        {
                            DockPanel dp = sliderCtrl.Content as DockPanel;
                            int index = Int32.Parse(dp.Uid);
                            StackPanel sp = dp.Children[1] as StackPanel;
                            //string name = Enum.GetName(typeof(NameControls), index);

                            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sp); i++)
                            {
                                Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, i);
                                if (childVisual.GetType() == typeof(Slider))
                                {
                                    (childVisual as Slider).Value = FACEBody.CurrentMotorState[index].PulseWidthNormalized;
                                }
                            }
                            sp.IsEnabled = false;
                        }

                        PleasureTextbox.Text = "0.000";
                        ArousalTextbox.Text = "0.000";
                        DominanceTextbox.Text = "0.000";
                        NameTextbox.Text = "ExpressionName";
                    }
                    catch (FACException fEx)
                    {
                        TextError.Text = "Error occurs opening a new file";
                        ErrorDialog errorDiag = new ErrorDialog();
                        errorDiag.tbInstructionText.Text = fEx.Message;
                        errorDiag.Show();
                    }
                    break;

                case Mode.Net:
                    break;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            loading = true;

            Microsoft.Win32.OpenFileDialog dlg;
            dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = expressionsPath;
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML file (*.xml)|*.xml"; // Filter files by extension
            dlg.RestoreDirectory = true;

            Nullable<bool> result;
            result = dlg.ShowDialog();

            if (result == true)
            {
                lastOpenFilename = dlg.FileName;
                FACEMotion motion = FACEMotion.LoadFromXmlFormat(lastOpenFilename);

                switch (visualMode)
                {
                    case Mode.View:
                        break;

                    case Mode.Edit:
                        try
                        {
                            float defValue = 0;
                            for (int i = 0; i < motion.ServoMotorsList.Count; i++)
                            {
                                defValue = motion.ServoMotorsList[i].PulseWidthNormalized;

                                DockPanel dp = sliders[i].Content as DockPanel;
                                int index = Int32.Parse(dp.Uid);
                                StackPanel sp = dp.Children[1] as StackPanel;

                                for (int k = 0; k < VisualTreeHelper.GetChildrenCount(sp); k++)
                                {
                                    Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, k);
                                    if (childVisual.GetType() == typeof(Slider))
                                    {
                                        if (defValue != -1)
                                        {
                                            (dp.Children[0] as CheckBox).IsChecked = true;
                                            (childVisual as Slider).Value = defValue;
                                        }
                                        else
                                        {
                                            (childVisual as Slider).Value = FACEBody.DefaultMotorState[index].PulseWidthNormalized;
                                            (dp.Children[0] as CheckBox).IsChecked = false;
                                        }
                                    }
                                }
                            }
                            PleasureTextbox.Text = motion.ECSCoord.Pleasure.ToString();
                            ArousalTextbox.Text = motion.ECSCoord.Arousal.ToString();
                            DominanceTextbox.Text = motion.ECSCoord.Dominance.ToString();
                            NameTextbox.Text = motion.Name;
                        }
                        catch (FACException fEx)
                        {
                            TextError.Text = "Error occurs loading " + dlg.FileName.ToString().Remove(dlg.FileName.Length - 4) + " file.";
                            ErrorDialog errorDiag = new ErrorDialog();
                            errorDiag.tbInstructionText.Text = fEx.Message;
                            errorDiag.Show();
                        }
                        break;
                }
                loading = false;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FACEMotion motionToSave = new FACEMotion(FACEBody.CurrentMotorState.Count);
            motionToSave.Duration = expressionTime;
            motionToSave.DelayTime = 0;
            motionToSave.ECSCoord = new ECS.ECSCoordinate(Single.Parse(PleasureTextbox.Text), Single.Parse(ArousalTextbox.Text), Single.Parse(DominanceTextbox.Text));
            motionToSave.Name = NameTextbox.Text;
            motionToSave.Priority = 10;

            switch (visualMode)
            {
                case Mode.View:
                    break;

                case Mode.Edit:
                    foreach (SliderController sliderCtrl in sliders)
                    {
                        DockPanel dp = sliderCtrl.Content as DockPanel;
                        int index = Int32.Parse(dp.Uid);
                        CheckBox cb = dp.Children[0] as CheckBox;
                        StackPanel sp = dp.Children[1] as StackPanel;

                        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sp); i++)
                        {
                            Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, i);
                            if (childVisual.GetType() == typeof(TextBox))
                            {
                                if ((bool)cb.IsChecked)
                                {
                                    float newValue = Convert.ToSingle((childVisual as TextBox).Text, NumberFormatInfo.InvariantInfo);
                                    motionToSave.ServoMotorsList.ElementAt(index).PulseWidthNormalized = newValue;
                                }
                                else
                                {
                                    motionToSave.ServoMotorsList.ElementAt(index).PulseWidthNormalized = -1;
                                }
                            }
                        }
                    }
                    break;
            }

            try
            {
                FACEMotion.SaveAsXmlFormat(motionToSave, lastOpenFilename);
            }
            catch (FACException fEx)
            {
                TextError.Text = "Error occurs saving " + lastOpenFilename.ToString().Remove(lastOpenFilename.Length - 4) + " expression..";
                ErrorDialog errorDiag = new ErrorDialog();
                errorDiag.tbInstructionText.Text = fEx.Message;
                errorDiag.Show();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.FileName = "Expression"; // Default file name
            saveDialog.DefaultExt = ".xml";
            saveDialog.Filter = "XML file (*.xml)|*.xml"; // Filter files by extension "All Files (*.*)|*.*|XML file (*.xml)|*.xml"
            saveDialog.AddExtension = true; // Adds a extension if the user does not
            saveDialog.InitialDirectory = expressionsPath;
            saveDialog.RestoreDirectory = true;

            Nullable<bool> result = saveDialog.ShowDialog();

            FACEMotion motionToSave = new FACEMotion(FACEBody.CurrentMotorState.Count);
            motionToSave.Duration = expressionTime;
            motionToSave.DelayTime = 0;
            motionToSave.ECSCoord = new ECS.ECSCoordinate(Single.Parse(PleasureTextbox.Text), Single.Parse(ArousalTextbox.Text), Single.Parse(DominanceTextbox.Text));
            motionToSave.Name = NameTextbox.Text;
            motionToSave.Priority = 10;

            if (result == true)
            {
                string filename = saveDialog.FileName;

                switch (visualMode)
                {
                    case Mode.View:
                        break;

                    case Mode.Edit:
                        foreach (SliderController sliderCtrl in sliders)
                        {
                            DockPanel dp = sliderCtrl.Content as DockPanel;
                            int index = Int32.Parse(dp.Uid);
                            CheckBox cb = dp.Children[0] as CheckBox;
                            StackPanel sp = dp.Children[1] as StackPanel;

                            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sp); i++)
                            {
                                Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, i);
                                if (childVisual.GetType() == typeof(TextBox))
                                {
                                    if ((bool)cb.IsChecked)
                                    {
                                        float newValue = Convert.ToSingle((childVisual as TextBox).Text, NumberFormatInfo.InvariantInfo);
                                        motionToSave.ServoMotorsList.ElementAt(index).PulseWidthNormalized = newValue;
                                    }
                                    else
                                    {
                                        motionToSave.ServoMotorsList.ElementAt(index).PulseWidthNormalized = -1;
                                    }
                                }
                            }
                        }
                        break;
                }

                try
                {
                    motionToSave.Name = (filename.Split(new Char[] { '.' }))[0];
                    FACEMotion.SaveAsXmlFormat(motionToSave, filename);
                }
                catch (FACException fEx)
                {
                    TextError.Text = "Error occurs saving " + filename.ToString().Remove(filename.Length - 4) + " expression..";
                    ErrorDialog errorDiag = new ErrorDialog();
                    errorDiag.tbInstructionText.Text = fEx.Message;
                    errorDiag.Show();
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestExpressionButton_Click(object sender, RoutedEventArgs e)
        {
            FACEMotion motionToTest = new FACEMotion(FACEBody.CurrentMotorState.Count);
            motionToTest.Duration = expressionTime;
            motionToTest.DelayTime = 0;
            motionToTest.ECSCoord = new ECS.ECSCoordinate(Single.Parse(PleasureTextbox.Text), Single.Parse(ArousalTextbox.Text), Single.Parse(DominanceTextbox.Text));
            motionToTest.Name = NameTextbox.Text;
            motionToTest.Priority = 10;

            switch (visualMode)
            {
                case Mode.View:
                    break;

                case Mode.Edit:
                    foreach (SliderController sliderCtrl in sliders)
                    {
                        DockPanel dp = sliderCtrl.Content as DockPanel;
                        int index = Int32.Parse(dp.Uid);
                        CheckBox cb = dp.Children[0] as CheckBox;

                        if ((bool)cb.IsChecked)
                        {
                            StackPanel sp = dp.Children[1] as StackPanel;
                            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sp); i++)
                            {
                                Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, i);
                                if (childVisual.GetType() == typeof(TextBox))
                                {
                                    float newValue = Convert.ToSingle((childVisual as TextBox).Text, NumberFormatInfo.InvariantInfo);
                                    motionToTest.ServoMotorsList[index].PulseWidthNormalized = newValue;
                                }
                            }
                        }
                        else
                        {
                            motionToTest.ServoMotorsList[index].PulseWidthNormalized = -1;
                        }
                    }
                    break;

                case Mode.Net:
                    break;
            }

            try
            {
                FACEBody.ExecuteMotion(motionToTest);
                SBInfoBox.Text = "Testing expression..";
                StartProgressbarTime(expressionTime);
            }
            catch (FACException fEx)
            {
                SBInfoBox.Text = "";
                //SBProgressBar.Value = SBProgressBar.Minimum;

                TextError.Text = "Warning! " + fEx.Message;
                WarningDialog warningDiag = new WarningDialog();
                warningDiag.tbInstructionText.Text = fEx.Message;
                warningDiag.Show();
            }
        }


        /// <summary>
        /// Updates the slider values from the current motor positions (useful for expressions sent through ECS)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            //List<ServoMotor> expressionConfig = FACEBody.CurrentState;
            //List<ServoMotor> defaultConfig = new List<ServoMotor>(FACEBody.DefaultState.ServoMotorsList); //temp ***
            //float defValue = 0;

            //try
            //{
            //    for (int i = 0; i < expressionConfig.ServoMotorsList.Count; i++)
            //    {
            //        defValue = expressionConfig.ServoMotorsList.ElementAt(i).PulseWidth;

            //        DockPanel dp = sliders[i].Content as DockPanel;
            //        int index = Int32.Parse(dp.Uid);
            //        StackPanel sp = dp.Children[1] as StackPanel;

            //        for (int k = 0; k < VisualTreeHelper.GetChildrenCount(sp); k++)
            //        {
            //            Visual childVisual = (Visual)VisualTreeHelper.GetChild(sp, k);
            //            if (childVisual.GetType() == typeof(Slider))
            //            {
            //                if (defValue != -1)
            //                {
            //                    (dp.Children[0] as CheckBox).IsChecked = true;
            //                    (childVisual as Slider).Value = defValue;
            //                    //ServoMotorGroup.ExecuteMovement(index, motorPos, expressionTime);
            //                    //expressionConfig.ServoMotorsList.ElementAt(index).PulseWidth = motorPos;
            //                }
            //                else
            //                {
            //                    (childVisual as Slider).Value = defaultConfig.ServoMotorsList.ElementAt(index).PulseWidth;
            //                    (dp.Children[0] as CheckBox).IsChecked = false;
            //                }
            //            }
            //        }
            //    }

            //    //PleasureTextbox.Text = String.Format(expressionConfig.Pleasure.ToString("0.00", CultureInfo.InvariantCulture));
            //    //ArousalTextbox.Text = String.Format(expressionConfig.Arousal.ToString("0.00", CultureInfo.InvariantCulture));
            //    //DominanceTextbox.Text = String.Format(expressionConfig.Dominance.ToString("0.00", CultureInfo.InvariantCulture));
            //    //NameTextbox.Text = expressionConfig.Name;
            //}
            //catch (FACException fEx)
            //{
            //    //TextError.Text = "Error occurs opening " + filename.ToString().Remove(filename.Length - 4) + " expression.";
            //    ErrorDialog errorDiag = new ErrorDialog();
            //    errorDiag.tbInstructionText.Text = fEx.Message;
            //    errorDiag.Show();
            //}
        }

        #endregion


        #region Update

        /// <summary>
        /// 
        /// </summary>
        private void UpdateSliders(List<ServoMotor>cs)
        {
            try
            {

                foreach (SliderController sliderControl in sliders)
                {
                    DockPanel dp = sliderControl.Content as DockPanel;
                    int index = Int32.Parse(dp.Uid);

                    if (cs[index].PulseWidthNormalized != -1)
                    {
                        sliderControl.SliderValueChanged -= new RoutedEventHandler(SliderCtlr_SliderValueChanged);


                        sliderControl.SliderControl.Value = cs[index].PulseWidthNormalized;

                        sliderControl.SliderValueChanged += new RoutedEventHandler(SliderCtlr_SliderValueChanged);
                      
                    }
                }

             
            }
            catch (FACException fEx)
            {
                //TextError.Text = "Error occurs loading " + filename.ToString().Remove(filename.Length - 4) + " expression.";
                ErrorDialog errorDiag = new ErrorDialog();
                errorDiag.tbInstructionText.Text = fEx.Message;
                errorDiag.Show();
            }
        }

        #endregion


        #region View Mode

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Image img = null;

            switch (visualMode)
            {
                case Mode.View:
                    break;
                case Mode.Edit:
                    EditGrid.Visibility = Visibility.Hidden;
                    img = EditButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceEdit.png")));
                    break;
                //case Mode.Config:
                //    ConfigGrid.Visibility = Visibility.Hidden;
                //    img = ConfigButton.Content as System.Windows.Controls.Image;
                //    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceConfig.png")));
                //    break;
                case Mode.Net:
                    NetGrid.Visibility = Visibility.Hidden;
                    img = NetButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceNet.png")));
                    break;
                case Mode.ECS:
                    break;
                case Mode.Gamepad:
                    break;
                case Mode.Test:
                    TestGrid.Visibility = Visibility.Hidden;
                    //img = RecognitionTestButton.Content as System.Windows.Controls.Image;
                    //img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceTest.png")));
                    break;
            }

            ViewGrid.Visibility = Visibility.Visible;
            visualMode = Mode.View;
            img = ViewButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceViewPressed.png")));
        }

        private void LoadAndSendExpression(string fileName, FACEActions actionType)
        {
            try
            {
                SBInfoBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    SBInfoBox.Text = "Testing " + fileName.ToString().Remove(fileName.Length - 4) + " expression..";
                }));
                FACEBody.ExecuteFile(fileName, 10);
                //LogEvents(actionType);
                //StartProgressbarTime(currExpression.Face.Time);
            }
            catch (FACException fEx)
            {
                TextError.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    TextError.Text = "Error occurs testing " + fileName.ToString().Remove(fileName.Length - 4) + " expression..";
                }));
                ErrorDialog errorDiag = new ErrorDialog();
                errorDiag.tbInstructionText.Text = fEx.Message;
                errorDiag.Show();
            }
        }
        
        private void LoadAndSendExpression(string fileName)
        {
            try
            {
                if (ComPorts.OpenedPort)
                {
                    SBInfoBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        SBInfoBox.Text = "Testing " + fileName.ToString().Remove(fileName.Length - 4) + " expression..";
                    }));
                    FACEBody.ExecuteFile(fileName, 10);
                }
                else
                 {
                     WarningDialog warningDialog = new WarningDialog();
                     warningDialog.tbInstructionText.Text = "There are not opened ports!";
                     warningDialog.Show();
                 }
                
            }
            catch (FACException fEx)
            {
                TextError.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    TextError.Text = "Error occurs testing " + fileName.ToString().Remove(fileName.Length - 4) + " expression..";
                }));
                ErrorDialog errorDiag = new ErrorDialog();
                errorDiag.tbInstructionText.Text = fEx.Message;
                errorDiag.Show();
            }
        }

        /*  Expressions  */
        #region Expression
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression("Config.xml", FACEActions.Reset);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void NeutralButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Neutral.xml", FACEActions.Neutral);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void HappyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Happiness.xml", FACEActions.Happiness);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void AngryButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Anger.xml", FACEActions.Anger);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void SadButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Sadness.xml", FACEActions.Sadness);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void DisgustButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Disgust.xml", FACEActions.Disgust);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void FearButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Fear.xml", FACEActions.Fear);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void SurpriseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(expressionsPath + "AU_Surprise.xml", FACEActions.Surprise);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }
        #endregion

        /*  Movements  */
        #region Movements
        private void YesMovementButton_Click(object sender, RoutedEventArgs e)
        {
            FACEBody.StartYesMovement();
            //LogEvents(FACEActions.Yes);
        }

        private void NoMovementButton_Click(object sender, RoutedEventArgs e)
        {
            FACEBody.StartNoMovement();
            //LogEvents(FACEActions.No);
        }

        private void CloseEyesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(motionsPath + "CloseEyes.xml", FACEActions.None);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void OpenEyesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                LoadAndSendExpression(motionsPath + "OpenEyes.xml", FACEActions.None);
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }
        #endregion

        #region New Set Expressions

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button1.xml");
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button2.xml");
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button3.xml");
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button4.xml");
        }

        private void Button5_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button5.xml");
        }
        private void Button6_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button6.xml");
        }

        private void Button7_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button7.xml");
        }

        private void Button8_Click(object sender, RoutedEventArgs e)
        {
            LoadAndSendExpression(NewSetExpressionPath + "Button8.xml");
        }

        private void Window_PreviewKey(object sender, KeyEventArgs e)
        {
            if (EnabledKeyPress.IsChecked==true)
            {
               

                KeyConverter kc = new KeyConverter();
                string key = kc.ConvertToString(e.Key);
                Console.WriteLine("Function linked to KeyPress: " + key);

                switch (key)
                {
                    case "1":

                        LoadAndSendExpression(NewSetExpressionPath + "Button1.xml");
                        break;

                    case "2":

                        LoadAndSendExpression(NewSetExpressionPath + "Button2.xml");
                        break;

                    case "3":

                        LoadAndSendExpression(NewSetExpressionPath + "Button3.xml");
                        break;

                    case "4":

                        LoadAndSendExpression(NewSetExpressionPath + "Button4.xml");
                        break;

                    case "5":

                        LoadAndSendExpression(NewSetExpressionPath + "Button5.xml");
                        break;

                    case "6":

                        LoadAndSendExpression(NewSetExpressionPath + "Button6.xml");
                        break;
                    case "7":

                        LoadAndSendExpression(NewSetExpressionPath + "Button7.xml");

                        break;
                    case "8":

                        LoadAndSendExpression(NewSetExpressionPath + "Button8.xml");
                        break;


                }
            }
        }

        private void EnabledKeyPress_Click(object sender, RoutedEventArgs e)
        {
            if (EnabledKeyPress.IsChecked != true)
            {
                Button1.IsEnabled = false;
                Button2.IsEnabled = false;
                Button3.IsEnabled = false;
                Button4.IsEnabled = false;
                Button5.IsEnabled = false;
                Button6.IsEnabled = false;
                Button7.IsEnabled = false;
                Button8.IsEnabled = false;
            }
            else
            {
                Button1.IsEnabled = true;
                Button2.IsEnabled = true;
                Button3.IsEnabled = true;
                Button4.IsEnabled = true;
                Button5.IsEnabled = true;
                Button6.IsEnabled = true;
                Button7.IsEnabled = true;
                Button8.IsEnabled = true;

            }

        }
        #endregion
        #endregion


        #region Editor Mode

        private bool loading = false; //testare: mettere loading = true in Load button

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Image img = null;

            switch (visualMode)
            {
                case Mode.View:
                    ViewGrid.Visibility = Visibility.Hidden;
                    img = ViewButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceView.png")));
                    break;
                case Mode.Edit:
                    break;
                //case Mode.Config:
                //    ConfigGrid.Visibility = Visibility.Hidden;
                //    img = ConfigButton.Content as System.Windows.Controls.Image;
                //    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceConfig.png")));
                //    break;
                case Mode.Net:
                    NetGrid.Visibility = Visibility.Hidden;
                    img = NetButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceNet.png")));
                    break;
                case Mode.ECS:
                    break;
                case Mode.Gamepad:
                    break;
                case Mode.Test:
                    TestGrid.Visibility = Visibility.Hidden;
                    //img = RecognitionTestButton.Content as System.Windows.Controls.Image;
                    //img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceTest.png")));
                    break;
            }

            EditGrid.Visibility = Visibility.Visible;
            visualMode = Mode.Edit;
            UpdateSliders(FACEBody.CurrentMotorState);
            img = EditButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceEditPressed.png")));

            NewButton.IsEnabled = true;
            LoadButton.IsEnabled = true;
        }

        private void SliderCtlr_SliderValueChanged(object sender, RoutedEventArgs e)
        {
            if (!loading)
            {
                SliderController sliderCtrl = e.Source as SliderController;
                DockPanel dp = sliderCtrl.Content as DockPanel;
                int index = Int32.Parse(dp.Uid);

                Slider sliderObj = e.OriginalSource as Slider;
                try
                {
                    float value = Convert.ToSingle(sliderObj.Value, NumberFormatInfo.InvariantInfo);
                    
                    FACEBody.ExecuteSingleMovement(index, value, TimeSpan.FromMilliseconds(expressionTime), 10, TimeSpan.FromMilliseconds(0));
                    //StartProgressbarTime(expressionConfig.Time);
                }
                catch (FACException fEx)
                {
                    SBInfoBox.Text = "";
                    //SBProgressBar.Value = SBProgressBar.Minimum;

                    TextError.Text = "Warning! " + fEx.Message;
                    WarningDialog warningDiag = new WarningDialog();
                    warningDiag.tbInstructionText.Text = fEx.Message;
                    warningDiag.Show();
                }
            }
        }

        private void SliderCtrl_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            SliderController sliderCtrl = e.Source as SliderController;
            DockPanel dp = sliderCtrl.Content as DockPanel;
            StackPanel sp = dp.Children[1] as StackPanel;
            sp.IsEnabled = true;
        }

        private void SliderCtrl_CheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            SliderController sliderCtrl = e.Source as SliderController;
            DockPanel dp = sliderCtrl.Content as DockPanel;
            StackPanel sp = dp.Children[1] as StackPanel;
            sp.IsEnabled = false;
        }

        #endregion


        #region ECS Mode

        private int ecsTime = 800;

        private void ECSButton_Click(object sender, RoutedEventArgs e)
        {
            //ThreadPool.QueueUserWorkItem(loadECS);
            //this.Dispatcher.BeginInvoke(
            //    System.Windows.Threading.DispatcherPriority.Normal,
            //    (System.Threading.ThreadStart)delegate
            //    {                    
            ecsWin = new ECSWin();
            ecsWin.Closed += new EventHandler(ecsWin_Closed);
            ecsWin.ecscontroller.NewECSEventHandler += new RoutedEventHandler(ecscontroller_ECSEventHandler);

            foreach (FACEMotion motion in ecsMotions)
            {
                ecsWin.ecscontroller.LoadECSPoint(motion.Name, new Point(motion.ECSCoord.Pleasure, motion.ECSCoord.Arousal));
            }

                    ////System.Windows.Controls.Image img = ECSButton.Content as System.Windows.Controls.Image;
                    ////img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceECSPressed.png")));

                    ecsWin.Show();
                //});
        }

        private void loadECS(object state) 
        {
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (System.Threading.ThreadStart)delegate
                {
                    ecsWin = new ECSWin();
                    ecsWin.Closed += new EventHandler(ecsWin_Closed);
                    ecsWin.ecscontroller.NewECSEventHandler += new RoutedEventHandler(ecscontroller_ECSEventHandler);

                    foreach (FACEMotion motion in ecsMotions)
                    {
                        ecsWin.ecscontroller.LoadECSPoint(motion.Name, new Point(motion.ECSCoord.Pleasure, motion.ECSCoord.Arousal));
                    }

                    //System.Windows.Controls.Image img = ECSButton.Content as System.Windows.Controls.Image;
                    //img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceECSPressed.png")));

                    ecsWin.Show();
                });
        }

        private void ecsWin_Closed(object sender, EventArgs e)
        {
            System.Windows.Controls.Image img = ECSButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceECS.png")));
        }

        private void ecscontroller_ECSEventHandler(object sender, RoutedEventArgs e)
        {
            Point p = (Point)e.OriginalSource;

            FACEMotion motionToTest = new FACEMotion(FACEBody.CurrentMotorState.Count);
            foreach (ECSMotor m in ecs.ECSMotorList)
            {
                float f = ecs.GetECSValue(m.Channel, (float)p.X, (float)p.Y);
                motionToTest.ServoMotorsList[m.Channel].PulseWidthNormalized = f;
            }

            //foreach (ServoMotor m in motionToTest.ServoMotorsList)
            //{
            //    System.Diagnostics.Debug.WriteLine(motionToTest.ServoMotorsList[m.Channel].PulseWidth - FACEBody.CurrentMotorState[m.Channel].PulseWidth);
            //}

            motionToTest.Duration = expressionTime;
            motionToTest.Priority = 10;
            motionToTest.ECSCoord = new ECS.ECSCoordinate((float)p.X, (float)p.Y, 0);


            SetMotors(motionToTest.ServoMotorsList, TimeSpan.FromMilliseconds(expressionTime));

            //try
            //{
            //    //SBInfoBox.Text = "Testing expression..";
            //    FACEBody.ExecuteMotion(motionToTest);
            //    //StartProgressbarTime(expressionConfig.Time);
            //}
            //catch (FACException fEx)
            //{
            //    SBInfoBox.Text = "";
            //    //SBProgressBar.Value = SBProgressBar.Minimum;

            //    TextError.Text = "Warning! " + fEx.Message;
            //    WarningDialog warningDiag = new WarningDialog();
            //    warningDiag.tbInstructionText.Text = fEx.Message;
            //    warningDiag.Show();
            //}
        }

        #endregion 


        #region Gamepad Mode

        private GamepadWin gamepadWin = null;

        private void GamepadButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Image img = GamepadButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceGamepadPressed.png")));

            gamepadWin = new GamepadWin();
            gamepadWin.Closed += new EventHandler(gamepadWin_Closed);
            gamepadWin.Gamepad0.ButtonClickedEventHandler += new RoutedEventHandler(Gamepad0_ButtonClickedEventHandler);

            gamepadWin.Gamepad1.ButtonClickedEventHandler += new RoutedEventHandler(Gamepad1_ButtonClickedEventHandler);
            gamepadWin.Gamepad1.AxisChangedEventHandler += new RoutedEventHandler(Gamepad1_AxisChangedEventHandler);
            gamepadWin.Show();
        }

        private void gamepadWin_Closed(object sender, EventArgs e)
        {
            System.Windows.Controls.Image img = GamepadButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceGamepad.png")));
        }

        private void Gamepad0_ButtonClickedEventHandler(object sender, RoutedEventArgs e)
        {
            double ecsIncr = 0.0005;

            int idButton = (int)e.OriginalSource;
            switch (idButton)
            {
                case 1:
                    if (ecsWin.ecscontroller.CurrentECS.Y <= 1 - ecsIncr)
                        ecsWin.ecscontroller.CurrentECS = new Point(ecsWin.ecscontroller.CurrentECS.X, ecsWin.ecscontroller.CurrentECS.Y + ecsIncr);
                    break;
                case 2:
                    if (ecsWin.ecscontroller.CurrentECS.X <= 1 - ecsIncr)
                        ecsWin.ecscontroller.CurrentECS = new Point(ecsWin.ecscontroller.CurrentECS.X + ecsIncr, ecsWin.ecscontroller.CurrentECS.Y);
                    break;
                case 3:
                    if (ecsWin.ecscontroller.CurrentECS.Y >= -1 + ecsIncr)
                        ecsWin.ecscontroller.CurrentECS = new Point(ecsWin.ecscontroller.CurrentECS.X, ecsWin.ecscontroller.CurrentECS.Y - ecsIncr);
                    break;
                case 4:
                    if (ecsWin.ecscontroller.CurrentECS.X >= -1 + ecsIncr)
                        ecsWin.ecscontroller.CurrentECS = new Point(ecsWin.ecscontroller.CurrentECS.X - ecsIncr, ecsWin.ecscontroller.CurrentECS.Y);
                    break;
                case 5:

                    break;
                case 6:

                    break;
                case 7:

                    break;
                case 8:

                    break;
                case 9:

                    break;
                case 10:
                    //if (!(bool)CheckboxBrain.IsChecked)
                    //    CheckboxBrain.IsChecked = true;
                    //AutomaticFaceTrackingCheckbox.IsChecked = (bool)AutomaticFaceTrackingCheckbox.IsChecked ? false : true;
                    break;
                default:
                    //
                    break;
            }
        }

        private void Gamepad1_ButtonClickedEventHandler(object sender, RoutedEventArgs e)
        {
            float neckIncr = 0.20f;
            int neckTime = 300; // to be tested
            float lowerNodV = 0;

            switch ((int)e.OriginalSource)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    lowerNodV = FACEBody.CurrentMotorState[(int)MotorsNames.LowerNod].PulseWidthNormalized;
                    if (lowerNodV <= 1 - neckIncr)
                        lowerNodV += neckIncr;
                    FACEBody.ExecuteSingleMovement((int)MotorsNames.LowerNod, lowerNodV, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
                    break;
                case 6:
                    break;
                case 7:
                    lowerNodV = FACEBody.CurrentMotorState[(int)MotorsNames.LowerNod].PulseWidthNormalized;
                    if (lowerNodV >= neckIncr)
                        lowerNodV -= neckIncr;
                    FACEBody.ExecuteSingleMovement((int)MotorsNames.LowerNod, lowerNodV, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
                    break;
                case 8:
                    break;
                case 9:
                    break;
                case 10:
                    break;
                default:
                    break;
            }
        }

        private void Gamepad1_AxisChangedEventHandler(object sender, RoutedEventArgs e)
        {
            float neckIncr = 0.20f;
            int neckTime = 300; // to be tested
            float turnV = 0, upperNodV = 0;

            Point p = (Point)e.OriginalSource;

            turnV = FACEBody.CurrentMotorState[(int)FaceTrackingControls.Turn].PulseWidthNormalized;
            upperNodV = FACEBody.CurrentMotorState[(int)FaceTrackingControls.UpperNod].PulseWidthNormalized;

            if (p.X < -900)
            {
                if (turnV >= neckIncr)
                    turnV -= neckIncr;
                FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.Turn, turnV, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
            }
            else if (p.X > 900)
            {
                if (turnV <= 1 - neckIncr)
                    turnV += neckIncr;
                FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.Turn, turnV, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
            }

            if (p.Y < -900)
            {
                if (upperNodV <= 1 - neckIncr)
                    upperNodV += neckIncr;
                FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.UpperNod, upperNodV, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
            }
            else if (p.Y > 900)
            {
                if (upperNodV >= neckIncr)
                    upperNodV -= neckIncr;
                FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.UpperNod, upperNodV, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
            }
        }

        #endregion


        #region Animator

        private System.Threading.Thread animation;

        private void CheckboxAnimator_Checked(object sender, RoutedEventArgs e)
        {
            animation = new System.Threading.Thread(new ThreadStart(AnimationEngine.StartAnimation));
            animation.Start();
        }

        private void CheckboxAnimator_Unchecked(object sender, RoutedEventArgs e)
        {
            AnimationEngine.StopAnimation();
        }

        #endregion


        #region PreviewKey events

        private float turnNeckValue, upperNodNeckValue;

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
            //    AutomaticFaceTrackingCheckbox.IsChecked = false;

            upperNodNeckValue = FACEBody.CurrentMotorState[(int)FaceTrackingControls.UpperNod].PulseWidthNormalized;
            turnNeckValue = FACEBody.CurrentMotorState[(int)FaceTrackingControls.Turn].PulseWidthNormalized;

            float step = 0.01f;

            switch (e.Key)
            {
                case Key.Up:
                    if (upperNodNeckValue < 1)
                        upperNodNeckValue += step;
                    break;
                case Key.Down:
                    if (upperNodNeckValue > 0)
                        upperNodNeckValue -= step;
                    break;
                case Key.Left:
                    if (turnNeckValue < 1)
                        turnNeckValue += step;
                    break;
                case Key.Right:
                    if (turnNeckValue > 0)
                        turnNeckValue -= step;
                    break;
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.UpperNod, upperNodNeckValue, TimeSpan.FromMilliseconds(expressionTime), 10, TimeSpan.FromMilliseconds(0));
                    break;
                case Key.Down:
                    FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.UpperNod, upperNodNeckValue, TimeSpan.FromMilliseconds(expressionTime), 10, TimeSpan.FromMilliseconds(0));
                    break;
                case Key.Left:
                    FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.Turn, turnNeckValue, TimeSpan.FromMilliseconds(expressionTime), 10, TimeSpan.FromMilliseconds(0));
                    break;
                case Key.Right:
                    FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.Turn, turnNeckValue, TimeSpan.FromMilliseconds(expressionTime), 10, TimeSpan.FromMilliseconds(0));
                    break;
            }
        }

        #endregion


        #region Blinking

        private void BlinkRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BlinkRateBox != null)
            {
                
                int rate = 1;
                try
                {
                    rate = Convert.ToInt32(BlinkRateBox.Text, NumberFormatInfo.InvariantInfo);
                    if (rate < 1)
                    {
                        rate = 1;
                        BlinkRateBox.Text = String.Format(rate.ToString("0", CultureInfo.InvariantCulture));
                    }

                    if (AutomaticBlinkingCheckbox != null && AutomaticBlinkingCheckbox.IsChecked == true)
                    {
                        FACEBody.StopBlinking();
                        FACEBody.BlinkingRate = rate;
                        FACEBody.StartBlinking();
                    }
                }
                catch
                {
                    rate = 1;
                    BlinkRateBox.Text = String.Format(rate.ToString("0", CultureInfo.InvariantCulture));
                }
            }
        }

        private void BlinkTimeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BlinkTimeBox != null)
            {
                int closedTime = 1;
                try
                {
                    closedTime = Convert.ToInt32(BlinkTimeBox.Text, NumberFormatInfo.InvariantInfo);
                    if (closedTime < 1)
                    {
                        closedTime = 1;
                        BlinkTimeBox.Text = String.Format(closedTime.ToString("00", CultureInfo.InvariantCulture));
                    }
                    if (AutomaticBlinkingCheckbox != null && AutomaticBlinkingCheckbox.IsChecked == true)
                    {
                        FACEBody.StopBlinking();
                        FACEBody.ClosedEyesTime = closedTime;
                        FACEBody.StartBlinking();
                    }
                }
                catch
                {
                    closedTime = 1;
                    BlinkTimeBox.Text = String.Format(closedTime.ToString("0", CultureInfo.InvariantCulture));
                }
            }
        }

        private void BlinkSpeedBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BlinkSpeedBox != null)
            {
                int speed = 1;
                try
                {
                    speed = Convert.ToInt32(BlinkSpeedBox.Text, NumberFormatInfo.InvariantInfo);
                    if (speed < 1)
                    {
                        speed = 1;
                        BlinkSpeedBox.Text = String.Format(speed.ToString("00", CultureInfo.InvariantCulture));
                    }
                    if (AutomaticBlinkingCheckbox != null && AutomaticBlinkingCheckbox.IsChecked == true)
                    {
                        FACEBody.StopBlinking();
                        FACEBody.SpeedEyesTime = speed;
                        FACEBody.StartBlinking();
                        //Brain.ClosedEyesTime = closedTime;
                    }
                }
                catch
                {
                    speed = 1;
                    BlinkSpeedBox.Text = String.Format(speed.ToString("0", CultureInfo.InvariantCulture));
                }
            }
        }

        private void CheckboxAutomaticBlinking_Checked(object sender, RoutedEventArgs e)
        {
            //FACEBody.StartBlinking(FACEBody.BlinkingRate);
            FACEBody.StartBlinking();
            //LogEvents(FACEActions.StartBlinking);

            AutomaticBlinkingParams.IsEnabled = true;

            ManualBlinkingCheckbox.IsChecked = false;
            ManualBlinkingSliderPanel.IsEnabled = false;
        }

        private void CheckboxAutomaticBlinking_Unchecked(object sender, RoutedEventArgs e)
        {
            FACEBody.StopBlinking();
            //LogEvents(FACEActions.StopBlinking);

            AutomaticBlinkingParams.IsEnabled = false;
        }

        private void CheckboxManualBlinking_Checked(object sender, RoutedEventArgs e)
        {
            AutomaticBlinkingCheckbox.IsChecked = false;
            AutomaticBlinkingParams.IsEnabled = false;

            ManualBlinkingSliderPanel.IsEnabled = true;
        }

        private void CheckboxManualBlinking_Unchecked(object sender, RoutedEventArgs e)
        {
            ManualBlinkingSliderPanel.IsEnabled = false;
        }


        #endregion


        #region Test

        private void RecognitionTestButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Image img = null;

            switch (visualMode)
            {
                case Mode.View:
                    ViewGrid.Visibility = System.Windows.Visibility.Hidden;
                    img = ViewButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceView.png")));
                    break;
                case Mode.Edit:
                    EditGrid.Visibility = System.Windows.Visibility.Hidden;
                    img = EditButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceEdit.png")));
                    break;
                //case Mode.Config:
                //    ConfigGrid.Visibility = Visibility.Hidden;
                //    img = ConfigButton.Content as System.Windows.Controls.Image;
                //    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceConfig.png")));
                //    break;
                case Mode.Net:
                    NetGrid.Visibility = Visibility.Hidden;
                    img = NetButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceNet.png")));
                    break;
                case Mode.ECS:
                    break;
                case Mode.Gamepad:
                    break;
                case Mode.Test:
                    break;
            }

            TestGrid.Visibility = Visibility.Visible;
            visualMode = Mode.Test;
            img = RecognitionTestButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceTestPressed.png")));
        }

        private DispatcherTimer clockTimer;
        private DateTime startClockTime;
        private TimeSpan timeDiff;

        private string logName;
        private DateTime startTime;
        private DateTime lastExpressionTime;
        private DateTime answerTime;
        private RadioButton selected;

        private int answer;
        private int countTick = 0;
        private int idExpression = 0;
        private List<int> outputList;

        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        public static readonly RoutedEvent TickEvent = EventManager.RegisterRoutedEvent("KeySpacePressed",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DispatcherTimer));

        public enum FACEAnswers
        {
            Pride = 0, Happiness, Embarrassment, Neutral, Surprise, Disgust, Pain, Pity, Contempt, Sadness,
            Interest, Shame, Fear, Excitement, Anger, Other
        };

        #region Logs

        private void InitTestLog()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd", new System.Globalization.CultureInfo("it-IT", false).DateTimeFormat);
            string time = String.Format("{0:HH.mm.ss}", DateTime.Now);
            logName = "TestFACE_" + date + "_" + time + ".xml";
            logsPath += logName;

            if (!File.Exists(logsPath))
            {
                XDocument xDoc = new XDocument(new XDeclaration("1.0", System.Text.Encoding.UTF8.WebName, "yes"));

                XElement xRoot = new XElement(XName.Get("Test"));
                xRoot.SetAttributeValue("Date", date);
                startTime = DateTime.Now;
                lastExpressionTime = startTime;
                xRoot.SetAttributeValue("Time", String.Format("{0:HH:mm:ss.ffff}", startTime));
                xRoot.SetAttributeValue("Timestamp", String.Format("{0:0.000000}", FromDateToDouble(startTime)).Replace(",", "."));
                xDoc.Add(xRoot);

                XElement xmlElem = new XElement("PersonalInfo");
                xmlElem.SetAttributeValue("Name", TextboxName.Text);
                xmlElem.SetAttributeValue("Surname", TextboxSurname.Text);
                xmlElem.SetAttributeValue("Birth", TextboxDay.Text + "/" + TextboxMonth.Text + "/" + TextboxYear.Text);
                xmlElem.SetAttributeValue("Age", (2012 - Int32.Parse(TextboxYear.Text)).ToString());
                xmlElem.SetAttributeValue("Sex", ((bool)MRadioButton.IsChecked) ? "M" : "F");
                xmlElem.SetAttributeValue("Faculty", TextboxFaculty.Text);
                xmlElem.SetAttributeValue("Job", TextboxJob.Text);
                xDoc.Root.Add(xmlElem);

                XElement actions = new XElement(XName.Get("Events"));
                xRoot.Add(actions);

                xDoc.Save(logsPath);
            }
            else
            {
                XDocument xDoc = XDocument.Load(logsPath);
            }
        }

        private void LogFaceEvents(FACEActions actionType)
        {
            try
            {
                XDocument xDoc = XDocument.Load(logsPath);

                XElement xmlElem = new XElement("Action");
                xmlElem.SetAttributeValue("Type", "Expression");
                xmlElem.SetAttributeValue("Value", actionType);
                xmlElem.SetAttributeValue("Index", (int)actionType);
                xmlElem.SetAttributeValue("Timestamp", String.Format("{0:0.000000}", FromDateToDouble(lastExpressionTime)).Replace(",", "."));
                xmlElem.SetAttributeValue("Time", String.Format("{0:HH:mm:ss.ffff}", DateTime.Now));

                xDoc.Root.Element("Events").Add(xmlElem);
                xDoc.Save(logsPath);
            }
            catch
            {
                ErrorDialog errDialog = new ErrorDialog();
                errDialog.tbInstructionText.Text = "Some problems occurred writing the log file.";
                errDialog.Show();
            }
        }

        private void LogSubjectEvents(int answer, DateTime dt)
        {
            try
            {
                XDocument xDoc = XDocument.Load(logsPath);

                XElement xmlElem = new XElement("Action");
                xmlElem.SetAttributeValue("Type", "Answer");
                xmlElem.SetAttributeValue("Value", Enum.GetName(typeof(FACEAnswers), answer));
                xmlElem.SetAttributeValue("Index", answer);
                xmlElem.SetAttributeValue("Timestamp", String.Format("{0:0.000000}", FromDateToDouble(dt)).Replace(",", "."));
                xmlElem.SetAttributeValue("ResponseTime", String.Format("{0:0}", (dt.Subtract(lastExpressionTime)).ToString()));

                xDoc.Root.Element("Events").Add(xmlElem);
                xDoc.Save(logsPath);
            }
            catch
            {
                ErrorDialog errDialog = new ErrorDialog();
                errDialog.tbInstructionText.Text = "Some problems occurred writing the log file.";
                errDialog.Show();
            }
        }

        private void LogRestEvent(TimeSpan restSpan)
        {
            try
            {
                XDocument xDoc = XDocument.Load(logsPath);

                XElement xmlElem = new XElement("Action");
                xmlElem.SetAttributeValue("Type", "Rest");
                xmlElem.SetAttributeValue("RestTime", String.Format("{0:0}", restSpan.ToString()));

                xDoc.Root.Element("Events").Add(xmlElem);
                xDoc.Save(logsPath);
            }
            catch
            {
                ErrorDialog errDialog = new ErrorDialog();
                errDialog.tbInstructionText.Text = "Some problems occurred writing the log file.";
                errDialog.Show();
            }
        }

        private void ConvertTimestamp()
        {
            string[] filePaths = Directory.GetFiles(@"D:\Università\_Test espressioni\DATI\Conversione", "*.xml");
            foreach (string file in filePaths)
            {
                FileInfo fInfo = new FileInfo(file);
                XDocument xDoc = XDocument.Load(file);
                XElement root = xDoc.Root; //Test
                XElement events = root.Element(XName.Get("Events"));
                IEnumerable<XElement> elements = events.Elements(XName.Get("Action"));

                XDocument newDoc = new XDocument(new XDeclaration("1.0", System.Text.Encoding.UTF8.WebName, "yes"));

                XElement xRoot = new XElement(XName.Get("Test"));
                newDoc.Add(xRoot);
                XElement xmlElem = new XElement("PersonalInfo");
                newDoc.Root.Add(xmlElem);
                XElement newEvents = new XElement(XName.Get("Events"));
                newDoc.Root.Add(newEvents);

                DateTime parsedDate = DateTime.Now;
                foreach (XElement elem in elements)
                {
                    XElement el = new XElement("Action");

                    if ((string)elem.Attribute("Type") == "Rest")
                    {
                        el.SetAttributeValue("Type", (string)elem.Attribute("Type"));
                        el.SetAttributeValue("RestTime", (string)elem.Attribute("RestTime"));
                    }
                    else if ((string)elem.Attribute("Type") == "Expression")
                    {
                        el.SetAttributeValue("Type", (string)elem.Attribute("Type"));
                        el.SetAttributeValue("Value", (string)elem.Attribute("Value"));
                        el.SetAttributeValue("Index", (string)elem.Attribute("Index"));
                        string date = (string)root.Attribute("Date") + " " + (string)elem.Attribute("Time");
                        DateTime.TryParseExact(date, "yyyy-MM-dd HH:mm:ss.ffff", null, DateTimeStyles.None, out parsedDate);
                        double ts = FromDateToDouble(parsedDate);
                        el.SetAttributeValue("Timestamp", ts);
                        el.SetAttributeValue("Time", (string)elem.Attribute("Time"));

                        //string ts = (string)elem.Attribute("Timestamp");
                        //el.SetAttributeValue("Timestamp", ts);
                        //double parsed = Double.Parse(ts.Replace('.', ','));
                        //DateTime newdt = FromDoubleToDate(parsed);
                        //el.SetAttributeValue("Time", String.Format("{0:HH:mm:ss.ffff}", newdt));                        
                        //string tts = (1355324775.6170).ToString();
                        //double parsedt = Double.Parse(tts.Replace('.', ','));
                        //DateTime newdtt = FromDoubleToDate(parsedt);
                    }
                    else if ((string)elem.Attribute("Type") == "Answer")
                    {
                        if ((string)elem.Attribute("Value") != "-")
                        {
                            el.SetAttributeValue("Type", (string)elem.Attribute("Type"));
                            el.SetAttributeValue("Value", (string)elem.Attribute("Value"));
                            el.SetAttributeValue("Index", (string)elem.Attribute("Index"));
                            string stamp = (string)elem.Attribute("Timestamp");
                            el.SetAttributeValue("Timestamp", stamp.Substring(0, stamp.Length - 2));
                            string respTime = (string)elem.Attribute("ResponseTime");
                            TimeSpan response = TimeSpan.Parse(respTime);
                            double parsedt = Double.Parse(stamp.Replace('.', ','));
                            DateTime rTime = FromDoubleToDate(parsedt);
                            //DateTime rTime = parsedDate.Add(response);
                            //el.SetAttributeValue("Time", String.Format("{0:HH:mm:ss.ffff}", rTime));
                            el.SetAttributeValue("Time", String.Format("{0:HH:mm:ss.ffff}", rTime));
                            el.SetAttributeValue("ResponseTime", respTime);
                        }
                    }
                    newDoc.Root.Element("Events").Add(el);
                }
                newDoc.Save(@"D:\Università\_Test espressioni\DATI\Conversione\NewFiles\OK_" + fInfo.Name);
            }
        }

        #endregion


        #region Buttons

        private void RestTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (TextboxName.Text != "" && TextboxSurname.Text != "" && TextboxDay.Text != "" && TextboxMonth.Text != "" &&
                TextboxYear.Text != "" && (MRadioButton.IsChecked == true || FRadioButton.IsChecked == true))
            {
                PersonalDataPanel.IsEnabled = false;
                InitTestLog();
                StartClock();
            }
            else
            {
                MessageBox.Show("Uno o più campi non sono corretti!");
            }
        }

        private void GotoTestButton_Click(object sender, RoutedEventArgs e)
        {
            LogRestEvent(DateTime.Now.Subtract(startClockTime));
            clockTimer.Stop();
            GotoTestButton.IsEnabled = true;
            TestPanel.IsEnabled = true;
            LeftPanel.IsEnabled = false;
        }

        private void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComPorts.OpenedPort)
            {
                //t = new System.Threading.Timer(new System.Threading.TimerCallback(TimerProc));                
                List<int> inputList = new List<int> { 1, 2, 3, 4, 5, 6 };
                outputList = ShuffleList(inputList);

                dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = TimeSpan.FromSeconds(10);
                dispatcherTimer.Start();
                //ComPorts.SelectedPort.WriteLine(String.Format("{0:HH.mm.ss}", DateTime.Now));
            }
            else
            {
                WarningDialog warningDialog = new WarningDialog();
                warningDialog.tbInstructionText.Text = "There are not opened ports!";
                warningDialog.Show();
            }
        }

        private void NextTestButton_Click(object sender, RoutedEventArgs e)
        {
            LogSubjectEvents(answer, answerTime);
            selected.IsChecked = false;

            if (countTick == 0)
            {
                countTick = 1;
                RaiseEvent(new RoutedEventArgs(TickEvent));
            }
        }

        private void StopTestButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            PersonalDataPanel.IsEnabled = true;
            TestPanel.IsEnabled = false;
        }

        #endregion


        #region Controls

        private void StartClock()
        {
            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1.0);
            clockTimer.Start();
            TimeSpan end = new TimeSpan(0, 3, 1);
            startClockTime = DateTime.Now;
            clockTimer.Tick += new EventHandler(delegate(object s, EventArgs a)
            {
                timeDiff = DateTime.Now.Subtract(startClockTime);
                if (timeDiff.CompareTo(end) == -1)
                {
                    string sec = (timeDiff.Seconds < 10) ? "0" + timeDiff.Seconds.ToString() : timeDiff.Seconds.ToString();
                    ClockTextbox.Text = "0" + timeDiff.Hours + ":0" + timeDiff.Minutes + ":" + sec;
                }
                else
                {
                    GotoTestButton.IsEnabled = true;
                }
            });
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (countTick == 0)
            {
                switch (outputList[idExpression])
                {
                    case 1:
                        LoadAndSendExpressionForTest(testPath + "AU_Anger.xml", FACEActions.Anger);
                        break;
                    case 2:
                        LoadAndSendExpressionForTest(testPath + "AU_Disgust.xml", FACEActions.Disgust);
                        break;
                    case 3:
                        LoadAndSendExpressionForTest(testPath + "AU_Fear.xml", FACEActions.Fear);
                        break;
                    case 4:
                        LoadAndSendExpressionForTest(testPath + "AU_Happiness.xml", FACEActions.Happiness);
                        break;
                    case 5:
                        LoadAndSendExpressionForTest(testPath + "AU_Sadness.xml", FACEActions.Sadness);
                        break;
                    case 6:
                        LoadAndSendExpressionForTest(testPath + "AU_Surprise.xml", FACEActions.Surprise);
                        break;
                }
                //lastExpressionTime = DateTime.Now;
                idExpression++;
                countTick = 1;
                //ComPorts.SelectedPort.WriteLine(String.Format("----- {0:HH.mm.ss} espressione", DateTime.Now));
            }
            else if (countTick == 1)
            {
                LoadAndSendExpressionForTest(testPath + "AU_Neutral.xml", FACEActions.Neutral);
                countTick = 2;
                //ComPorts.SelectedPort.WriteLine(String.Format("----- {0:HH.mm.ss} Neutra", DateTime.Now));
            }
            else if (countTick == 2)
            {
                countTick = 0;
                if (idExpression == outputList.Count)
                {
                    dispatcherTimer.Stop();
                    NextTestButton.IsEnabled = false;
                }
            }

            // Forcing the CommandManager to raise the RequerySuggested event
            //CommandManager.InvalidateRequerySuggested();
        }

        private void LoadAndSendExpressionForTest(string fileName, FACEActions actionType)
        {
            try
            {
                //SBInfoBox.Text = "Testing " + fileName.ToString().Remove(fileName.Length - 4) + " expression..";
                FACEBody.ExecuteFile(fileName, 10);
                lastExpressionTime = DateTime.Now;
                LogFaceEvents(actionType);
            }
            catch (FACException fEx)
            {
                TextError.Text = "Error occurs testing " + fileName.ToString().Remove(fileName.Length - 4) + " expression..";
                ErrorDialog errorDiag = new ErrorDialog();
                errorDiag.tbInstructionText.Text = fEx.Message;
                errorDiag.Show();
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (selected != null)
                selected.IsChecked = false;
            answerTime = DateTime.Now;
            selected = (RadioButton)sender;
            answer = Int32.Parse(selected.Uid);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (dispatcherTimer.IsEnabled)
                {
                    LoadAndSendExpressionForTest(testPath + "AU_Neutral.xml", FACEActions.Neutral);
                    //ComPorts.SelectedPort.WriteLine(String.Format("----- {0:HH.mm.ss} Barra spaziatrice", DateTime.Now));
                    //if (countTick == 0)
                    //{
                    //    LoadAndSendExpressionForTest(testPath + "AU_Neutral.xml", FACEActions.Neutral);
                    //    countTick = 1;
                    //}
                    //else if (countTick == 1)
                    //    countTick = 2;

                    //RaiseEvent(new RoutedEventArgs(TickEvent));                    
                }
                else
                {
                    WarningDialog warningDialog = new WarningDialog();
                    warningDialog.tbInstructionText.Text = "There are not opened ports!";
                    warningDialog.Show();
                }
            }
        }

        private List<int> ShuffleList(List<int> inputList)
        {
            List<int> randomList = new List<int>();
            System.Random r = new System.Random();
            int randomIndex = 0;
            while (inputList.Count > 0)
            {
                randomIndex = r.Next(0, inputList.Count); //Choose a random object in the list
                randomList.Add(inputList[randomIndex]); //add it to the new, random list
                inputList.RemoveAt(randomIndex); //remove to avoid duplicates
            }
            return randomList; //return the new random list
        }

        #endregion

        #endregion


        #region Respiration

        private void RespirationRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RespirationRateBox != null)
            {
                int rate = 1;
                try
                {
                    rate = Convert.ToInt32(RespirationRateBox.Text, NumberFormatInfo.InvariantInfo);
                    if (rate < 1)
                    {
                        rate = 1;
                        RespirationRateBox.Text = String.Format(rate.ToString("0", CultureInfo.InvariantCulture));
                    }
                    FACEBody.StopRespiration();
                    FACEBody.RespirationRate = rate;
                    FACEBody.StartRespiration();
                }
                catch
                {
                    rate = 1;
                    RespirationRateBox.Text = String.Format(rate.ToString("0", CultureInfo.InvariantCulture));
                }
            }
        }


        private void RespirationSpeedBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RespirationSpeedBox != null)
            {
                int speed = 1;
                try
                {
                    speed = Convert.ToInt32(RespirationSpeedBox.Text, NumberFormatInfo.InvariantInfo);
                    if (speed < 1)
                    {
                        speed = 1;
                        RespirationSpeedBox.Text = String.Format(speed.ToString("00", CultureInfo.InvariantCulture));
                    }
                    FACEBody.StopRespiration();
                    FACEBody.RespirationSpeed = speed;
                    FACEBody.StartRespiration();
                    //Brain.ClosedEyesTime = closedTime;
                }
                catch
                {
                    speed = 1;
                    RespirationSpeedBox.Text = String.Format(speed.ToString("0", CultureInfo.InvariantCulture));
                }
            }
        }

        private void CheckboxAutomaticRespiration_Checked(object sender, RoutedEventArgs e)
        {
            FACEBody.StartRespiration();
            //LogEvents(FACEActions.StartRespiration);

            AutomaticRespirationParams.IsEnabled = true;
            RespirationRateBox.IsEnabled = true;
            RespirationSpeedBox.IsEnabled = true;
        }

        private void CheckboxAutomaticRespiration_Unchecked(object sender, RoutedEventArgs e)
        {
            FACEBody.StopRespiration();

            AutomaticRespirationParams.IsEnabled = false;
            RespirationRateBox.IsEnabled = false;
            RespirationSpeedBox.IsEnabled = false;
        }

        #endregion


        #region Status monitor

        //private Thread statusMonitor;
        private StatusMonitor.MainWindow statusWin;

        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
          
            statusWin = new StatusMonitor.MainWindow();
            statusWin.Show();
        
        }

        #endregion
        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            FACEConfigWin fc = new FACEConfigWin();
            fc.ShowDialog();

            FACEBody.LoadConfigFile("Config.xml");
            currentSmState = FACEBody.CurrentMotorState;

            InitEditMode(RightSlidersPanel);
            InitEditMode(LeftSlidersPanel);
        }

        #region YARP

    
       //   void ReceiveDataSetMotors(object sender, ElapsedEventArgs e) 
        void ReceiveDataSetMotors(object sender) 
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {

                yarpPortSetMotors.receivedData(out receivedSetMotors);
                if (receivedSetMotors != null && receivedSetMotors != "" && yarpExpressionOn)
                {
                    //Console.WriteLine(receivedSetMotors);
                    try
                    {
                        List<ServoMotor> listMotors = new List<ServoMotor>();



                        string xml = receivedSetMotors;
                        xml = xml.Replace(@"\", "");
                        xml = xml.Substring(1, xml.Length - 2);
                        //xml = xml.Replace(@"?", "");
                        if (xml.Substring(0, 1) == "?")
                            xml = xml.Remove(0, 1);

                        //Console.WriteLine(xml.Substring(0, 5));
                        if (xml.Substring(0, 5) == "<?xml")
                        {

                            listMotors = ComUtils.XmlUtils.Deserialize<List<ServoMotor>>(receivedSetMotors);

                            foreach (ServoMotor serv in listMotors.FindAll(a => ((a.PulseWidthNormalized <= 0 && a.PulseWidthNormalized >= 1) && (a.PulseWidthNormalized != -1.0)))) 
                            {
                                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                                                       new Action(delegate()
                                {
                                    MessageBox.Show("Error PulseWidth of ServoMotor " + serv.Name + " PulseWidth:" + serv.PulseWidthNormalized);
                                                                
                                                       }));
                                
                                continue;
                            }

                            if (listMotors.FindAll(a => ((a.PulseWidthNormalized <= 0 && a.PulseWidthNormalized >= 1) && (a.PulseWidthNormalized != -1.0))).Count > 0)
                                continue;
                            //foreach (ServoMotor serv in listMotors)
                            //{
                            //    //if (!((serv.PulseWidth >= 0 && serv.PulseWidth <= 1) || serv.PulseWidth == -1.0))
                            //    //{
                            //    //    //SBInfoBox.Text = "";
                            //    //    //SBProgressBar.Value = SBProgressBar.Minimum;

                            //    //    //TextError.Text = "Error PulseWidth of ServoMotor " + serv.Name + " PulseWidth:" + serv.PulseWidth;
                            //    //    //WarningDialog warningDiag = new WarningDialog();
                            //    //    //warningDiag.tbInstructionText.Text = "Error PulseWidth of ServoMotor " + serv.Name + " PulseWidth:" + serv.PulseWidth; 
                            //    //    //warningDiag.Show();
                            //    //    Console.WriteLine("Error PulseWidth of ServoMotor " + serv.Name + " PulseWidth:" + serv.PulseWidth);
                            //    //    return;
                            //    //}
                            //}
                            //stopWatch.Stop();
                            //TimeSpan ts = stopWatch.Elapsed;
                            // string elapsedTime = String.Format("{0:00}.{1:00}", ts.Seconds, ts.Milliseconds / 10);
                            //string elapsedTime = string.Format("{0} ms", stopWatch.Elapsed.Milliseconds);

                            TextTIME.Dispatcher.Invoke(
                                       System.Windows.Threading.DispatcherPriority.Normal,
                                       new Action(() => TextTIME.Text = string.Format("{0} ms", stopWatch.Elapsed.Milliseconds))
                                   );

                            if (listMotors[(int)MotorsNames.Turn].PulseWidthNormalized != -1 || listMotors[(int)MotorsNames.UpperNod].PulseWidthNormalized != -1)
                                SetMotors(listMotors, TimeSpan.FromMilliseconds(NeckTime));
                            else
                                SetMotors(listMotors, TimeSpan.FromMilliseconds(expressionTime));

                            stopWatch.Restart();

                        }
                        else
                        {
                            switch (receivedSetMotors)
                            {
                                case "none":
                                    break;
                                case "Yes":
                                    YesMovementButton_Click(this, new RoutedEventArgs());
                                    break;
                                case "No":
                                    NoMovementButton_Click(this, new RoutedEventArgs());
                                    break;
                                case "OpenEyes":
                                    OpenEyesButton_Click(this, new RoutedEventArgs());
                                    break;
                                case "CloseEyes":
                                    CloseEyesButton_Click(this, new RoutedEventArgs());
                                    break;
                                default:
                                    MessageBox.Show("Moviement Unknown");
                                    break;
                            }
                        }






                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("Error XML Set motors: " + exc.Message);
                    }


                }
            }
        }

        private void SendFeedBack()
        {

            while (true)
            {

                yarpPortFeedback.sendData(ComUtils.XmlUtils.Serialize<List<ServoMotor>>(FACEBody.CurrentMotorState));
                //Console.WriteLine(" Turn " + FACEBody.CurrentMotorState[(int)MotorsNames.Turn].PulseWidth);


            }
        }


        #endregion


        #region Net: UDP

        private void NetButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Image img = null;

            switch (visualMode)
            {
                case Mode.View:
                    ViewGrid.Visibility = System.Windows.Visibility.Hidden;
                    img = ViewButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceView.png")));
                    break;
                case Mode.Edit:
                    EditGrid.Visibility = System.Windows.Visibility.Hidden;
                    img = EditButton.Content as System.Windows.Controls.Image;
                    img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceEdit.png")));
                    break;
                case Mode.Net:
                    break;
                case Mode.ECS:
                    break;
                case Mode.Gamepad:
                    break;
                case Mode.Test:
                    TestGrid.Visibility = Visibility.Hidden;
                    //img = RecognitionTestButton.Content as System.Windows.Controls.Image;
                    //img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceTest.png")));
                    break;
            }

            //roba per yarp
            ServerIPTextbox.Text = ServerYarpConf();
            MyIPLabel.Content = LocalIP();

            NetGrid.Visibility = Visibility.Visible;
            visualMode = Mode.Net;
            img = NetButton.Content as System.Windows.Controls.Image;
            img.Source = new BitmapImage(new Uri(String.Format(@"pack://application:,,,/Images/Buttons/FaceNetPressed.png")));
        }

        private int count = 0;

        private void ListenUDPButton_Click(object sender, RoutedEventArgs e)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.125"), 5566);
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            ManualResetEvent wait = new ManualResetEvent(false);

            UdpClient listener = new UdpClient(localEndPoint);

            System.Threading.Thread receiver = new System.Threading.Thread(() =>
            {
                while (true)
                {
                    while (listener.Available != 0)
                    {
                        //wait.Set();
                        byte[] received = listener.Receive(ref remoteIpEndPoint);
                       // int code = Int32.Parse(Encoding.ASCII.GetString(received));
                       
                        System.Diagnostics.Debug.WriteLine("[" + count + "]" + Encoding.ASCII.GetString(received));
                        count++;

                    //    float neckIncr = 0.10f;
                    //    int neckTime = 300; // to be tested
                    //    float turnNeckUDP = 0, upperNodUDP = 0;

                    //    switch (code)
                    //    {
                    //        case 1:
                    //            LoadAndSendExpression(expressionsPath + "AU_Neutral.xml", FACEActions.Neutral);
                    //            break;
                    //        case 2:
                    //            LoadAndSendExpression(expressionsPath + "AU_Anger.xml", FACEActions.Neutral);
                    //            break;
                    //        case 3:
                    //            LoadAndSendExpression(expressionsPath + "AU_Disgust.xml", FACEActions.Neutral);
                    //            break;
                    //        case 4:
                    //            LoadAndSendExpression(expressionsPath + "AU_Fear.xml", FACEActions.Neutral);
                    //            break;
                    //        case 5:
                    //            LoadAndSendExpression(expressionsPath + "AU_Happiness.xml", FACEActions.Neutral);
                    //            break;
                    //        case 6:
                    //            LoadAndSendExpression(expressionsPath + "AU_Sadness.xml", FACEActions.Neutral);
                    //            break;
                    //        case 7:
                    //            LoadAndSendExpression(expressionsPath + "AU_Surprise.xml", FACEActions.Neutral);
                    //            break;
                    //        case 8:
                    //            turnNeckUDP = FACEBody.CurrentMotorState[(int)MotorsNames.Turn].PulseWidth;
                    //            if (turnNeckUDP >= neckIncr)
                    //                turnNeckUDP -= neckIncr;
                    //            FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.Turn, turnNeckUDP, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
                    //            break;
                    //        case 9:
                    //            turnNeckUDP = FACEBody.CurrentMotorState[(int)MotorsNames.Turn].PulseWidth;
                    //            if (turnNeckUDP <= 1 - neckIncr)
                    //                turnNeckUDP += neckIncr;
                    //            FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.Turn, turnNeckUDP, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
                    //            break;
                    //        case 10:
                    //            upperNodUDP = FACEBody.CurrentMotorState[(int)MotorsNames.UpperNod].PulseWidth;
                    //            if (upperNodUDP >= neckIncr)
                    //                upperNodUDP -= neckIncr;
                    //            FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.UpperNod, upperNodUDP, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
                    //            break;
                    //        case 11:
                    //            upperNodUDP = FACEBody.CurrentMotorState[(int)MotorsNames.UpperNod].PulseWidth;
                    //            if (upperNodUDP <= 1 - neckIncr)
                    //                upperNodUDP += neckIncr;
                    //            FACEBody.ExecuteSingleMovement((int)FaceTrackingControls.UpperNod, upperNodUDP, TimeSpan.FromMilliseconds(neckTime), 10, TimeSpan.FromMilliseconds(0));
                    //            break;
                    //    }
                    }

                    //wait.WaitOne();

                }
            });
            receiver.Start();
        }

        #endregion

        #region Net: YARP
       
        /// <summary>
        /// Read server IP address from yarp.conf file
        /// </summary>
        /// <returns>The current server IP address</returns>
        private string ServerYarpConf()
        {
            string line = "";
            try
            {
                StreamReader sr = new StreamReader(path_yarp);
                line = sr.ReadLine();
                sr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }

            return line.Split(' ')[0];
        }

        /// <summary>
        /// Write the server IP address in the textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveYarpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StreamWriter sw = new StreamWriter(path_yarp);
                sw.WriteLine(ServerIPTextbox.Text + " 10000" + " yarp");
                sw.Close();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception: " + exc.Message);
            }

        }

        /// <summary>
        /// Read the local IP address
        /// </summary>
        /// <returns>The local IP address of this machine</returns>
        private string LocalIP()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine(addr);
                    return addr.ToString();
                }
            }
            return "";
        }


        private void CheckStatusYarp(object source, ElapsedEventArgs e)
        {


            if (yarpPortSetMotors.PortExists(setmotors_out) && colMot == "red")
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { Ellexp.Fill = Brushes.Green; }));
                colMot = "green";
            }
            else if (!yarpPortSetMotors.PortExists(setmotors_out) && colMot == "green")
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { Ellexp.Fill = Brushes.Red; }));
                colMot = "red";
            }

            if (yarpPortSetMotors.NetworkExists() && colYarp == "red")
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { Ellyarp.Fill = Brushes.Green; BarEllyarp.Fill = Brushes.Green; }));
                colYarp = "green";
            }
            else if (!yarpPortSetMotors.NetworkExists() && colYarp == "green")
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { Ellyarp.Fill = Brushes.Red; BarEllyarp.Fill = Brushes.Red; }));
                colYarp = "red";
            }
        }


        //private void YarpActivation_Checked(object sender, RoutedEventArgs e)
        //{
        //    TimerCheckStatusYarp.Start();

        //    InitCheckStatusYarp();

        //    SaveYarpButton.IsEnabled = false;
        //    ServerIPTextbox.IsEnabled = false;
        //}

        //private void YarpActivation_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    TimerCheckStatusYarp.Stop();
        //    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate() { EllAttention.Fill = Brushes.Red; Ellexp.Fill = Brushes.Red; Ellyarp.Fill = Brushes.Red; }));
        //    colYarp = "red";
        //    colAtt = "red";
        //    colExp = "red";
        //    SaveYarpButton.IsEnabled = true;
        //    ServerIPTextbox.IsEnabled = true;
        //    YarpDisconnect();
        //}

        private void YarpDisconnect()
        {
            if (TimerCheckStatusYarp != null)
                TimerCheckStatusYarp.Stop();

            if (yarpReceverSetMotorTimer != null)
                yarpReceverSetMotorTimer.Stop();

            if (yarpPortSetMotors.PortExists(setmotors_out))
                yarpPortSetMotors.Disconect(setmotors_in, setmotors_out);
           
        }

        
        #endregion




        private void SetMotors(List<ServoMotor> ListMotors, TimeSpan t )
        {
            FACEMotion motionToTest=new FACEMotion(ListMotors, t);
          

            try
            {
                //SBInfoBox.Text = "Testing expression..";
                FACEBody.ExecuteMotion(motionToTest);
                //StartProgressbarTime(expressionConfig.Time);

                //// Trovo tutti quelli a -1 e aggiorno con il vecchio valore
                //foreach (ServoMotor sm in ListMotors.FindAll(a => a.PulseWidth == -1.0))
                //    ListMotors.Find(a => a.Channel == sm.Channel).PulseWidth = currentSmState.Find(a => a.Channel == sm.Channel).PulseWidth;


                //currentSmState = ListMotors;

                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                {
                    UpdateSliders(ListMotors);
                }));
                   
            }
            catch (FACException fEx)
            {
                SBInfoBox.Text = "";
                //SBProgressBar.Value = SBProgressBar.Minimum;

                TextError.Text = "Warning! " + fEx.Message;
                WarningDialog warningDiag = new WarningDialog();
                warningDiag.tbInstructionText.Text = fEx.Message;
                warningDiag.Show();
            }


        }

        private bool yarpExpressionOn = false;

        private void CheckboxYarpExp_Checked(object sender, RoutedEventArgs e)
        {
            yarpExpressionOn = true;
        }

        private void CheckboxYarpExp_Unchecked(object sender, RoutedEventArgs e)
        {
            yarpExpressionOn = false;
        }

        

   

       


    }



}



//ConvertTimestamp();

//1366646127.863000 17:55:27.8650

//DateTime n = DateTime.Now;
//double ndouble = FromDateToDouble(n);
//DateTime nn = FromDoubleToDate(ndouble);

//DateTime dt1 = new DateTime(2013, 04, 22, 17, 55, 27, 865); //FromDoubleToDate(1355413493.422000);
//DateTime dt2 = FromDoubleToDate(1366646138.498000);
////dt = dt.AddSeconds(30);
//string s = String.Format("{0:0.000000}", FromDateToDouble(dt2)).Replace(",", ".");
//String.Format("{0:HH:mm:ss.ffff}", DateTime.Now);

//List<FACEMotion> motions = new List<FACEMotion>(2);
//if (Directory.Exists("Animations\\"))
//{
//    FACEMotion m1 = FACEMotion.LoadFromXmlFormat(motionsPath + "TurnDown.xml");
//    motions.Add(m1);
//    FACEMotion m2 = FACEMotion.LoadFromXmlFormat(motionsPath + "TurnUp.xml");
//    motions.Add(m2);

//    FACEAnimation anim = new FACEAnimation(motions, 2, 0);
//    anim.Name = "Yes";
//    FACEAnimation.SaveAsXmlFormat(anim, "Animations\\Yes.xml");
//}