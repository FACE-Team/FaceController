using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;

namespace FACEBodyControl
{
    public class ComPorts
    {
        /*  MOVE  */
        private static StdType deviceType;
        public static StdType DeviceType
        {
            get { return deviceType; }
            set { deviceType = value; }
        }
        /*  MOVE  */


        private static bool comPortExists;
        public static bool ComPortExists
        {
            get { return comPortExists; }
        }

        private static string[] portNames;
        public static string[] PortNames
        {
            get { return portNames; }
        }

        private static SerialPort previousPort = new SerialPort();
        public static SerialPort PreviousPort
        {
            get { return previousPort; }
            set { previousPort = value; }
        }

        private static SerialPort selectedPort = new SerialPort();
        public static SerialPort SelectedPort
        {
            get { return selectedPort; }
            set { selectedPort = value; }
        }

        private static bool isSelected;
        public static bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; }
        }

        private static bool portChanged;
        internal static bool PortChanged
        {
            get { return portChanged; }
            set { portChanged = value; }
        }

        private static bool parameterChanged;
        internal static bool ParameterChanged
        {
            get { return parameterChanged; }
            set { parameterChanged = value; }
        }

        private static bool openedPort;
        public static bool OpenedPort
        {
            get { return openedPort; }
            set { openedPort = value; }
        }

        public static string noComPortsMessage = "No COM ports found.";


        /// <summary>
        /// Find the PC's COM ports and store parameters for each port.
        /// Use saved parameters if possible, otherwise use default values.  
        /// </summary>
        /// <remarks> 
        /// The ports can change if a USB/COM-port converter is attached or removed,
        /// so this routine may need to run multiple times.
        /// </remarks>
        public static void FindComPorts()
        {
            portNames = SerialPort.GetPortNames();

            // If there is at least one COM port
            if (portNames.Length > 0)
            {
                comPortExists = true;
                Array.Sort(portNames);
            }
            else
            {
                // No COM ports found.
                comPortExists = false;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="comName"></param>
        /// <param name="bitRate"></param>
        /// <param name="dataBit"></param>
        /// <param name="p"></param>
        /// <param name="stopBit"></param>
        /// <param name="hs"></param>
        public static void setParamsPort(string comName, int bitRate, int dataBit, Parity p, StopBits stopBit, Handshake hs)
        {
            try
            {
                int start = comName.IndexOf("(");
                int end = comName.IndexOf(")");
                string result = comName.Substring(start, end - start);
                result = result.Replace("(", "");

                // Save previous port
                PreviousPort = SelectedPort;

                // Set the new serial port preferences
                SelectedPort.PortName = result;
                SelectedPort.BaudRate = bitRate;
                SelectedPort.DataBits = dataBit;
                SelectedPort.Parity = p;
                SelectedPort.StopBits = stopBit;
                SelectedPort.Handshake = hs;

                IsSelected = true;
            }
            catch (ArgumentException aEx)
            {
                ParameterChanged = true;
                PortChanged = true;
                //DisplayException(ModuleName, aEx);
            }
            catch (InvalidOperationException iopEx)
            {
                ParameterChanged = true;
                PortChanged = true;
                //DisplayException(ModuleName, iopEx);
            }
            catch (IOException ioEx)
            {
                ParameterChanged = true;
                PortChanged = true;
                //DisplayException(ModuleName, ioEx);
            }

        }


        /// <summary>
        /// Open the SerialPort object selectedPort.
        /// If open, close the SerialPort object previousPort.
        /// </summary>
        public static bool OpenComPort()
        {
            bool success = false;
            //SerialDataReceivedEventHandler1 = new SerialDataReceivedEventHandler(DataReceived);
            //SerialErrorReceivedEventHandler1 = new SerialErrorReceivedEventHandler(ErrorReceived);

            try
            {
                if (comPortExists)
                {
                    //  The system has at least one COM port.
                    //  If the previously selected port is still open, close it.
                    if (PreviousPort.IsOpen)
                    {
                        //CloseComPort(PreviousPort);
                        PreviousPort.Close();
                    }

                    if (!(SelectedPort.IsOpen) | PortChanged)
                    {
                        SelectedPort.Open();

                        if (SelectedPort.IsOpen)
                        {
                            OpenedPort = true;

                            // The port is open. Set additional parameters.
                            // Timeouts are in milliseconds.
                            SelectedPort.DtrEnable = true;
                            SelectedPort.RtsEnable = true;
                            //SelectedPort.ReadTimeout = 5000;
                            //SelectedPort.WriteTimeout = 5000;

                            // Specify the routines that run when a DataReceived or ErrorReceived event occurs
                            //SelectedPort.DataReceived += SerialDataReceivedEventHandler1;
                            //SelectedPort.ErrorReceived += SerialErrorReceivedEventHandler1;

                            success = true;

                            // The port is open with the current parameters.
                            PortChanged = false;

                            // Update previous port
                            PreviousPort = SelectedPort;
                        }
                    }
                }
            }

            catch (InvalidOperationException ex)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = false;
                //DisplayException(ModuleName, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = false;
                //DisplayException(ModuleName, ex);
            }
            catch (System.IO.IOException ex)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = false;
                //DisplayException(ModuleName, ex);
            }

            return success;
        }


        /// <summary>
        /// If the COM port is open, close it.
        /// </summary>
        /// <param name="portToClose"> the SerialPort object to close </param>  
        public static void CloseComPort()
        {
            try
            {
                if (SelectedPort != null)
                {
                    if (SelectedPort.IsOpen)
                    {
                        SelectedPort.Close();
                        OpenedPort = false;
                    }
                }
            }

            catch (InvalidOperationException ex)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = true;
                //DisplayException(ModuleName, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = true;
                //DisplayException(ModuleName, ex);
            }
            catch (IOException ex)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = true;
                //DisplayException(ModuleName, ex);
            }
            catch (ArgumentException aEx)
            {
                ParameterChanged = true;
                PortChanged = true;
                OpenedPort = true;
                //DisplayException(ModuleName, ex);
            }
        }

    }
}