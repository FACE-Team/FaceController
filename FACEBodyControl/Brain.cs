using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Timers;

namespace FACEBodyControl
{
    public class Brain
    {
        #region Constants from interface

        private static float xLimit = 0.1f;
        public static float XLimit
        {
            get { return xLimit; }
            set { xLimit = value; }
        }

        private static float yLimit = 0.1f;
        public static float YLimit
        {
            get { return yLimit; }
            set { yLimit = value; }
        }

        private static float moveEyesMax = 0.2f;
        public static float MoveEyesMax
        {
            get { return moveEyesMax; }
            set { moveEyesMax = value; }
        }

        private static float kNeckTurn = 0.05f;
        public static float KNeckTurn
        {
            get { return kNeckTurn; }
            set { kNeckTurn = value; }
        }

        private static float kNeckTilt = 0.05f;
        public static float KNeckTilt
        {
            get { return kNeckTilt; }
            set { kNeckTilt = value; }
        }

        #endregion


        #region Members

        private static DispatcherTimer timer = new DispatcherTimer();

        private static bool brainRunning;
        public static bool BrainRunning
        {
            get { return brainRunning; }
            set { brainRunning = value; }
        }

        #endregion


        #region On/Off Brain

        public static void TurnOnBrain()
        {
            if (ComPorts.OpenedPort)
            {
                FACEBody.ResetBody();
                brainRunning = true;

                // Set the Interval: 300
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Tick += new EventHandler(delegate(object s, EventArgs a)
                {
                    DoStep(FACEBody.GetPerception());
                });

                timer.Start();
            }
            else
            {
                throw new FACException("There are not opened ports.");
            }
        }


        public static void TurnOffBrain()
        {
            timer.Stop();
            brainRunning = false;
        }

        #endregion

        
        private static float GetNeckTurnInc(float deltaX, float minSpeed, float maxSpeed, float threshold, float maxDeltaVal)
        {
            double alpha = (Math.Log((double)maxSpeed) - Math.Log((double)minSpeed)) / (maxDeltaVal - threshold);
            double res = minSpeed / 10;
            //if (Math.Abs(deltaX) > threshold)
            //{
            //    res = (maxSpeed * Math.Exp(alpha * (deltaX + threshold))) - 2;
            //}
            if (Math.Abs(deltaX) > threshold)
            {
                res = (maxSpeed * Math.Exp(alpha * (Math.Abs(deltaX) - maxDeltaVal))) - 1;
            }
            
            return (float)res * kNeckTurn;
        }

        private static float GetNeckTiltInc(float deltaX, float minSpeed, float maxSpeed, float threshold, float maxDeltaVal)
        {
            double alpha = (Math.Log((double)maxSpeed) - Math.Log((double)minSpeed)) / (maxDeltaVal - threshold);
            double res = minSpeed / 10;
            //if (Math.Abs(deltaX) > threshold)
            //{
            //    res = (maxSpeed * Math.Exp(alpha * (deltaX + threshold))) - 2;
            //}
            if (Math.Abs(deltaX) > threshold)
            {
                res = (maxSpeed * Math.Exp(alpha * (Math.Abs(deltaX) - maxDeltaVal))) - 1;
            }

            return (float)res * kNeckTilt;
        }


        private static void DoStep(Percept p)
        {
            // FaceTracker Module: 
            // - receives a list of rectangles (a rectangle identifies a face)
            // - selects the greatest rectangle, means the face is nearest to the webcam (it can be changed)
            // - calculates the direction and amount of the movement
            // - sends the command to Body (TurnNeck/TiltNeck/TurnEyes)

            if (p != null)
            {
                //if (p.ObjInfo.Description.Contains("FaceTrack"))
                switch (p.ObjInfo.Module)
                {
                    case BrainModules.FaceTrack:

                        //if (p.ObjInfo.FacesList.Count != 0)
                        //{
                        //    // Select greater rectangle
                        //    Rect face = SelectFace(p.ObjInfo.FacesList);

                        //    //calculates the amount (delta) of the movement (normalized)
                        //    float deltaX = (float)((face.X + (face.Width / 2)) - 0.5);
                        //    float deltaY = (float)((face.Y + (face.Height / 2)) - 0.5);

                        //    float turn = GetNeckTurnInc(deltaX, 1, 2, xLimit, 0.5f);
                        //    float tilt = GetNeckTiltInc(deltaY, 1, 2, yLimit, 0.5f);

                        //    //calculates the direction of the movement                  
                        //    Direction dirToSetX, dirToSetY/*, dirNeck*/;

                        //    if (Math.Abs(deltaX) > xLimit)
                        //    {
                        //        if (deltaX > 0)
                        //            //face to right
                        //            dirToSetX = Direction.Right;
                        //        else
                        //            dirToSetX = Direction.Left;

                                
                        //        //float deltaNeck = (float)(ServoMotorGroup.CurrentState.ElementAt((int)MotorsNames.EyeTurnRight).PulseWidth - 0.8);
                            
                        //        //if (deltaNeck < 0)
                        //        //    dirNeck = Direction.Right;
                        //        //else
                        //        //    dirNeck = Direction.Left;
                                

                        //        //if (Math.Abs(deltaX) > moveEyesMax)
                        //        //{                                   
                        //            //FACEBody.TurnNeck(dirToSetX, Math.Abs(deltaX), DateTime.Now);

                                    
                        //            FACEBody.TurnNeck(dirToSetX, turn, DateTime.Now);
                        //        //}

                        //        //FACEBody.TurnEyes(dirToSetX, Math.Abs(deltaX), DateTime.Now);
                        //    }

                        //    //if (Math.Abs(deltaY) > yLimit)
                        //    //{
                        //        if (deltaY > 0)
                        //            //face toward down
                        //            dirToSetY = Direction.Down;
                        //        else
                        //            dirToSetY = Direction.Up;

                        //        //FACEBody.TiltNeck(dirToSetY, Math.Abs(deltaY), DateTime.Now);
                        //        FACEBody.TiltNeck(dirToSetY, tilt, DateTime.Now);
                        //    //}
                        //}
                        break;

                    case BrainModules.Blink:

                        break;
                }
            }
        }


        private static Rect SelectFace(List<Rect> l)
        {
            Rect maxRect = l.ElementAt(0);
            for (int i = 1; i < l.Count; i++)
            {
                if ((l.ElementAt(i).Width * l.ElementAt(i).Height) > (maxRect.Width * maxRect.Height))
                    maxRect = l.ElementAt(i);
            }
            return maxRect;
        }

    }
}



//#region Brain with While NOT WORKING

//public static void TurnOnBrain_While()
//{
//    if (ComPorts.OpenedPort)
//    {
//        //FACEBody.ResetBody();
//        brainRunning = true;

//        while (brainRunning)
//        {
//            try
//            {
//                DoStep(FACEBody.GetPerception());
//                //System.Diagnostics.Debug.WriteLine("Step done");
//            }
//            catch (Exception e)
//            {
//                string s = e.Message;
//            }
//        }
//    }
//    else
//    {
//        throw new FACException("There are not opened ports.");
//    }
//}

//public static void TurnOffBrain_While()
//{
//    brainRunning = false;
//}

//#endregion