using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FACEBodyControl
{
    public enum MotorTaskType { Generic, Single, Blinking, Yes, No, Respiration }

    /** 
     * This is the base class for all tasks that can be scheduled 
     * by the library. It simply defines the expected structure of an object 
     * that is handled by the scheduler. 
     * The model chosen by the library is that of Finite State Automata (FSA): 
     * each activity is an automata that is notified as time passes and of other 
     * events. 
     */
    public interface Task
    {
        DateTime Start { get; set; }

        int Interval { get; set; }

        /**
         * Notify the task that the animation has been paused.
         */
        void OnPause();
        /**
         * Notify the task that the animation has been resumed.
         */
        void OnResume();
        /**
         * Notify the task that the animation has been stopped.
         */
        void OnStop();
        /**
         * Perform a step of computation
         */
        void DoAction(int delay);
    }


    /******************************* MotorTask Class *******************************/

    /// <summary>
    /// MotorTask class represents the movement of a motor
    /// from a motor configuration to another
    /// </summary>
    public class MotorTask : Task
    {
        private MotorTaskType type;
        public MotorTaskType Type
        {
            get { return type; }
            set { type = value; }
        }

        private List<ServoMotor> startPosition;
        public List<ServoMotor> StartPosition
        {
            get { return startPosition; }
            set { startPosition = value; }
        }

        private List<ServoMotor> endPosition;
        public List<ServoMotor> EndPosition
        {
            get { return endPosition; }
            set { endPosition = value; }
        }

        private DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { start = value; }
        }
        
        private TimeSpan interval;
        public int Interval
        {
            get { return (int)interval.TotalMilliseconds; }
            set { interval = TimeSpan.FromMilliseconds(value); }
        }

        private TimeSpan duration;
        public int Duration
        {
            get { return (int)duration.TotalMilliseconds; }
            set { duration = TimeSpan.FromMilliseconds(value); }
        }

        private int numTimes;
        public int NumTimes
        {
            get { return numTimes; }
            set { numTimes = value; }
        }

        private int frequency;
        public int Frequency
        {
            get { return frequency; }
            set { frequency = value; }
        }

        private int priority;
        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        private int steps;
        public int Steps
        {
            get { return steps; }
        }

        //private int restoreSteps = 0;

        public MotorTask(MotorTaskType tType, FACEMotion motion, DateTime when, int times, int freq)
        {
            type = tType;
            endPosition = motion.ServoMotorsList;
            start = when;
            duration = TimeSpan.FromMilliseconds(motion.Duration);
            interval = FACEBody.MotorInterval;
            steps = (int)(duration.TotalMilliseconds / interval.TotalMilliseconds);
            numTimes = times;
            frequency = freq;
            priority = motion.Priority;

            //restoreSteps = steps;
        }

        #region MotorTask Members

        public void OnPause()
        {
            //restoreSteps = steps;
        }

        public void OnResume()
        {
            //steps = restoreSteps;
        }

        public void OnStop()
        {
        }

        /// <summary>
        /// A task is divided in subtasks based on the number of steps of the task.
        /// DoAction calculates the amount of movement that each motors must perform at each step.
        /// </summary>
        /// <param name="delay"></param>
        public void DoAction(int delay)
        {
            float val = 0;

            startPosition = FACEBody.CurrentMotorState;

            // Calculate the amount of movement that each motor must perform at the current step
            // and send it to the AnimationEngine
            for (int i = 0; i < endPosition.Count; i++)
            {
                //current[i] must be different from -1 (motor not working)
                if (endPosition[i].PulseWidthNormalized != -1 && startPosition[i].PulseWidthNormalized != -1)
                {
                    if(steps == 0)
                        val = (endPosition[i].PulseWidthNormalized - startPosition[i].PulseWidthNormalized);
                    else
                        val = (endPosition[i].PulseWidthNormalized - startPosition[i].PulseWidthNormalized) / steps;
                   
                    if (Math.Round(val, 4) != 0)
                    {
                        AnimationEngine.AddMotor(i, val, priority);
                    }
                }
            }
            steps--;

            // If the task is not completed, it is rescheduled into the heap
            if (steps > 0)
            {
                AnimationEngine.AddTask(this, DateTime.Now + interval);
            }
            else
            {
                if (numTimes > 0)
                {
                    numTimes--;
                    //wait = TimeSpan.FromMilliseconds(frequency);
                }

                if (steps == 0 && numTimes != 0)
                {
                    steps = (int)(duration.TotalMilliseconds / interval.TotalMilliseconds);
                    //Random r = new Random();
                    //double factor = r.NextDouble() + 0.5;
                    //TimeSpan wait = TimeSpan.FromMilliseconds((int)((60 / frequency) * 1000) * factor); //value between (wait/2) and (1.5*wait)
                    
                    TimeSpan wait = TimeSpan.FromMilliseconds(frequency);
                    AnimationEngine.AddTask(this, DateTime.Now + duration + wait);
                }
            }
        }

        #endregion
    }




    /******************************* MotorInterfaceTask Class *******************************/

    /// <summary>
    /// InterpolatorTask class represents the task which interpolates
    /// all the tasks into the heap of the AnimationEngine when it is being run.
    /// </summary>
    public class InterpolatorTask : Task
    {
        /// <summary>
        /// 
        /// </summary>
        private DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { start = value; }
        }

        /// <summary>
        /// Task frequency in milliseconds
        /// </summary>
        private TimeSpan interval;
        public int Interval
        {
            get { return (int)interval.TotalMilliseconds; }
            set { interval = TimeSpan.FromMilliseconds(value); }
        }

        public InterpolatorTask(DateTime when, TimeSpan interv)
        {            
            start = when;
            interval = interv;
        }


        #region InterpolatorTask Members

        public void OnPause()
        {
        }

        public void OnResume()
        {
        }

        public void OnStop()
        {
        }

        /// <summary>
        /// Calculate the new positions for each motors based on all the 
        /// tasks which are required to be executed.
        /// </summary>
        /// <param name="delay"></param>
        public void DoAction(int delay)
        {
            Dictionary<int, List<MotorStep>> valuesToInterp = new Dictionary<int, List<MotorStep>>(AnimationEngine.MotorSteps);
            AnimationEngine.MotorSteps.Clear();
            
            if (valuesToInterp.Count != 0)
            {
                

                FACEMotion motion = new FACEMotion(FACEBody.CurrentMotorState.Count);
                List<ServoMotor> motorList = motion.ServoMotorsList;

                try
                {
                    foreach (KeyValuePair<int, List<MotorStep>> couple in valuesToInterp)
                    {
                        List<MotorStep> value = couple.Value.ToList();
                     
                        float totVal = 0;
                        int totPr = 0;
                        foreach (MotorStep step in value)
                        {
                            totVal += (step.val * step.pr);
                            totPr += step.pr;
                        }
                        motorList[couple.Key].PulseWidthNormalized =
                            FACEBody.CurrentMotorState[couple.Key].PulseWidthNormalized + (totVal / totPr);
                    }
                    AnimationEngine.SendCommand(motorList);
                }
                catch
                {
                    AnimationEngine.SendCommand(FACEBody.DefaultMotorState);  //check correctness
                }
            }
            // Reschedule

             AnimationEngine.AddTask(this, DateTime.Now + interval);
        }

        #endregion
    }

}