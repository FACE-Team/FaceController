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
using Pololu.Usc;
using Pololu.UsbWrapper;

namespace FACEBodyControl
{
    public class Percept
    {
        private ObjectInfo objInfo;
        public ObjectInfo ObjInfo
        {
            get { return objInfo; }
            set { objInfo = value; }
        }

        public Percept(ObjectInfo info)
        {
            objInfo = info;
        }
    }


    public class WarningEventArgs : EventArgs
    {
        public WarningEventArgs(FACException fe)
        {
            fException = fe;
        }

        public FACException fException
        {
            get;
            private set;
        }
    }


    public class ObjectInfo
    {
        private List<Rect> facesList;
        public List<Rect> FacesList
        {
            get { return facesList; }
            set { facesList = value; }
        }

        //private BrainModules module;
        //public BrainModules Module
        //{
        //    get { return module; }
        //    set { module = value; }
        //}

        //public ObjectInfo(List<Rect> l, BrainModules mod)
        //{
        //    facesList = l;
        //    module = mod;
        //}
    }


    public static class FACEBody
    {
        #region Members

        public const int NUMBER_OF_MOTORS = 32;

        /// <summary>
        /// Minimum value of the servo motor.
        /// </summary>
        private static int minLimit = 500;
        public static int MinLimit
        {
            get { return minLimit; }
            set { minLimit = value; }
        }

        /// <summary>
        /// Maximum value of the servo motor.
        /// </summary>
        private static int maxLimit = 2500;
        public static int MaxLimit
        {
            get { return maxLimit; }
            set { maxLimit = value; }
        }

        private static TimeSpan motorInterval = new TimeSpan(400000); // 4 milliseconds
        public static TimeSpan MotorInterval
        {
            get { return motorInterval; }
        }

        private static List<ServoMotor> currentMotorState;
        public static List<ServoMotor> CurrentMotorState
        {
            get { return currentMotorState; }
        }

        private static List<ServoMotor> defaultMotorState;
        public static List<ServoMotor> DefaultMotorState
        {
            get { return defaultMotorState; }
        }

        private static int eyeState = -1; //0=open, 1=close
        public static int EyeState
        {
            get { return eyeState; }
            set { eyeState = value; }
        }

        #endregion


        #region Constants from interface

        private static float kEyesTurn = 0.05f;
        public static float KEyesTurn
        {
            get { return kEyesTurn; }
            set { kEyesTurn = value; }
        }

        private static int blinkingRate = 10; // blink/sec
        public static int BlinkingRate
        {
            get { return blinkingRate; }
            set { blinkingRate = value; }
        }

        private static int closedEyesTime = 100; // milliseconds
        public static int ClosedEyesTime
        {
            get { return closedEyesTime; }
            set { closedEyesTime = value; }
        }

        private static int speedEyesTime = 100; // milliseconds
        public static int SpeedEyesTime
        {
            get { return speedEyesTime; }
            set { speedEyesTime = value; }
        }


        private static int respirationRate = 8; // blink/sec
        public static int RespirationRate
        {
            get { return respirationRate; }
            set { respirationRate = value; }
        }

        private static int respirationSpeed = 3500; // milliseconds
        public static int RespirationSpeed
        {
            get { return respirationSpeed; }
            set { respirationSpeed = value; }
        }

        

        #endregion


        #region Perception

        private static Percept perception;

        internal static Percept GetPerception()
        {
            return perception;
        }

        internal static void ResetBody()
        {
            perception = null;
        }

        #endregion


        #region Eyes Blinking

        /// <summary>
        /// Start blinking routine.
        /// </summary>
        public static void StartBlinking()
        {
            FACEAnimation animation = FACEAnimation.LoadFromXmlFormat("Animations\\Blinking.xml");
            animation.RepeatFrequency = (int)((60 / blinkingRate) * 1000);
            FACEBody.ExecuteAnimation(animation, MotorTaskType.Blinking);
        }

