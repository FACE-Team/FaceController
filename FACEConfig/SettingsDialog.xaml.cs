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

namespace FACEConfig
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private int[] bitRates;
        private bool settingsPortInitialized, settingsDeviceInitialized;

        /*
        // Saved indexes for comboboxes
        private string previousDeviceType;
        private string previousPortName;
        private int previousBitRateIndex;
        private int previousDataBitsIndex;
        private int previousParityIndex;
        private int previousStopBitsIndex;
        private int previousHandshakeIndex;

        // Saved indexes for comboboxes
        private int defaultDeviceTypeIndex = -1;
        private int defaultPortNameIndex = -1;
        private int defaultBitRateIndex = 4;
        private int defaultDataBitsIndex = 3;
        private int defaultParityIndex = 2;
        private int defaultStopBitsIndex = 1;
        private int defaultHandshakeIndex = 0;
        */

        /* Saved indexes/values for comboboxes */
        private string previousDeviceType = "";
        private string previousPortName = "";
        private int previousBitRateIndex = -1;
        private int previousDataBitsIndex = -1;
        private int previousParityIndex = -1;
        private int previousStopBitsIndex = -1;
        private int previousHandshakeIndex = -1;
        private int previousMinLimit = -1;
        private int previousMaxLimit = -1;

        /* Indexes/values for initialization */
        private int defaultDeviceTypeIndex = -1;
        private int defaultPortNameIndex = 8;
        private int defaultBitRateIndex = 9;
        private int defaultDataBitsIndex = 3;
        private int defaultParityIndex = 2;
        private int defaultStopBitsIndex = 1;
        private int defaultHandshakeIndex = 0;
        private int defaultMinLimit = 500;
        private int defaultMaxLimit = 2500;

        public delegate void UIPortSettingsEventHandler(string selectedPort, int selectedBitRate, int selectedDataBits,
            Parity selectedParity, StopBits selectedStopBits, Handshake selectedHandshake);
        public event UIPortSettingsEventHandler UIPortSettings;

        public delegate void UIDeviceSettingsEventHandler(string selectedDevice, string msgDevice, int minLimit, int maxLimit, string msgLimit);
        public event UIDeviceSettingsEventHandler UIDeviceSettings;

        public delegate void UIMessageEventHandler(string message);
        public event UIMessageEventHandler UIMessage;

        public SettingsDialog()
        {
            InitializeComponent();

            InitializeDeviceSettings();
            InitializePortSettings();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            //OkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            //this.AcceptButton = OkButton;

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

                minValueBox.Text = defaultMinLimit.ToString();
                maxValueBox.Text = defaultMaxLimit.ToString();
                //SaveDevicePreferences();
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

            ////  Clear the combo box and repopulate (in case ports have been added or removed).
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
            return ((string.Compare(previousDeviceType, Convert.ToString(standardCombo.SelectedItem), true) != 0) |
                 (previousMinLimit != Convert.ToInt32(minValueBox.Text)) | (previousMaxLimit != Convert.ToInt32(maxValueBox.Text)));
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

        #endregion


        #region Save Preferences

        /// <summary>
        /// Save the current device parameters.
        /// Enables learning if a parameter has changed.
        /// </summary>
        private void SaveDevicePreferences()
        {
            previousDeviceType = Convert.ToString(standardCombo.SelectedItem);
            defaultMinLimit = Convert.ToInt32(minValueBox.Text);
            defaultMaxLimit = Convert.ToInt32(maxValueBox.Text);
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


        /// Restore the default values for DeviceSettings comboboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            standardCombo.SelectedIndex = defaultDeviceTypeIndex;
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

            /*** Set the device parameters ***/
            if (PortParameterChanged())
            {
                ComPorts.DeviceType = (StdType)standardCombo.SelectedIndex;
                FACEBody.MinLimit = Convert.ToInt32(minValueBox.Text);
                FACEBody.MaxLimit = Convert.ToInt32(maxValueBox.Text);

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
            string deviceStatusMessage = "";
            string limitsStatusMessage = "";

            /*** Set the device parameters ***/
            if (standardCombo.SelectedIndex == -1)
            {
                deviceStatusMessage = "No device selected.";
            }
            else
            {
                ComPorts.DeviceType = (StdType)standardCombo.SelectedIndex;
            }

            if (minValueBox.Text == "-1" || maxValueBox.Text == "-1")
            {
                limitsStatusMessage = "No limits set.";
            }
            else
            {
                FACEBody.MinLimit = Convert.ToInt32(minValueBox.Text);
                FACEBody.MaxLimit = Convert.ToInt32(maxValueBox.Text);
            }

            if (UIDeviceSettings != null)
                UIDeviceSettings(ComPorts.DeviceType.ToString(), deviceStatusMessage, FACEBody.MinLimit, FACEBody.MaxLimit, limitsStatusMessage);



            //if (standardCombo.SelectedIndex == -1)
            //{
            //    deviceStatusMessage = "No device.";
            //    if (UIDeviceSettings != null)
            //        UIDeviceSettings(deviceStatusMessage);
            //}
            //else if (minValueBox.Text == "-1" || maxValueBox.Text == "-1")
            //{
            //    deviceStatusMessage = "No limits are set.";
            //    if (UIDeviceSettings != null)
            //        UIDeviceSettings(deviceStatusMessage);
            //}
            //else
            //{
            //    // Set parameters to device type
            //    //ServoMotorGroup.DeviceType = standardCombo.SelectedItem.ToString();
            //    ComPorts.DeviceType = (StdType)standardCombo.SelectedIndex;

            //    // Pass parameters to main windows to display them in the status bar
            //    if (UIDeviceSettings != null)
            //        UIDeviceSettings(standardCombo.SelectedItem.ToString());
            //}
            SaveDevicePreferences();

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

            Close();
        }

        #endregion

    }
}

