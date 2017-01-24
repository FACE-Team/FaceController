using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Pololu.UsbWrapper;
using Pololu.Usc;

namespace FACEBodyControl
{
    public struct MotorStep
    {
        public float val;
        public int pr;
    }

    public static class AnimationEngine
    {
        // Create an AutoReset EventWaitHandle
        private static EventWaitHandle ewHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        public static EventWaitHandle EWHandle
        {
            get { return ewHandle; }
        }

        // 
        private static Dictionary<int, List<MotorStep>> motorSteps = new Dictionary<int, List<MotorStep>>();
        public static Dictionary<int, List<MotorStep>> MotorSteps
        {
            get { return motorSteps; }
            set { motorSteps = value; }
        }


        // Heap used to store tasks ordered by due time.
        private static AnimationHeap tasksHeap = new AnimationHeap();

        /// InterpolatorTask represents the task which interpolates all the tasks 
        /// into the heap of the AnimationEngine when it is being run.
        /// It is rescheduled into the heap when it finishes its task.
        private static InterpolatorTask interp;

        // Waiting time of the thread.
        private static TimeSpan waitTime;

        // When the timer is started, it is used to record time.
        private static DateTime start = new DateTime();

        // (32x9byte + tempo) x 8bit at 115.200 kbs richiedono 20.208 milliseconds quindi non si può inviare più velocmente di 1 ogni 25ms
        private static TimeSpan interpolatorInterval = new TimeSpan(400000); // 1 milliseconds 
        // ---> controllare: fino al 25/02/2013 era 100000, cambiato a 400000 come Interval Motor in FACEBody

        // Return the timer time, a number from start time to the current time.
        private static TimeSpan GetTime()
        {
            return DateTime.Now - start;
        }

        private static bool running = false;
        public static bool Running
        {
            get { return running; }
            set { running = value; }
        }

        
        public static void StartAnimation()
        {
            start = DateTime.Now;
            interp = new InterpolatorTask(DateTime.Now, interpolatorInterval);           
            interp.DoAction(0);
            running = true;
            TimerTick();
        }

        public static void StopAnimation()
        {
            running = false;
        }

        /// <summary>
        /// Add a motor to the dictionary with a value and a priority.
        /// This dictionary will be used by the Interpolator task to merge the collected movements:
        /// all data in the dictionary will become a MotorTask to be executed.
        /// </summary>
        /// <param name="id"> The motor identifier. </param>
        /// <param name="val"> The motor value. </param>
        /// <param name="pr"> The motor priority (not used at the moment). </param>
        public static void AddMotor(int id, float val, int pr)
        {
            MotorStep step = new MotorStep();
            step.val = val;
            step.pr = pr;

            if (motorSteps.Keys.Contains(id))
                motorSteps[id].Add(step);
            else
            {
                List<MotorStep> values = new List<MotorStep>();
                values.Add(step);
                motorSteps.Add(id, values);
            }
        }

        /// <summary>
        /// Schedule a task for execution at a given time from the start of the timeline.
        /// </summary>
        /// <param name="mTask"> The task to be executed after t time elapsed. </param>
        /// <param name="t"> The expiring absolute time of the task. </param>
        public static void AddTask(Task mTask, DateTime t)
        {
            // mTask expires in (elem.Due) milliseconds
            HeapElement elem = new HeapElement();
            elem.Task = mTask;
            // elem.Due = DateTime.Now - t;
            elem.Due = t - start;  // The expiring relative time of the task from the start of the timer

            TimeSpan topTime;

            try
            {
                Monitor.Enter(tasksHeap);
                tasksHeap.Insert(elem);
                topTime = tasksHeap.Top().Due;
            }
            finally
            {
                Monitor.Exit(tasksHeap);
            }

            if (/*waitTime.Milliseconds != 0 &&*/ topTime <= elem.Due)
            {
                // restart thread
                waitTime = TimeSpan.FromMilliseconds(0); //<----------
                //ewh.Set();
                ewHandle.Set();
            }
            /*
            // if there is no timeout, calculate if and for how long time it is suspended
            if (waitTime.Milliseconds == 0)
            {
                //waitTime = tasksHeap.Top().Due - GetTime();
                waitTime = GetTime() - tasksHeap.Top().Due;
                //waitHandle.WaitOne(waitTime);
                //runAnimation.Join(waitTime);
                ewh.WaitOne(waitTime);
            }
            */
        }


      

        /// <summary>
        /// Stop the execution of a particular type of task by removing 
        /// all instances of it from the scheduler.
        /// </summary>
        /// <param name="tType"> The task type to be removed. </param>
        /// <returns></returns>
        /// 
        public static bool RemoveAllTasks(MotorTaskType tType)
        {
            try
            {
                List<Task> toRemove = new List<Task>();
                Monitor.Enter(tasksHeap);

                foreach (HeapElement elem in tasksHeap.Data)
                {
                    if (elem.Task == null)
                        toRemove.Add(elem.Task);
                    else
                    {
                        if (elem.Task.GetType() == typeof(MotorTask) && (elem.Task as MotorTask).Type == tType)
                        {
                            toRemove.Add(elem.Task);
                        }
                    }
                }
                foreach (Task t in toRemove)
                {
                    tasksHeap.RemoveTask(t);
                }
            }
            catch
            {
                Monitor.Exit(tasksHeap);
                return false;
            }
            finally
            {
                // Ensure that the lock is released.
                Monitor.Exit(tasksHeap);
            }
            return true;
        }
       