        /// <summary>
        /// Stop blinking routine.
        /// </summary>
        public static void StopBlinking()
        {
            AnimationEngine.RemoveAllTasks(MotorTaskType.Blinking);
        }


        //public static void ChangeBlinkingFrequency(int freq)
        //{
        //    AnimationEngine.RemoveAllTasks(MotorTaskType.Blinking);
        //    if (freq > 0)
        //        StartBlinking(freq);
        //}

        #endregion


        #region Respiration

        /// <summary>
        /// Start respiration routine.
        /// </summary>
        public static void StartRespiration()
        {
            FACEAnimation animation = FACEAnimation.LoadFromXmlFormat("Animations\\Respiration.xml");
            animation.RepeatFrequency = (int)((60 / respirationRate) * 1000);
            FACEBody.ExecuteAnimation(animation, MotorTaskType.Respiration);
        }

        /// <summary>
        /// Stop respiration routine.
        /// </summary>
        public static void StopRespiration()
        {
            AnimationEngine.RemoveAllTasks(MotorTaskType.Respiration);
        }

        #endregion


        #region Yes/No movement

        public static void StartYesMovement()
        {
            FACEAnimation animation = FACEAnimation.LoadFromXmlFormat("Animations\\Yes.xml");            
            FACEBody.ExecuteAnimation(animation, MotorTaskType.Yes);
        }

        public static void StopYesMovement()
        {
            AnimationEngine.RemoveAllTasks(MotorTaskType.Yes);
        }

        public static void StartNoMovement()
        {
            FACEAnimation animation = FACEAnimation.LoadFromXmlFormat("Animations\\No.xml");
            FACEBody.ExecuteAnimation(animation, MotorTaskType.No);
        }

        public static void StopNoMovement()
        {
            AnimationEngine.RemoveAllTasks(MotorTaskType.No);
        }
        #endregion


        #region Execute Movement

        /// <summary>
        /// Execute a movement saved in a xml file.
        /// </summary>
        /// <param name="filename">The name of the xml file to be loaded</param>
        /// <param name="priority">Priority of the movement</param>
        public static void ExecuteFile(string filename, int priority)
        {
            FACEMotion motion = FACEMotion.LoadFromXmlFormat(filename);
            //motion.Duration = 1500;  // DA CANCELLARE
            DateTime when = DateTime.Now.Add(TimeSpan.FromMilliseconds(motion.DelayTime));
            MotorTask m = new MotorTask(MotorTaskType.Generic, motion, when, 1, 0);
            AnimationEngine.AddTask(m, m.Start);
        }

