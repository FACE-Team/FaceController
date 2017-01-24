using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Threading;

namespace FACEBodyControl
{
    public enum Direction  {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

        
    //public class ObjectInfo
    //{
    //    private List<Rect> facesList;
    //    public List<Rect> FacesList
    //    {
    //        get { return facesList; }
    //        set { facesList = value; }
    //    }

    //    private BrainModules module;
    //    public BrainModules Module
    //    {
    //        get { return module; }
    //        set { module = value; }
    //    }

    //    public ObjectInfo(List<Rect> l, BrainModules mod)
    //    {
    //        facesList = l;
    //        module = mod;
    //    }
    //}


    //public class Percept
    //{
    //    private ObjectInfo objInfo;
    //    public ObjectInfo ObjInfo
    //    {
    //        get { return objInfo; }
    //        set { objInfo = value; }
    //    }

    //    public Percept(ObjectInfo info) 
    //    {
    //        objInfo = info;
    //    }
    //}


    //public class WarningEventArgs : EventArgs
    //{
    //    public WarningEventArgs(FACException fe)
    //    {
    //        fException = fe;
    //    }

    //    public FACException fException
    //    {
    //        get;
    //        private set;
    //    }
    //}


    public static class Body
    {
        # region DllImport

        [DllImport("highgui200d.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr cvCreateCameraCapture(int index);

        [DllImport("highgui200d.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void cvReleaseCapture(ref IntPtr capture);

        [DllImport("FACETracking2.0.dll", EntryPoint = "DetectFace", CallingConvention = CallingConvention.StdCall)]
        //public static extern int DetectFace(out int xImg, out int yImg, out int wImg, out int hImg);
        //public static extern int DetectFace(out int x, out int y, out int width, out int height, out int wFrame, out int hFrame);
        private static extern int DetectFace();

        [DllImport("FACETracking2.0.dll", EntryPoint = "InitTracking", CallingConvention = CallingConvention.StdCall)]
        private static extern int InitTracking(IntPtr ptr);

        [DllImport("FACETracking2.0.dll", EntryPoint = "StopTracking", CallingConvention = CallingConvention.StdCall)]
        private static extern void StopTracking();

        [DllImport("FACETracking2.0.dll", EntryPoint = "GetFaces", CallingConvention = CallingConvention.StdCall)]
        private static extern void GetFaces(out FaceInfoSet faces);

        //TO TEST
        //[DllImport("FACETracking2.0.dll", EntryPoint = "DetectFace2", CallingConvention = CallingConvention.StdCall)]
        ////public static extern int DetectFace(out int xImg, out int yImg, out int wImg, out int hImg);
        ////public static extern int DetectFace(out int x, out int y, out int width, out int height, out int wFrame, out int hFrame);
        //private static extern int DetectFace2();

        //[DllImport("FACETracking2.0.dll", EntryPoint = "InitTracking2", CallingConvention = CallingConvention.StdCall)]
        //private static extern int InitTracking2(IntPtr ptr);

        //[DllImport("FACETracking2.0.dll", EntryPoint = "StopTracking2", CallingConvention = CallingConvention.StdCall)]
        //private static extern void StopTracking2();

        //[DllImport("FACETracking2.0.dll", EntryPoint = "GetFaces2", CallingConvention = CallingConvention.StdCall)]
        //private static extern void GetFaces2(out FaceInfoSet faces);

        #endregion


        #region Members

        private static IntPtr camPtr;
        private static Percept perception;
        private static ECS ecs;
        public static event EventHandler<WarningEventArgs> VideoCamProblem;        
        private static TimeSpan motorInterval = new TimeSpan(400000); // 10 milliseconds

        private static ServoMotorGroup currentState;
        public static ServoMotorGroup CurrentState
        {
            get { return currentState; }
        }
        
        private static ServoMotorGroup defaultState;
        public static ServoMotorGroup DefaultState
        {
            get { return defaultState; }
        }

        private static int eyeState = -1; //0=open, 1=close
        public static int EyeState
        {
            get { return eyeState; }
            set { eyeState = value; }
        }

        //public static float eyeLidsUpper = currentState.ServoMotorsList[(int)MotorsNames.EyeLidsUpper].PulseWidth;
        //public static float eyeLidsLower = currentState.ServoMotorsList[(int)MotorsNames.EyeLidsLower].PulseWidth;

        #endregion

        
        #region Struct

        public struct FaceInfoSet
        {
            public int numberFaceInfo;
            public IntPtr faceInfo;
            public int wFrame;
            public int hFrame;
        };

        public struct FaceInfo
	    {
	        public int x;
            public int y;
            public int width;
            public int height;
	    };

        #endregion


        #region Constants from interface

        //private static float kNeckTurn = 0.05f;
        //public static float KNeckTurn
        //{
        //    get { return kNeckTurn; }
        //    set { kNeckTurn = value; }
        //}

        //private static float kNeckTilt = 0.05f;
        //public static float KNeckTilt
        //{
        //    get { return kNeckTilt; }
        //    set { kNeckTilt = value; }
        //}

        private static float kEyesTurn = 0.05f;
        public static float KEyesTurn
        {
            get { return kEyesTurn; }
            set { kEyesTurn = value; }
        }
        
        private static double rate = 1; // blink/sec
        public static double Rate
        {
            get { return rate; }
            set { rate = value; }
        }

        private static int closedEyesTime = 1000; // milliseconds
        public static int ClosedEyesTime
        {
            get { return closedEyesTime; }
            set { closedEyesTime = value; }
        }

        private static int speedEyesTime = 1000; // milliseconds
        public static int SpeedEyesTime
        {
            get { return speedEyesTime; }
            set { speedEyesTime = value; }
        }

        #endregion


        #region Notification

        private static void NotifyWebcamProblems(string msg)
        {
            if (VideoCamProblem != null)
                VideoCamProblem(Thread.CurrentThread, new WarningEventArgs(new FACException("Your webcam device is not detected.")));
        }

        #endregion


        #region Perception

        internal static Percept GetPerception()
        {
            return perception;
        }

        internal static void ResetBody()
        {
            perception = null;
        }

        #endregion


        #region ECS Initialization

        public static void ECSInit(string filename)
        {
            if (ecs == null)
                ecs = ECS.LoadFromXmlFormat(filename);            
        }

        #endregion


        #region Camera

        private static DispatcherTimer videoTimer = new DispatcherTimer();

        public static void StartCam()
        {   
            if(Webcams.WebcamId != -1)
            {
                videoTimer.Interval = new TimeSpan(100000); // 1/100 seconds
                
                try
                {
                    camPtr = cvCreateCameraCapture(Webcams.WebcamId);
                    if (camPtr != IntPtr.Zero)
                    {
                        InitTracking(camPtr);
                        //InitTracking2(_ptr);
                        
                        videoTimer.Tick += new EventHandler(delegate(object s, EventArgs a)
                        {
                            DetectFace();
                            //DetectFace2();
                        });

                        videoTimer.Start();
                    }                
                }
                catch
                {
                    StopCam();
                    if (VideoCamProblem != null)
                    {
                        string err = "Some problems occurs with your webcam device.";
                        Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => NotifyWebcamProblems(err)), null);
                    }
                }
            }
            else
            {
                StopCam();
                if (VideoCamProblem != null)
                {
                    string warning = "Your webcam device is not detected.";
                    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => NotifyWebcamProblems(warning)), null);
                }
            }
        }
                
