using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace FACEBodyControl
{
    [Serializable()]
    public class FACEMotion
    {        
        internal const int DEFAULT_TIME_VALUE = 2000;
        internal const int MIN_TIME_VALUE = 0; // Min Time in mS for the entire move
        internal const int MAX_TIME_VALUE = 65535; // Max Time in mS for the entire move

        /// <summary>
        /// Name of the motion
        /// </summary>
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// ECS coordinate 
        /// </summary>
        private ECS.ECSCoordinate ecsCoord;
        public ECS.ECSCoordinate ECSCoord
        {
            get { return ecsCoord; }
            set { ecsCoord = value; }
        }

        /// <summary>
        /// Duration time of the motion (in milliseconds)
        /// </summary>
        private TimeSpan duration;
        public int Duration
        {
            get { return (int)duration.TotalMilliseconds; }
            set { duration = TimeSpan.FromMilliseconds(value); }
        }

        /// <summary>
        /// Delay time of the motion from now
        /// </summary>
        private TimeSpan delayTime;
        public int DelayTime
        {
            get { return (int)delayTime.TotalMilliseconds; }
            set { delayTime = TimeSpan.FromMilliseconds(value); }
        }
        //public string DelayTime
        //{
        //    get { return String.Format("{0:c}", delayTime); }
        //    set { delayTime = TimeSpan.Parse(value); }
        //}

        /// <summary>
        /// 
        /// </summary>
        private int priority;
        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        /// <summary>
        /// The list of servo motors configuration.
        /// </summary>
        private List<ServoMotor> servoMotorsList;
        public List<ServoMotor> ServoMotorsList
        {
            get { return servoMotorsList; }
            set { servoMotorsList = value; }
        }


        public FACEMotion() { }


        public FACEMotion(int size)
        {
            name = "";
            ecsCoord = new ECS.ECSCoordinate(0, 0, 0);
            duration = TimeSpan.FromMilliseconds(0);
            delayTime = TimeSpan.FromMilliseconds(0);
            priority = 10;            
            servoMotorsList = new List<ServoMotor>(size);
            for (int i = 0; i < size; i++)
            {
                int min = (int)FACEBody.DefaultMotorState.ElementAt(i).MinValue;
                int max = (int)FACEBody.DefaultMotorState.ElementAt(i).MaxValue;
                string serial = FACEBody.DefaultMotorState.ElementAt(i).SerialSC;
                int port=(int) FACEBody.DefaultMotorState.ElementAt(i).PortSC;
                servoMotorsList.Add(new ServoMotor(Enum.GetName(typeof(MotorsNames), i), i, -1, min, max,serial,port ));
            }
        }

        public FACEMotion(List<ServoMotor> motorsList, TimeSpan dur)
        {
            name = "";
            ecsCoord = new ECS.ECSCoordinate(0, 0, 0);
            duration = dur;
            delayTime = TimeSpan.FromMilliseconds(0);
            priority = 10;
            servoMotorsList = motorsList;
        }

        public FACEMotion(List<ServoMotor> motorsList, TimeSpan dur, int pr)
        {
            name = "";
            ecsCoord = new ECS.ECSCoordinate(0, 0, 0);
            duration = dur;
            delayTime = TimeSpan.FromMilliseconds(0);
            priority = pr;
            servoMotorsList = motorsList;
        }

        public FACEMotion(List<ServoMotor> motorsList, TimeSpan dur, int pr, TimeSpan t)
        {
            name = "";
            ecsCoord = new ECS.ECSCoordinate(0, 0, 0);
            duration = dur;
            delayTime = t;
            priority = pr;
            servoMotorsList = motorsList;
        }

        public FACEMotion(List<ServoMotor> motorsList, TimeSpan dur, int pr, ECS.ECSCoordinate ecsStruct)
        {
            name = "";
            ecsCoord = ecsStruct;
            duration = dur;
            delayTime = TimeSpan.FromMilliseconds(0);
            priority = pr;
            servoMotorsList = motorsList;
        }


        #region Save as/Load from XML Format

        /// <summary>
        /// Save object to a file in XML format. 
        /// </summary>
        /// <param name="objGraph"></param>
        /// <param name="fileName"></param>
        /// <exception cref="FaceException"></exception>
        public static void SaveAsXmlFormat(object objGraph, string fileName)
        {
            XmlSerializer xmlFormat = new XmlSerializer(typeof(FACEMotion), new Type[] { typeof(ServoMotor) });
            Stream fStream = null;
            StreamWriter xmlWriter = null;

            try
            {
                fStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                xmlWriter = new StreamWriter(fStream);
                xmlFormat.Serialize(xmlWriter, objGraph);
                xmlWriter.Close();
                fStream.Close();
            }
            catch (Exception ex)
            {
                xmlWriter.Close();
                fStream.Close();
                throw new FACException("Some error occurs during save expression file.", ex, typeof(FACEMotion).FullName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static FACEMotion LoadFromXmlFormat(string fileName)
        {
            XmlSerializer xmlFormat = new XmlSerializer(typeof(FACEMotion), new Type[] { typeof(ServoMotor) });
            Stream filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Open);
                FACEMotion motion = xmlFormat.Deserialize(filestream) as FACEMotion;
                filestream.Close();
                return motion;
            }
            catch (Exception ex)
            {
                filestream.Close();
                throw new FACException("Some error occurs during load expression file.", ex, typeof(FACEMotion).FullName);
            }
        }

        #endregion


        #region Utils

        public double[] GetValues()
        {
            double[] values = new double[servoMotorsList.Count];
            for (int i = 0; i < servoMotorsList.Count; i++)
            {
                values[i] = servoMotorsList[i].PulseWidthNormalized;
            }
            return values;
        }       

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < servoMotorsList.Count; i++)
            {
                sb.AppendFormat("#{0} {1}; ", i, Decimal.Round((decimal)servoMotorsList[i].PulseWidthNormalized, 4));
            }
            return sb.ToString();
        }

        #endregion

    }
}
