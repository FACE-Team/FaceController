using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace FACEBodyControl
{
    [Serializable()]
    public class ServoMotorGroup 
    {
        internal const int NUMBER_OF_MOTORS = 32;
        internal const int DEFAULT_TIME_VALUE = 2000;
        internal const int MIN_TIME_VALUE = 0; // Min Time in mS for the entire move
        internal const int MAX_TIME_VALUE = 65535; // Max Time in mS for the entire move

        /// <summary>
        /// The list of servo motors configuration.
        /// </summary>
        private List<ServoMotor> servoMotorList;
        public List<ServoMotor> ServoMotorsList
        {
            get { return servoMotorList; }
            set { servoMotorList = value; }
        }        

        /// <summary>
        /// Time in ms for the entire move, affects all channels, 65535 max (optional)
        /// </summary>
        private int time;
        public int Time
        {
            get { return time; }
            set
            {
                if (value < MIN_TIME_VALUE)
                {
                    value = MIN_TIME_VALUE;
                }
                else if (value > MAX_TIME_VALUE)
                {
                    value = MAX_TIME_VALUE;
                }
                else
                {
                    time = value;
                }
            }
        }

        
        #region Constructors

        public ServoMotorGroup() { }


        /// <summary>
        /// Initializes a new instance of the ServoMotorGroup class with PulseWidth set to -1.
        /// </summary>
        /// <param name="list">List of all ServoMotor object</param>
        public ServoMotorGroup(int size)
        {
            servoMotorList = new List<ServoMotor>(size);
            for (int i = 0; i < size; i++)
            {
                int min = (int)FACEBody.DefaultState.servoMotorList.ElementAt(i).MinValue;
                int max = (int)FACEBody.DefaultState.servoMotorList.ElementAt(i).MaxValue;
                servoMotorList.Add(new ServoMotor(Enum.GetName(typeof(MotorsNames), i), i, -1, min, max));
            }

            time = DEFAULT_TIME_VALUE;
            //pleasure = 0;
            //arousal = 0;
            //dominance = 0;
            //name = "";
        }


        public ServoMotorGroup(int size, int min, int max)
        {
            servoMotorList = new List<ServoMotor>(size);
            for (int i = 0; i < size; i++)
            {                
                servoMotorList.Add(new ServoMotor(Enum.GetName(typeof(MotorsNames), i), i, -1, min, max));
            }

            time = DEFAULT_TIME_VALUE;
            //pleasure = 0;
            //arousal = 0;
            //dominance = 0;
            //name = "";
        }


        /// <summary>
        /// Initializes a new instance of the ServoMotorGroup class.
        /// </summary>
        /// <param name="list">List of all ServoMotor object</param>
        public ServoMotorGroup(List<ServoMotor> list)
        {
            servoMotorList = list;
            time = DEFAULT_TIME_VALUE;
            //pleasure = 0;
            //arousal = 0;
            //dominance = 0;
            //name = "";
        }


        /// <summary>
        /// Initializes a new instance of the ServoMotorGroup class.
        /// </summary>
        /// <param name="list">List of all ServoMotor object</param>
        /// <param name="t">Time in mS for entire move, effects all ServoMotors</param>
        public ServoMotorGroup(List<ServoMotor> list, int t)
        {
            servoMotorList = list;
            time = t;
            //pleasure = 0;
            //arousal = 0;
            //dominance = 0;
            //name = "";
        }

        #endregion


        #region Save as/Load from XML Format

        /// <summary>
        /// Save object to a file in XML format. 
        /// </summary>
        /// <param name="objGraph"></param>
        /// <param name="fileName"></param>
        /// <exception cref="FaceException"></exception>
        public static void SaveAsXmlFormat(object objGraph, string fileName)
        {
            XmlSerializer xmlFormat = new XmlSerializer(typeof(ServoMotorGroup), new Type[] { typeof(ServoMotor) });
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
                throw new FACException("Some error occurs during save expression file.", ex, typeof(ServoMotorGroup).FullName);
            }
        }

        public static ServoMotorGroup LoadFromXmlFormat(string fileName)
        {
            XmlSerializer xmlFormat = new XmlSerializer(typeof(ServoMotorGroup), new Type[] { typeof(ServoMotor) });
            Stream filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Open);
                ServoMotorGroup sm = xmlFormat.Deserialize(filestream) as ServoMotorGroup;
                filestream.Close();
                return sm;
            }
            catch (Exception ex)
            {
                filestream.Close();
                throw new FACException("Some error occurs during load expression file.", ex, typeof(ServoMotorGroup).FullName);
            }
        }

        #endregion


        #region Utils

        public double[] GetValues()
        {
            double[] values = new double[servoMotorList.Count];
            for (int i = 0; i < servoMotorList.Count; i++)
            {
                values[i] = servoMotorList[i].PulseWidth;
            }
            return values;
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < servoMotorList.Count; i++)
            {
                sb.AppendFormat("#{0} {1}; ", i, Decimal.Round((decimal)servoMotorList[i].PulseWidth, 4));
            }
            return sb.ToString();
        }

        #endregion


        #region Send Commands

        // Used by animator (Interpolator task)
        internal void SendCommand()
        {
            if (ComPorts.OpenedPort)
            {
                MemoryStream memStream = null;
                try
                {
                    switch (ComPorts.DeviceType)
                    {
                        case StdType.SSC32:
                            StringBuilder sb = new StringBuilder();

                            //At this point each servo motor value must be != -1 (controlled in MotorTask).
                            //If some servo motor value equals -1, it means that the servo motor
                            //is not configured (those servo motors equal -1 also in the default configuration).
                            for (int i = 0; i < servoMotorList.Count; i++)
                            {
                                //Only motor values != -1 are sent on the serial port
                                if (servoMotorList.ElementAt(i).PulseWidth != -1)
                                {
                                    memStream = servoMotorList.ElementAt(i).GenerateCommand();
                                    sb.Append(new StreamReader(memStream).ReadToEnd());
                                }
                            }
                            sb.Append(String.Format("T{0} \r", time));
                            ComPorts.SelectedPort.WriteLine(sb.ToString());

                            //If the command is sent correctly, FACEBody.CurrentState is updated
                            for (int j = 0; j < servoMotorList.Count; j++)
                            {
                                if (servoMotorList[j].PulseWidth != -1)
                                    FACEBody.CurrentState.ServoMotorsList[j].PulseWidth = servoMotorList[j].PulseWidth;
                            }
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

        // Used only by FACEConfig: without animator
        public void SendRawCommand(int ch, int rawVal, int time)
        {            
            if (ComPorts.OpenedPort)
            {
                MemoryStream memStream = null;
                try
                {
                    switch (ComPorts.DeviceType)
                    {
                        case StdType.SSC32:
                            CommandSSC32 ssc32Com = new CommandSSC32(servoMotorList[ch].Name, ch, rawVal);
                            memStream = ssc32Com.FormatServoMove();
                            StringBuilder sb = new StringBuilder(new StreamReader(memStream).ReadToEnd());
                            sb.Append(String.Format("T{0} \r", time));
                            ComPorts.SelectedPort.WriteLine(sb.ToString());

                            //If the command is sent correctly, the servo motor pulse is updated
                            servoMotorList[ch].UpdateConfigValue(rawVal);
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

        #endregion

    }
}