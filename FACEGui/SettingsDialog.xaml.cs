using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Management;

using FACEBodyControl;
using System.IO.Ports;

namespace FACEGui20
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private int[] bitRates;
        private bool settingsPortInitialized = false, settingsDeviceInitialized = false, settingsWebcamInitialized = false;

        /* Saved indexes for comboboxes */
        private string previousDeviceType = "";
        private string previousPortName = "";
        private int previousBitRateIndex = -1;
        private int previousDataBitsIndex = -1;
        private int previousParityIndex = -1;
        private int previousStopBitsIndex = -1;
        private int previousHandshakeIndex = -1;
        private string previousWebcamType = "";

        /* Saved indexes for comboboxes */
        private int defaultDeviceTypeIndex = 1;
        private int defaultPortNameIndex = 8;
        private int defaultBitRateIndex = 9;
        private int defaultDataBitsIndex = 3;
        private int defaultParityIndex = 2;
        private int defaultStopBitsIndex = 1;
        private int defaultHandshakeIndex = 0;
        private int defaultWebcamTypeIndex = -1;

        public delegate void UIPortSettingsEventHandler(string selectedPort, int selectedBitRate, int selectedDataBits,
            Parity selectedParity, StopBits selectedStopBits, Handshake selectedHandshake);
        public event UIPortSettingsEventHandler UIPortSettings;

        public delegate void UIDeviceSettingsEventHandler(string selectedDevice);
        public event UIDeviceSettingsEventHandler UIDeviceSettings;

        public delegate void UIWebcamSettingsEventHandler(int selectedWebcam);
        public event UIWebcamSettingsEventHandler UIWebcamSettings;

        public delegate void UIMessageEventHandler(string message);
        public event UIMessageEventHandler UIMessage;


        public SettingsDialog()
        {
            InitializeComponent();

            InitializeDeviceSettings();
            InitializePortSettings();
            InitializeWebcamSettings();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            //TODO Handle ENTER keyboard
            OkButton.Focus();
        }


        #region Initialization

        /// <summary>
        /// Set initial device parameters. 
        /// </summary>
        internal void InitializeDeviceSettings()
        {
            if (!settingsDeviceInitialized)
            {
                // Display available device type.
                string[] stdType = Enums.getStdType();
                for (int i = 0; i < stdType.Length; i++)
                    standardCombo.Items.Add(stdType[i]);

                standardCombo.SelectedIndex = defaultDeviceTypeIndex;
                settingsDeviceInitialized = true;

                //SaveDevicePreferences();
            }
        }

        /// <summary>
        /// Set initial webcam parameters. 
        /// </summary>
        internal void InitializeWebcamSettings()
        {
            if (!settingsWebcamInitialized)
            {
                // Display available webcam device type.
                string[] camName = Webcams.getWebcamDevice();
                for (int i = 0; i < camName.Length; i++)
                    webcamCombo.Items.Add(camName[i]);

                settingsDeviceInitialized = true;

                //SaveWebcamPreferences();
            }
        }

        /// <summary>
        /// Set initial port parameters. 
        /// </summary>
        internal void InitializePortSettings()
        {
            if (!settingsPortInitialized)
            {
                // Find and display available COM ports.
                ComPorts.FindComPorts();
                DisplayComPorts();

                // Bit Rates options.
                bitRates = new int[] { 300, 600, 1200, 2400, 9600, 14400, 19200, 38400, 57600, 115200, 128000 };
                for (int i = 0; i < bitRates.Length; i++)
                {
                    bitRateCombo.Items.Add(bitRates[i]);
                }
                bitRateCombo.SelectedIndex = defaultBitRateIndex;

                // Data Bits options.
                dataBitsCombo.Items.Add(5);
                dataBitsCombo.Items.Add(6);
                dataBitsCombo.Items.Add(7);
                dataBitsCombo.Items.Add(8);
                dataBitsCombo.SelectedIndex = defaultDataBitsIndex;

                // Parity options.
                parityCombo.Items.Add(Parity.Even);
                parityCombo.Items.Add(Parity.Mark);
                parityCombo.Items.Add(Parity.None);
                parityCombo.Items.Add(Parity.Odd);
                parityCombo.Items.Add(Parity.Space);
                parityCombo.SelectedIndex = defaultParityIndex;

                // Stop Bits options.
                stopBitsCombo.Items.Add(StopBits.None);
                stopBitsCombo.Items.Add(StopBits.One);
                stopBitsCombo.Items.Add(StopBits.OnePointFive);
                stopBitsCombo.Items.Add(StopBits.Two);
                stopBitsCombo.SelectedIndex = defaultStopBitsIndex;

                //  Handshaking options.
                handshakingCombo.Items.Add(Handshake.None);
                handshakingCombo.Items.Add(Handshake.XOnXOff);
                handshakingCombo.Items.Add(Handshake.RequestToSend);
                handshakingCombo.Items.Add(Handshake.RequestToSendXOnXOff);
                handshakingCombo.SelectedIndex = defaultHandshakeIndex;

                settingsPortInitialized = true;

                //SavePortPreferences();
            }
        }

        #endregion


        #region Utilities

        /// <summary>
        /// Display available COM ports in a combo box.
        /// Assumes ComPorts.FindComPorts has been run to fill the myPorts array.
        /// </summary>
        internal void DisplayComPorts()
        {
            //  Clear the combo box and repopulate (in case ports have been added or removed).
            //for (int i = 0; i < ComPorts.PortNames.Length; i++)
            //{
            //    comPortCombo.Items.Add(ComPorts.PortNames[i]);
            //}
            //comPortCombo.SelectedIndex = defaultPortNameIndex;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE Caption like '%USB-to%'");

                foreach (ManagementObject queryObj in searcher.Get())
                {

                    Console.WriteLine("Caption: {0}", queryObj["Caption"]);
                    comPortCombo.Items.Add(queryObj["Caption"]);


                }
            }
            catch (ManagementException e)
            {
                MessageBox.Show(e.Message);
            }
        }


        /// <summary>
        /// Compares stored device parameters with the current parameters.
        /// </summary>
        /// <returns>True if any device parameter has changed.</returns>
        internal bool DeviceParameterChanged()
        {
            return (string.Compare(previousDeviceType, Convert.ToString(standardCombo.SelectedItem), true) != 0);
        }


        /// <summary>
        /// Compares stored port parameters with the current parameters.
        /// </summary>
        /// <returns>True if any port parameter has changed.</returns>
        internal bool PortParameterChanged()
        {
            return ((string.Compare(previousPortName, Convert.ToString(comPortCombo.SelectedItem), true) != 0) |
                (previousBitRateIndex != bitRateCombo.SelectedIndex) |
                (previousDataBitsIndex != dataBitsCombo.SelectedIndex) |
                (previousParityIndex != parityCombo.SelectedIndex) |
                (previousStopBitsIndex != stopBitsCombo.SelectedIndex) |
                (previousHandshakeIndex != handshakingCombo.SelectedIndex));
        }


        /// <summary>
        /// Compares stored webcam parameters with the current parameters.
        /// </summary>
        /// <returns>True if any webcam parameter has changed.</returns>
        internal bool WebcamParameterChanged()
        {
            return (string.Compare(previousWebcamType, Convert.ToString(webcamCombo.SelectedItem), true) != 0);
        }

        #endregion


        #region Save Preferences

        /// <summary>
        /// Save the current device parameters.
        /// Enables learning if a parameter has changed.
        /// </summary>
        private void SaveDevicePreferences()
        {
            previousDeviceType = Convert.ToString(standardCombo.SelectedItem);
        }


        /// <summary>
        /// Save the current webcam device parameters.
        /// Enables learning if a parameter has changed.
        /// </summary>
        private void SaveWebcamPreferences()
        {
            previousWebcamType = Convert.ToString(webcamCombo.SelectedItem);
        }


        /// <summary>
        /// Save the current port parameters.
        /// Enables learning if a parameter has changed.
        /// </summary>
        private void SavePortPreferences()
        {
            previousBitRateIndex = bitRateCombo.SelectedIndex;
            previousDataBitsIndex = dataBitsCombo.SelectedIndex;
            previousParityIndex = parityCombo.SelectedIndex;
            previousStopBitsIndex = stopBitsCombo.SelectedIndex;
            previousHandshakeIndex = handshakingCombo.SelectedIndex;
            previousPortName = Convert.ToString(comPortCombo.SelectedItem);
        }

        #endregion


        #region Restore Buttons

        /// Restore the default values for DeviceSettings comboboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            standardCombo.SelectedIndex = defaultDeviceTypeIndex;
        }


        /// <summary>
        /// Restore the default values for PortSettings comboboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            comPortCombo.SelectedIndex = defaultPortNameIndex;
            bitRateCombo.SelectedIndex = defaultBitRateIndex;
            dataBitsCombo.SelectedIndex = defaultDataBitsIndex;
            parityCombo.SelectedIndex = defaultParityIndex;
            stopBitsCombo.SelectedIndex = defaultStopBitsIndex;
            handshakingCombo.SelectedIndex = defaultHandshakeIndex;
        }


        /// <summary>
        /// Restore the default values for WebcamSettings comboboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebcamRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            webcamCombo.SelectedIndex = defaultWebcamTypeIndex;
        }

        #endregion


        #region Buttons

        /// <summary>
        /// Don't save any changes to the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Apply all changes about PortSettings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string statusMessage = null;
            string deviceStatusMessage = null;

            /*** Set the device parameters ***/
            if (DeviceParameterChanged())
            {
                if (standardCombo.SelectedIndex == -1)
                {
                    deviceStatusMessage = "No device.";
                    if (UIDeviceSettings != null)
                        UIDeviceSettings(deviceStatusMessage);
                }
                else
                {
                    // Set parameters to device type
                    ComPorts.DeviceType = (StdType)standardCombo.SelectedIndex;

                    // Pass parameters to main windows to display them in the status bar
                    if (UIDeviceSettings != null)
                        UIDeviceSettings(standardCombo.SelectedItem.ToString());
                }
                SaveDevicePreferences();
            }

            /***  Set the port parameters ***/
            if (ComPorts.ComPortExists)
            {
                if (PortParameterChanged())
                {
                    if (comPortCombo.SelectedIndex == -1)
                    {
                        statusMessage = "No COM port selected.";

                        // Pass parameters to main windows to display them in the status bar
                        if (UIPortSettings != null)
                            UIPortSettings(string.Empty, Convert.ToInt32(bitRateCombo.SelectedItem),
                                Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                                (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));
                    }
                    else
                    {
                      
                        // Set parameters to COM serial port
                        ComPorts.setParamsPort(comPortCombo.SelectedItem.ToString(), Convert.ToInt32(bitRateCombo.SelectedItem),
                                Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                                (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));

                        // Pass parameters to main windows to display them in the status bar
                        if (UIPortSettings != null)
                            UIPortSettings(comPortCombo.SelectedItem.ToString(), Convert.ToInt32(bitRateCombo.SelectedItem),
                                Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                                (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));

                        statusMessage = "";
                    }

                    SavePortPreferences();
                }
            }
            else
            {
                statusMessage = ComPorts.noComPortsMessage;
            }


            /*** Set the webcam parameters ***/
            if (WebcamParameterChanged())
            {
                if (webcamCombo.SelectedIndex != -1)
                {
                    // Set parameters to webcam device id
                    Webcams.WebcamId = webcamCombo.SelectedIndex;

                    // Pass parameters to main windows to display them in the status bar
                    if (UIWebcamSettings != null)
                        UIWebcamSettings(webcamCombo.SelectedIndex);
                }
                SaveWebcamPreferences();
            }

            if (UIMessage != null)
                UIMessage(statusMessage);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string statusMessage = null;
            string deviceStatusMessage = null;

            /*** Set the device parameters ***/
            if (DeviceParameterChanged())
            {
                if (standardCombo.SelectedIndex == -1)
                {
                    deviceStatusMessage = "No device.";
                    if (UIDeviceSettings != null)
                        UIDeviceSettings(deviceStatusMessage);
                }
                else
                {
                    // Set parameters to device type
                    ComPorts.DeviceType = (StdType)standardCombo.SelectedIndex;

                    // Pass parameters to main windows to display them in the status bar
                    if (UIDeviceSettings != null)
                        UIDeviceSettings(standardCombo.SelectedItem.ToString());
                }
                SaveDevicePreferences();
            }

            /*** Set the port parameters ***/
            if (ComPorts.ComPortExists)
            {
                if (PortParameterChanged())
                {
                    if (comPortCombo.SelectedIndex == -1)
                    {
                        statusMessage = "No COM port selected.";
                        if (UIMessage != null)
                            UIMessage(statusMessage);
                    }
                    else
                    {
                        // Set parameters to COM serial port
                        ComPorts.setParamsPort(comPortCombo.SelectedItem.ToString(), Convert.ToInt32(bitRateCombo.SelectedItem),
                                Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                                (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));

                        // Pass parameters to main windows to display them in the status bar
                        if (UIPortSettings != null)
                            UIPortSettings(comPortCombo.SelectedItem.ToString(), Convert.ToInt32(bitRateCombo.SelectedItem),
                                Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                                (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));
                    }
                    SavePortPreferences();
                }
            }
            else
            {
                statusMessage = ComPorts.noComPortsMessage;
                if (UIMessage != null)
                    UIMessage(statusMessage);
            }

            // Set parameters to webcam device id
            Webcams.WebcamId = webcamCombo.SelectedIndex;

            /*** Set the webcam parameters ***/
            if (WebcamParameterChanged())
            {
                if (webcamCombo.SelectedIndex != -1)
                {
                    // Pass parameters to main windows to display them in the status bar
                    if (UIWebcamSettings != null)
                        UIWebcamSettings(webcamCombo.SelectedIndex);
                }
                SaveWebcamPreferences();
            }

            Close();
        }

        #endregion

    }
}