using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace FACEBodyControl
{
    [Serializable()]
    public class ServoMotor
    {
        [XmlIgnoreAttribute()]
        internal const int MIN_TIME_VALUE = 0; // Min Time in mS for the entire move
        [XmlIgnoreAttribute()]
        internal const int MAX_TIME_VALUE = 65535; // Max Time in mS for the entire move
        
        private string serialSC;
        private int pt;
        private string servoName;
        private int ch;
        private float pw;
        private float minValue;
        private float maxValue;


        /// <summary>
        /// Serial servo controller
        /// </summary>
        public string SerialSC
        {
            get { return serialSC; }
            set { serialSC = value; }
        }

        /// <summary>
        /// Port number in decimal of servo controllore, 0-n
        /// </summary>
        public int PortSC
        {
            get { return pt; }
            set { pt = value; }
        }
        /// <summary>
        /// Servo motor name
        /// </summary>
        public string Name
        {
            get { return servoName; }
            set { servoName = value; }
        }

        /// <summary>
        /// Channel number in decimal, 0-31
        /// </summary>
        public int Channel
        {
            get { return ch; }
            set { ch = value; }
        }

        /// <summary>
        /// Pulse width in microseconds
        /// </summary>
        public float PulseWidthNormalized
        {
            get { return pw; }
            set { pw = value; }
        }

        /// <summary>
        /// Minimum value of the servo motor between [0,1]
        /// </summary>
        public float MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }

        /// <summary>
        /// Maximum value of the servo motor between [0,1]
        /// </summary>
        public float MaxValue
        {
            get { return maxValue; }
            set { maxValue = value; }
        }


        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>

        public ServoMotor() { }


        /// <summary>
        /// Initializes a new instance of the ServoMove class.
        /// </summary>
        /// <param name="name"> Servo motor name. </param>
        /// <param name="channel"> Channel number in decimal, 0-31. </param>
        /// <param name="pulse"> Pulse width in microseconds. </param>
        /// <param name="min"> Minimum value of the servo motor between [0,1]. </param>
        /// <param name="max"> Maximum value of the servo motor between [0,1]. </param>
        /// <param name="serial">Serial servo controller</param>
        /// <param name="port">Port Servo Controller connectio servo motor</param>
        public ServoMotor(string name, int channel, float pulse, float min, float max, string serial, int port)
        {
            servoName = name;
            ch = channel;
            pw = pulse;
            minValue = min;
            maxValue = max;
            serialSC = serial;
            pt = port;
        }


        /// <summary>
        /// Initializes a new instance of the ServoMove class.
        /// </summary>
        /// <param name="channel"> Channel number in decimal, 0-31. </param>
        /// <param name="pulse"> Pulse width in microseconds. </param>
        /// <param name="serial">Serial servo controller</param>
        public ServoMotor(int channel, float pulse,string serial,int port)
        {
            ch = channel;
            pw = pulse;
            serialSC = serial;
            pt = port;
        }

        #endregion


        #region Generate Command  

        // Used only by FACEConfig

        /// <summary>
        /// Generate a command for the current servo motor.
        /// </summary>
        /// <param name="rawVal">Pulse width in microseconds [500, 2500]</param>
        /// <returns>A stream of data containing the formatted raw command.</returns>

     
        public MemoryStream GenerateRawCommand(int rawVal)
        {
            MemoryStream memStream = new MemoryStream();

            switch (ComPorts.DeviceType)
            {
                case StdType.SSC32:
                    CommandSSC32 ssc32Com = new CommandSSC32(servoName, ch, rawVal);
                    memStream = ssc32Com.FormatServoMove();
                    break;
            }

            return memStream;
        }


        /// <summary>
        /// Generate a command for the current servo motor using the normalized value.
        /// </summary>
        /// <returns>A stream of data containing the formatted command.</returns>

  
        public MemoryStream GenerateCommand()
        {
            MemoryStream memStream = new MemoryStream();
            int mappedPw = MappingOnMinMaxInterval(pw);

            switch (ComPorts.DeviceType)
            {
                case StdType.SSC32:
                    CommandSSC32 ssc32Com = new CommandSSC32(servoName, ch, mappedPw);
                    memStream = ssc32Com.FormatServoMove();
                    break;
            }

            return memStream;
        }

        #endregion


        #region Conversions

        /// <summary>
        /// Convert the pulse width from microseconds [500, 2500] to the range [0,1]. 
        /// </summary>
        /// <returns>The converted value</returns>

        public float MappingOnUnitaryInterval(int val)  //Used in defaultConfig 
        {
            // 0.5 : x = (max-min)/2 : (val-min) ==> x = (val - min) / (max - min)
            float res = (val - minValue) / (maxValue - minValue);
            return res;
        }




        /// <summary>
        /// Convert the pulse width from the range [0,1] to microseconds [500, 2500] 
        /// according to updated Min and Max values.
        /// </summary>
        /// <returns>The converted value</returns>
     
        public int MappingOnMinMaxInterval(float val) //to send command on serial port [500-2500]
        {
            int res = (int)((((maxValue - minValue) * val)) + minValue);
            return res;
        }

        /// <summary>
        /// Convert the pulse width from the range [0,1] to microseconds [500, 2500] 
        /// according to updated Min and Max values.
        /// </summary>
        /// <returns>The converted value</returns>
        public int MappingOnMinMaxInterval() //to send command on serial port [500-2500]
        {
            int res = (int)((((maxValue - minValue) * pw)) + minValue);
            return res;
        }

        #endregion


        #region Utils
 
        internal void UpdateRawValue(int val)
        {
            pw = MappingOnUnitaryInterval(val);
        }

        #endregion

    }
}