        public static void TimerTick()
        {
            while (running)
            {
                // t.Time() = the time from the start + 10 milliseconds
                TimeSpan currTime = GetTime() +TimeSpan.FromMilliseconds(100);

                try
                {
                    Monitor.Enter(tasksHeap);

                    //if there are some expired tasks, the heap executes them
                    while (tasksHeap.Count() > 0 && currTime >= tasksHeap.Top().Due)
                    {
                        var el = tasksHeap.Remove();
                        el.Task.DoAction((currTime - el.Due).Milliseconds);   //.Milliseconds o .TotalMilliseconds ???????
                    }
                }
                finally
                {
                    // Ensure that the lock is released.
                    Monitor.Exit(tasksHeap);
                }

                // if there are other tasks (not expired)
                if (tasksHeap.Count() > 0)
                {
                    currTime = GetTime();

                    /******* TO CONTROL THE CORRECTNESS *********/
                    // richiama la funzione dopo tot millisecondi = elemento più vicino da eseguire - currTime
                    //AnimeJTimerRunning = setTimeout('AnimeJTimerTick()', t.Heap.Top().Due - currt);

                    // it waits for a time 
                    if (tasksHeap.Top().Due >= currTime)
                    {
                        //waitTime = tasksHeap.Top().Due - currTime;
                        ewHandle.WaitOne(waitTime);
                    }
                    /******* TO CONTROL THE CORRECTNESS *********/
                }
                else
                {
                    ewHandle.WaitOne();
                }
            }
        }


        #region Send Commands

         //Used by animator (Interpolator task)
        //internal static void SendCommand(List<ServoMotor> motorsList)
        //{
        //    if (ComPorts.OpenedPort)
        //    {
        //        MemoryStream memStream = null;
        //        try
        //        {
        //            switch (ComPorts.DeviceType)
        //            {
        //                case StdType.SSC32:
        //                    StringBuilder sb = new StringBuilder();

        //                    //At this point each servo motor value must be != -1 (controlled in MotorTask).
        //                    //If some servo motor value equals -1, it means that the servo motor
        //                    //is not configured (those servo motors equal -1 also in the default configuration).
        //                    for (int i = 0; i < motorsList.Count; i++)
        //                    {
        //                        //Only motor values != -1 are sent on the serial port
        //                        if (motorsList.ElementAt(i).PulseWidth != -1)
        //                        {
        //                            memStream = motorsList.ElementAt(i).GenerateCommand();
        //                            sb.Append(new StreamReader(memStream).ReadToEnd());
        //                        }
        //                    }
        //                    sb.Append(String.Format("T{0} \r", (int)interpolatorInterval.TotalMilliseconds));
        //                    ComPorts.SelectedPort.WriteLine(sb.ToString());
        //                    System.Diagnostics.Debug.WriteLine(sb.ToString());

        //                    //If the command is sent correctly, FACEBody.CurrentState is updated
        //                    for (int j = 0; j < motorsList.Count; j++)
        //                    {
        //                        if (motorsList[j].PulseWidth != -1)
        //                            FACEBody.CurrentMotorState[j].PulseWidth = motorsList[j].PulseWidth;
        //                    }
        //                    ComPorts.SelectedPort.DiscardOutBuffer();
        //                    break;
        //            }
        //        }
        //        catch
        //        {
        //            throw new FACException("Command not sent. Some errors occured.");
        //        }
        //    }
        //    else
        //    {
        //        throw new FACException("There are not opened ports.");
        //    }
        //}

        internal static void SendCommand(List<ServoMotor> motorsList)
        {
            
            try{
 
                Usc usc = null;
                //At this point each servo motor value must be != -1 (controlled in MotorTask).
                //If some servo motor value equals -1, it means that the servo motor
                //is not configured (those servo motors equal -1 also in the default configuration).
                for (int i = 0; i < motorsList.Count; i++)
                {
                    //Only motor values != -1 are sent on the serial port
                    if (motorsList.ElementAt(i).PulseWidthNormalized != -1)
                    {
                        int port = motorsList.ElementAt(i).PortSC;
                        string serial = motorsList.ElementAt(i).SerialSC;
                        int rawVal =motorsList.ElementAt(i).MappingOnMinMaxInterval();

                        foreach (DeviceListItem d in Usc.getConnectedDevices())
                        {
                            if (d.serialNumber == serial)
                            {
                                usc = new Usc(d);
                                ServoStatus[] servos;
                                usc.getVariables(out servos);
                                int position = servos[port].position;

                                int diffPos = Math.Abs(position - (int)(rawVal / 0.25));
                                int speed = 0;
                                //if ((diffPos) * 10 >= interpolatorInterval.Milliseconds)
                                //{
                                //    speed = (int)((diffPos * 10) / interpolatorInterval.Milliseconds);

                                //}
                                
                                if (diffPos != 0)
                                {
                                    usc.setSpeed((byte)port, (ushort)speed);
                                    //usc.setSpeed((byte)port, (ushort)(int)interpolatorInterval.TotalMilliseconds);
                                    usc.setTarget((byte)port, (ushort)(rawVal / (0.25))); //the pulse width to transmit in units of quarter-microseconds 
                                    //Console.WriteLine("Connected to #" + serial + " position:" + (ushort)(rawVal / (0.25)) + " servo:" + motorsList.ElementAt(i).Channel+" time:"+ DateTime.Now.ToString("ss.fff"));
                                }
                                usc.Dispose();
                                usc = null;
                            }
                           
                         }
                    }
                }

                //If the command is sent correctly, FACEBody.CurrentState is updated
                for (int j = 0; j < motorsList.Count; j++)
                {
                    if (motorsList[j].PulseWidthNormalized != -1)
                        FACEBody.CurrentMotorState[j].PulseWidthNormalized = motorsList[j].PulseWidthNormalized;
                }
                           
                   
            }
            catch(Exception ex)
            {
                //throw new FACException("Command not sent. Some errors occured.");
                throw new FACException("Send Command Pololu Error:" +ex.Message);

            }

        }

        #endregion

    }
}