        public static void StopCam()
        {            
            videoTimer.Stop();
            cvReleaseCapture(ref camPtr);
            StopTracking();
            //StopTracking2();
        }

        #endregion


        #region Face Tracking

        private static DispatcherTimer faceTrackTimer = new DispatcherTimer();

        public static void PositionTracker() {

            if(Webcams.WebcamId != -1)
            {
                //CreateAndShowMainWindow();

                faceTrackTimer.Interval = new TimeSpan(100000); // 1/100 seconds

                // Normalized rect values
                //int xImg, yImg, wImg, hImg, wFrame, hFrame;
                FaceInfoSet facesSet;

                try
                {
                    camPtr = cvCreateCameraCapture(Webcams.WebcamId); // passing index by interface
                    if (camPtr != IntPtr.Zero)
                    {
                        InitTracking(camPtr);
                        //InitTracking2(_ptr);

                        //while(tracking)
                        faceTrackTimer.Tick += new EventHandler(delegate(object s, EventArgs a)
                        {
                            DetectFace();
                            //DetectFace2();

                            // Marshall struct
                            GetFaces(out facesSet);
                            //GetFaces2(out facesSet);

                            int structSize = Marshal.SizeOf(typeof(FaceInfo));
                            List<FaceInfo> facesList = new List<FaceInfo>();
                            IntPtr ptr = facesSet.faceInfo;

                            for (int i = 0; i < facesSet.numberFaceInfo; i++)
                            {
                                facesList.Add((FaceInfo)Marshal.PtrToStructure(ptr, typeof(FaceInfo)));
                                ptr = (IntPtr)((int)ptr + structSize);
                            }
                            
                            // Normalize faces list
                            List<Rect> list = new List<Rect>();
                            foreach (FaceInfo f in facesList)
                            {
                                double xImgNorm = (f.x * 0.5) / (facesSet.wFrame / 2);
                                double yImgNorm = (f.y * 0.5) / (facesSet.hFrame / 2);
                                double wImgNorm = (f.width * 0.5) / (facesSet.wFrame / 2);
                                double hImgNorm = (f.height * 0.5) / (facesSet.hFrame / 2);
                                if (xImgNorm > 0 && yImgNorm > 0 && wImgNorm > 0 && hImgNorm > 0)
                                    list.Add(new Rect(xImgNorm, yImgNorm, wImgNorm, hImgNorm));
                            }

                            ObjectInfo obj = new ObjectInfo(list, BrainModules.FaceTrack);
                            perception = new Percept(obj);


                            /********************************* TO DELETE *********************************/
                            //TextBlock tx = (TextBlock)myCanvas.Children[3];
                            //tx.Text = "Number of faces: " + facesSet.numberFaceInfo;

                            //Ellipse myEllipse = new Ellipse();
                            //myEllipse.Stroke = Brushes.Black;
                            //myEllipse.Fill = Brushes.DarkBlue;
                            //myEllipse.Width = 3;
                            //myEllipse.Height = 3;

                            ////seleziona la faccia più grande per disegnare l'ovale
                            //if (list.Count != 0)
                            //{
                            //    Rect max = new Rect(new Size(0, 0));
                            //    foreach (Rect r in list)
                            //    {
                            //        if ((r.Width * r.Height) > (max.Width * max.Height))
                            //            max = r;
                            //    }

                            //    //normalizza le coordinate
                            //    double xNorm = (max.X * 0.5) / (facesSet.wFrame / 2);
                            //    double yNorm = (max.Y * 0.5) / (facesSet.hFrame / 2);
                            //    double wNorm = (max.Width * 0.5) / (facesSet.wFrame / 2);
                            //    double hNorm = (max.Height * 0.5) / (facesSet.hFrame / 2);

                            //    Canvas.SetTop(myEllipse, (yNorm + (hNorm / 2)) * facesSet.hFrame);
                            //    Canvas.SetLeft(myEllipse, (xNorm + (wNorm / 2)) * facesSet.wFrame);

                            //    /*
                            //    double deltaX = (xImgNorm + (wImgNorm / 2)) - 0.5;
                            //    double deltaY = (yImgNorm + (hImgNorm / 2)) - 0.5;

                            //    TextBlock tx = (TextBlock)myCanvas.Children[0];
                            //    tx.Text = ""+deltaX;

                            //    TextBlock ty = (TextBlock)myCanvas.Children[1];
                            //    ty.Text = "" + deltaY;
                            //    */

                            //    myCanvas.Children.RemoveAt(6);
                            //    myCanvas.Children.Add(myEllipse);
                            //    /********************************* TO DELETE *********************************/
                            //}
                        });

                        faceTrackTimer.Start();
                    }
                }
                catch
                {
                    StopCam();
                    if (VideoCamProblem != null)
                    {
                        string err = "Some problems occurs with your webcam device.";
                        Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => NotifyWebcamProblems(err)), null);
                    }
                }
            }
            else
            {
                StopCam();
                if (VideoCamProblem != null)
                {
                    string warning = "Your webcam device is not detected.";
                    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => NotifyWebcamProblems(warning)), null);
                }
            }
        }

        public static void StopPositionTracker()
        {
            if (faceTrackTimer.IsEnabled)
            {
                faceTrackTimer.Stop();
                StopTracking();
                //StopTracking2();
                cvReleaseCapture(ref camPtr);                
            }

            /***************** TO DELETE *****************/
            //if(mainWindow != null)
            //    mainWindow.Close();
            /***************** TO DELETE *****************/
        }

        #endregion


        #region Eyes Blinking

        /// <summary>
        /// Start blinking routine.
        /// </summary>
        public static void StartBlinking()
        {
            ServoMotorGroup smgClose = LoadExpression(@"Movements\ClosedEyes.xml");
            smgClose.Time = speedEyesTime;
            ServoMotorGroup smgOpen = LoadExpression(@"Movements\OpenedEyes.xml");
            smgOpen.Time = speedEyesTime;

            TimeSpan closeDur = new TimeSpan(smgClose.Time * (int)Math.Pow(10, 4)); // seconds

            MotorTask mClose = new MotorTask(TaskType.Blinking, DateTime.Now, closeDur, motorInterval, smgClose, 10);            
            AnimationEngine.AddTask(mClose, mClose.Start);

            TimeSpan wait = new TimeSpan(closedEyesTime * (int)Math.Pow(10, 4)); //1 second
            TimeSpan openDur = new TimeSpan(smgOpen.Time * (int)Math.Pow(10, 4)); // seconds

            MotorTask mOpen = new MotorTask(TaskType.Blinking, DateTime.Now + closeDur + wait, openDur, motorInterval, smgOpen, 10);
            AnimationEngine.AddTask(mOpen, mOpen.Start);
        }

        /// <summary>
        /// Stop blinking routine.
        /// </summary>
        public static void StopBlinking()
        {
            AnimationEngine.RemoveAllTasks(TaskType.Blinking);
        }

        #endregion


        #region Yes/No Movement

        public static void StartYesMovement()
        {           
            ServoMotorGroup smgDown = LoadExpression(@"Movements\MoveDown.xml");   
            ServoMotorGroup smgUp = LoadExpression(@"Movements\MoveUp.xml");
            int neckSpeed = smgDown.Time;

            TimeSpan dur = new TimeSpan(neckSpeed * (int)Math.Pow(10, 4)); // seconds

            MotorTask mDown = new MotorTask(TaskType.Yes, DateTime.Now, dur, motorInterval, smgDown, 10);
            AnimationEngine.AddTask(mDown, mDown.Start);            

            TimeSpan wait = new TimeSpan(2 * (int)Math.Pow(10, 6)); //1/10 seconds

            MotorTask mUp = new MotorTask(TaskType.Yes, DateTime.Now + dur + wait, dur, motorInterval, smgUp, 10);
            AnimationEngine.AddTask(mUp, mUp.Start);
        }

        public static void StartNoMovement()
        {
            ServoMotorGroup smgLeft = LoadExpression(@"Movements\MoveLeft.xml");
            ServoMotorGroup smgRight = LoadExpression(@"Movements\MoveRight.xml");
            int neckSpeed = smgLeft.Time;

            TimeSpan dur = new TimeSpan(neckSpeed * (int)Math.Pow(10, 4)); // seconds

            MotorTask mLeft = new MotorTask(TaskType.Yes, DateTime.Now, dur, motorInterval, smgLeft, 10);
            AnimationEngine.AddTask(mLeft, mLeft.Start);

            TimeSpan wait = new TimeSpan(2 * (int)Math.Pow(10, 6)); //1/10 seconds

            MotorTask mRight = new MotorTask(TaskType.Yes, DateTime.Now + dur + wait, dur, motorInterval, smgRight, 10);
            AnimationEngine.AddTask(mRight, mRight.Start);
        }

        #endregion


        #region Pleasure/Arousal 

        //public static void ApplayECS(float pleasure, float arousal, int duration)
        //{            
        //    List<ServoMotor> smList = currentState.ServoMotorsList;
        //    ecs.SetServoMotorList(smList, pleasure, arousal);

        //    ServoMotorGroup smg = new ServoMotorGroup(smList, duration);
        //    //FACExpression expr = new FACExpression(smg, DateTime.Now, 10);
        //    ExecuteMovement(smg, 10, DateTime.Now);
        //}

        #endregion


        #region UDP Connection
       
        //public static void InitUDPConnection(int localPort, System.Net.IPAddress remoteIP)
        //{
        //    UDPConnection udp = new UDPConnection(localPort);
        //    udp.ListenOnUDPConnection(remoteIP);            
        //}

        #endregion


        #region Execute Movement

        /// <summary>
        /// Execute a movement saved in a xml file.
        /// </summary>
        /// <param name="filename">The name of the xml file to be loaded</param>
        /// <param name="priority">Priority of the movement</param>
        public static void ExecuteFileMovement(string filename, int priority)
        {
            ServoMotorGroup smg = LoadExpression(filename);
            ExecuteMovement(smg, priority, DateTime.Now);
        }

        /// <summary>
        /// Execute a movement saved in a xml file.
        /// </summary>
        /// <param name="filename">The name of the xml file to be loaded</param>
        /// <param name="priority">Priority of the movement</param>
        /// <param name="duration">Duration of the movement</param>
        public static void ExecuteFileMovement(string filename, int priority, int duration)
        {
            ServoMotorGroup smg = LoadExpression(filename);
            smg.Time = duration;
            ExecuteMovement(smg, priority, DateTime.Now);
        }

        /// <summary>
        /// Execute a movement.
        /// </summary>
        /// <param name="smg">Motor configuration to be executed</param>
        /// <param name="priority">Priority of the movement</param>
        /// <param name="when">Start time of the movement</param>
        public static void ExecuteMovement(ServoMotorGroup smg, int priority, DateTime when)
        {
            TimeSpan duration = new TimeSpan(smg.Time * (int)Math.Pow(10, 4));
            MotorTask m = new MotorTask(TaskType.Motor, when, duration, motorInterval, smg, priority);
            AnimationEngine.AddTask(m, m.Start);
        }

        /// <summary>
        /// Execute a single motor movement.
        /// </summary>
        /// <param name="ch">Number of the channel</param>
        /// <param name="pulse">Amount of the movement between 0 and 1</param>
        /// <param name="time">Duration of the movement</param>
        /// <param name="priority">Priority of the movement</param>
        /// <param name="when">Start time of the movement</param>
        public static void ExecuteSingleMovement(int ch, float pulse, int time, int priority, DateTime when)
        {
            ServoMotorGroup smg = new ServoMotorGroup(currentState.ServoMotorsList.Count);
            smg.ServoMotorsList[ch].PulseWidth = pulse;
            smg.Time = time;

            TimeSpan duration = new TimeSpan(smg.Time * (int)Math.Pow(10, 4));

            MotorTask m = new MotorTask(TaskType.Motor, when, duration, motorInterval, smg, priority);
            AnimationEngine.AddTask(m, m.Start);
        }

        #endregion


        #region Neck/Eyes Movements

        /// <summary>
        /// Turn neck horizontally
        /// </summary>
        /// <param name="dir">Direction of the movement</param>
        /// <param name="amount">Displacement of the movement</param>
        /// <param name="timeNow">When the movement is executed</param>
        public static void TurnNeck(Direction dir, float amount, DateTime when) 
        {
            // mapping amount on absolute value of motor displacements
            //float move = amount * kNeckTurn;
            float move = amount;

            // send displacement to Animation
            if(dir == Direction.Right)
                UpdateState(Convert.ToInt32(MotorsNames.Turn, CultureInfo.InvariantCulture), -move, when);
            else
                UpdateState(Convert.ToInt32(MotorsNames.Turn, CultureInfo.InvariantCulture), move, when);
        }

        /// <summary>
        /// Move neck up and down
        /// </summary>
        /// <param name="dir">Direction of the movement</param>
        /// <param name="amount">Displacement of the movement</param>
        /// <param name="when">When the movement is executed</param>
        public static void TiltNeck(Direction dir, float amount, DateTime when) 
        {
            // mapping amount on absolute value of motor displacements
            //float move = amount * kNeckTilt;
            float move = amount;

            // send displacement to Animation
            if (dir == Direction.Up)
                UpdateState(Convert.ToInt32(MotorsNames.UpperNod, CultureInfo.InvariantCulture), move, when);
            else
                UpdateState(Convert.ToInt32(MotorsNames.UpperNod, CultureInfo.InvariantCulture), -move, when);
        }
        
        /// <summary>
        /// Turn eyes hoizontally
        /// </summary>
        /// <param name="dir">Direction of the movement</param>
        /// <param name="amount">Displacement of the movement</param>
        /// <param name="when">When the movement is executed</param>
        public static void TurnEyes(Direction dir, float amount, DateTime when)
        {
            // mapping amount on absolute value of motor displacements
            //float move = amount * kEyesTurn;
            float move = amount;

            // send displacement to Animation
            if (dir == Direction.Right)
            {
                UpdateState(Convert.ToInt32(MotorsNames.EyeTurnRight, CultureInfo.InvariantCulture), move, when);
                UpdateState(Convert.ToInt32(MotorsNames.EyeTurnLeft, CultureInfo.InvariantCulture), move, when);
            }
            else
            {
                UpdateState(Convert.ToInt32(MotorsNames.EyeTurnRight, CultureInfo.InvariantCulture), -move, when);
                UpdateState(Convert.ToInt32(MotorsNames.EyeTurnLeft, CultureInfo.InvariantCulture), -move, when);
            }
        }


        //TimeSpan interval = new TimeSpan(400000); // 40 ms = 25fps
        private static void UpdateState(int index, float delta, DateTime when)
        {
            ServoMotorGroup smg = new ServoMotorGroup(currentState.ServoMotorsList.Count);
            smg.Time = 350;  // <----------------------- CONTROL -----------------------  set time from interface ???

            if ((currentState.ServoMotorsList.ElementAt(index).PulseWidth + delta) > 1)
                smg.ServoMotorsList.ElementAt(index).PulseWidth = 1;
            else if ((currentState.ServoMotorsList.ElementAt(index).PulseWidth + delta) < 0)
                smg.ServoMotorsList.ElementAt(index).PulseWidth = 0;
            else
                smg.ServoMotorsList.ElementAt(index).PulseWidth = currentState.ServoMotorsList.ElementAt(index).PulseWidth + delta;

            double[] from = currentState.GetValues();
            double[] to = smg.GetValues();

            TimeSpan dur = new TimeSpan(smg.Time * (int)Math.Pow(10, 4));
            //MotorTask m = new MotorTask(TaskType.Motor, when, dur, motorInterval, from, to, 8); ;
            MotorTask m = new MotorTask(TaskType.Motor, when, dur, motorInterval, smg, 8); ;
            AnimationEngine.AddTask(m, m.Start);
        }

        #endregion


        #region Load/Save configurations

        public static ServoMotorGroup LoadExpression(string filename)
        {
            try
            {
                ServoMotorGroup expression = ServoMotorGroup.LoadFromXmlFormat(filename);
                for (int i = 0; i < expression.ServoMotorsList.Count; i++)
                {
                    expression.ServoMotorsList.ElementAt(i).MinValue = defaultState.ServoMotorsList.ElementAt(i).MinValue;
                    expression.ServoMotorsList.ElementAt(i).MaxValue = defaultState.ServoMotorsList.ElementAt(i).MaxValue;
                }
                return expression;
            }
            catch
            {
                throw;
            }
        }

        public static void SaveExpression(object objGraph, string fileName)
        {
            try
            {
                ServoMotorGroup.SaveAsXmlFormat(objGraph, fileName);
            }
            catch
            {
                throw;
            }
        }


        public static ServoMotorGroup LoadConfigFile(string filename)
        {
            try
            {
                defaultState = ServoMotorGroup.LoadFromXmlFormat(filename);
                currentState = defaultState;
                return currentState;
            }
            catch
            {
                throw;
            }
        }

        public static void SaveConfigFile(object objGraph, string fileName)
        {
            try
            {
                ServoMotorGroup.SaveAsXmlFormat(objGraph, fileName);
                defaultState = objGraph as ServoMotorGroup;
            }
            catch
            {
                throw;
            }
        }

        #endregion

    }
}