        /// <summary>
        /// Execute a movement.
        /// </summary>
        /// <param name="smg">Motor configuration to be executed</param>
        /// <param name="priority">Priority of the movement</param>
        /// <param name="when">Start time of the movement</param>
        public static void ExecuteMotion(FACEMotion motion)
        {
            DateTime when = DateTime.Now.Add(TimeSpan.FromMilliseconds(motion.DelayTime));
            MotorTask m = new MotorTask(MotorTaskType.Generic, motion, when, 1, 0);
            AnimationEngine.AddTask(m, m.Start);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animation"></param>
        public static void ExecuteAnimation(FACEAnimation animation)
        {
            List<FACEMotion> motions = animation.FACEMotionsList;
            
            foreach (FACEMotion m in motions)
            {
                DateTime when = DateTime.Now.Add(TimeSpan.FromMilliseconds(m.DelayTime));
                MotorTask task = new MotorTask(MotorTaskType.Generic, m, when, animation.RepeatTimes, animation.RepeatFrequency);
                AnimationEngine.AddTask(task, task.Start);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animation"></param>
        public static void ExecuteAnimation(FACEAnimation animation, MotorTaskType motorType)
        {
            DateTime when = DateTime.Now;
            MotorTask task;

            switch(motorType)
            {
                case MotorTaskType.Blinking:

                    when = DateTime.Now;

                    //Chiusura
                    animation.FACEMotionsList[0].Duration = speedEyesTime; //velocità di chiusura
                    when = DateTime.Now.Add(TimeSpan.FromMilliseconds(animation.FACEMotionsList[0].DelayTime));
                    task = new MotorTask(motorType, animation.FACEMotionsList[0], when, animation.RepeatTimes, animation.RepeatFrequency);
                    AnimationEngine.AddTask(task, task.Start);

                    //Apertura
                    animation.FACEMotionsList[1].Duration = speedEyesTime; //velocità di apertura
                    animation.FACEMotionsList[1].DelayTime = closedEyesTime;
                    when = DateTime.Now.Add(TimeSpan.FromMilliseconds(animation.FACEMotionsList[1].DelayTime + animation.FACEMotionsList[0].Duration));
                    task = new MotorTask(motorType, animation.FACEMotionsList[1], when, animation.RepeatTimes, animation.RepeatFrequency);
                    AnimationEngine.AddTask(task, task.Start);

                    //DateTime when = DateTime.Now;
                    //animation.FACEMotionsList[0].Duration = speedEyesTime; //velocità di chiusura
                    //animation.FACEMotionsList[1].Duration = speedEyesTime; //velocità di apertura
                    //animation.FACEMotionsList[1].DelayTime = closedEyesTime;

                    //for (int i = 0; i < animation.FACEMotionsList.Count; i++)
                    //{
                    //    if (i > 0)
                    //    {
                    //        when = DateTime.Now.Add(TimeSpan.FromMilliseconds(animation.FACEMotionsList[i].DelayTime + animation.FACEMotionsList[i - 1].Duration));
                    //    }
                    //    else
                    //    {
                    //        when = DateTime.Now.Add(TimeSpan.FromMilliseconds(animation.FACEMotionsList[i].DelayTime));
                    //    }
                    //    MotorTask task = new MotorTask(motorType, animation.FACEMotionsList[i], when, animation.RepeatTimes, animation.RepeatFrequency);
                    //    AnimationEngine.AddTask(task, task.Start);
                    //}
                    break;

                case MotorTaskType.Respiration:

                    when = DateTime.Now;

                    //Chiusura
                    animation.FACEMotionsList[0].Duration = respirationSpeed; //velocità di chiusura
                    when = DateTime.Now.Add(TimeSpan.FromMilliseconds(animation.FACEMotionsList[0].DelayTime));
                    task = new MotorTask(motorType, animation.FACEMotionsList[0], when, animation.RepeatTimes, animation.RepeatFrequency);
                    AnimationEngine.AddTask(task, task.Start);

                    //Apertura
                    animation.FACEMotionsList[1].Duration = respirationSpeed; //velocità di apertura
                    animation.FACEMotionsList[1].DelayTime = 0;
                    when = DateTime.Now.Add(TimeSpan.FromMilliseconds(animation.FACEMotionsList[1].DelayTime + animation.FACEMotionsList[0].Duration));
                    task = new MotorTask(motorType, animation.FACEMotionsList[1], when, animation.RepeatTimes, animation.RepeatFrequency);
                    AnimationEngine.AddTask(task, task.Start);
                    break;
            }
        }

        /// <summary>
        /// Execute a single motor movement.
        /// </summary>
        /// <param name="ch">Number of the channel</param>
        /// <param name="pulse">Amount of the movement between 0 and 1</param>
        /// <param name="time">Duration of the movement</param>
        /// <param name="priority">Priority of the movement</param>
        /// <param name="when">Start time of the movement</param>
        public static void ExecuteSingleMovement(int ch, float pulse, TimeSpan duration, int priority, TimeSpan delay)
        {            
            FACEMotion motion = new FACEMotion(NUMBER_OF_MOTORS);
            motion.Duration = (int)duration.TotalMilliseconds;
            motion.Priority = priority;
            motion.DelayTime = (int)delay.TotalMilliseconds;
            motion.ServoMotorsList[ch].PulseWidthNormalized = pulse;

            DateTime when = DateTime.Now.Add(TimeSpan.FromMilliseconds(motion.DelayTime));

            MotorTask m = new MotorTask(MotorTaskType.Single, motion, when, 0, 1);
            AnimationEngine.AddTask(m, m.Start);
        }


        /// <summary>
        /// Send a raw value ([500,2500]) to the servo motor.
        /// </summary>
        /// <param name="ch">Number of the channel</param>
        /// <param name="rawVal">Amount of the movement in the range [500,2500]</param>
        /// <param name="time">Duration of the movement</param>
        public static void SendRawCommand(int ch, int rawVal, int time) // Used only by FACEConfig: without animator
        {
            if (ComPorts.OpenedPort)
            {
                System.IO.MemoryStream memStream = null;
                try
                {
                    switch (ComPorts.DeviceType)
                    {
                        case StdType.SSC32:
                            CommandSSC32 ssc32Com = new CommandSSC32(currentMotorState[ch].Name, ch, rawVal);
                            memStream = ssc32Com.FormatServoMove();
                            StringBuilder sb = new StringBuilder(new System.IO.StreamReader(memStream).ReadToEnd());
                            sb.Append(String.Format("T{0} \r", time));
                            ComPorts.SelectedPort.WriteLine(sb.ToString());

                            //If the command is sent correctly, the servo motor pulse is updated
                            currentMotorState[ch].UpdateRawValue(rawVal); // CHECK FOR CHANGING
                            break;
                    }
                }
                catch
                {
                    throw new FACException("Command not sent. Some errors occured.");
                }
            }
            else
            {
                throw new FACException("There are not opened ports.");
            }
        }

        /// <summary>
        /// Send a raw value ([500,2500]) to the servo motor.
        /// </summary>
        /// <param name="ch">Number of the channel</param>
        /// <param name="port">POrt Servo Controller</param>
        /// <param name="rawVal">Amount of the movement in the range [500,2500] us</param>
        /// <param name="serial">Serial Servo Controller</param>
        public static void SendRawCommand(int ch, int port, int rawVal, string serial, int time) // Used only by FACEConfig: without animator
        {
            
                try
                {
                     Usc usc = null;
                     foreach (DeviceListItem d in Usc.getConnectedDevices())
                     {
                        if (d.serialNumber == serial){
                            usc = new Usc(d);
                            ServoStatus[] servos;
                            usc.getVariables(out servos);
                            int position =servos[port].position;

                            int diffPos = Math.Abs(position - (int)(rawVal/0.25));
                            int speed=0;
                            if ((diffPos) * 10 >= time) 
                            {
                                speed =(int)( (diffPos * 10) /  time);
                               
                            }

                            usc.setSpeed((byte)port, (ushort)speed);
                            usc.setTarget((byte)port, (ushort)(rawVal/(0.25))); //the pulse width to transmit in units of quarter-microseconds 
                            Console.WriteLine("Connected to #" + serial + ".");
                            usc.Dispose();
                            usc = null;
                        }
                           
                     }

                      //If the command is sent correctly, the servo motor pulse is updated
                      currentMotorState[ch].UpdateRawValue(rawVal); // CHECK FOR CHANGING
                            
                    
                }
                catch
                {
                    throw new FACException("Command not sent. Some errors occured.");
                }
            
            //else
            //{
            //    throw new FACException("There are not opened ports.");
            //}
        }
        #endregion


        #region Load/Save configurations

        public static List<ServoMotor> LoadConfigFile(string filename)
        {
            try
            {
                defaultMotorState = (FACEMotion.LoadFromXmlFormat(filename)).ServoMotorsList;
                currentMotorState = defaultMotorState;
                return currentMotorState;
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
                FACEMotion.SaveAsXmlFormat(objGraph, fileName);
                defaultMotorState = objGraph as List<ServoMotor>;
            }
            catch
            {
                throw;
            }
        }

        #endregion
        
    }
    
}