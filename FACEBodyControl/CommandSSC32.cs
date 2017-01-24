using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FACEBodyControl
{
    [Serializable]
    public class CommandSSC32 : Command
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandSSC32() { }

        /// <summary>
        /// Initializes a new instance of the CommandSSC32 class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel"></param>
        /// <param name="pulse"></param>
        public CommandSSC32(string name, int channel, int pulse) : base(name, channel, pulse) { }

        
        /// <summary>
        /// Format the command to send on serial port
        /// </summary>
        /// <returns></returns>
        public override MemoryStream FormatServoMove()
        {
            StringBuilder sb = new StringBuilder();
            MemoryStream memStream = null;

            try
            {
                sb.AppendFormat("#{0} P{1} ", Channel, PulseWidth);                
                memStream = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
            }
            catch
            {
                memStream.Close();
                memStream = null;
            }

            return memStream;
        }

        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}: #{1} P{2} \n", Name, Channel, PulseWidth);
            return sb.ToString();
        }

    }
}