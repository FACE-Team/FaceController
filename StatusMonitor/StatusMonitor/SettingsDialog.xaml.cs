using System;
using System.Collections.Generic;
using System.Management;
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
using System.IO.Ports;

namespace StatusMonitor
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private int[] bitRates;
        private bool settingsPortInitialized;
        private SerialPort port;
                
        public delegate void UIPortSettingsEventHandler(string selectedPort, int selectedBitRate, int selectedDataBits,
            Parity selectedParity, StopBits selectedStopBits, Handshake selectedHandshake);
        public event UIPortSettingsEventHandler UIPortSettings;
    
        public delegate void UIMessageEventHandler(string message);
        public event UIMessageEventHandler UIMessage;
/*
        if (UIMessage != null)
                UIMessage(statusMessage);
            Close();
*/
        public SettingsDialog()
        {
            InitializeComponent();
            InitializePortSettings();
        }

        public SettingsDialog(SerialPort serialport)
        {
            InitializeComponent();

            port = serialport;
            InitializePortSettings();
        }


        internal void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            OkButton.Focus();
        }


        #region Initialization

        internal void InitializePortSettings()
        {
            if (!settingsPortInitialized)
            {
                DisplayComPorts();

                bitRates = new int[] { 300, 600, 1200, 2400, 9600, 14400, 19200, 38400, 57600, 115200, 128000 };
                for (int i = 0; i < bitRates.Length; i++)
                {
                    bitRateCombo.Items.Add(bitRates[i]);
                }
                bitRateCombo.SelectedItem = port.BaudRate;

                // Data Bits options.
                dataBitsCombo.Items.Add(5);
                dataBitsCombo.Items.Add(6);
                dataBitsCombo.Items.Add(7);
                dataBitsCombo.Items.Add(8);
                dataBitsCombo.SelectedItem = port.DataBits;

                // Parity options.
                parityCombo.Items.Add(Parity.Even);
                parityCombo.Items.Add(Parity.Mark);
                parityCombo.Items.Add(Parity.None);
                parityCombo.Items.Add(Parity.Odd);
                parityCombo.Items.Add(Parity.Space);
                parityCombo.SelectedItem = port.Parity;
                
                // Stop Bits options.
                stopBitsCombo.Items.Add(StopBits.None);
                stopBitsCombo.Items.Add(StopBits.One);
                stopBitsCombo.Items.Add(StopBits.OnePointFive);
                stopBitsCombo.Items.Add(StopBits.Two);
                stopBitsCombo.SelectedItem = port.StopBits;

                //  Handshaking options.
                handshakingCombo.Items.Add(Handshake.None);
                handshakingCombo.Items.Add(Handshake.XOnXOff);
                handshakingCombo.Items.Add(Handshake.RequestToSend);
                handshakingCombo.Items.Add(Handshake.RequestToSendXOnXOff);
                handshakingCombo.SelectedItem = port.Handshake;

                settingsPortInitialized = true;
            }
        }
        
        private void DisplayComPorts()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE Caption like '%Arduino%'");

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
            //for (int i = 0; i < SerialPort.GetPortNames().Length; i++)
            //{

            //    comPortCombo.Items.Add(SerialPort.GetPortNames()[i]);
            //}
            comPortCombo.SelectedItem = port.PortName;
        }

        #endregion


        #region Buttons

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string statusMessage = "No COM port selected.";

            if (SerialPort.GetPortNames().Length > 0)
            {
                if (comPortCombo.SelectedIndex > -1)
                {
                    statusMessage = "";
                    // Pass parameters to main windows to display them in the status bar
                    if (UIPortSettings != null)
                        UIPortSettings(comPortCombo.SelectedItem.ToString(), Convert.ToInt32(bitRateCombo.SelectedItem),
                            Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                            (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));
                }
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
            string statusMessage = "No COM port selected.";

            if (SerialPort.GetPortNames().Length > 0)
            {                
                if (comPortCombo.SelectedIndex > -1)
                {
                    statusMessage = "";
                    // Pass parameters to main windows to display them in the status bar
                    if (UIPortSettings != null)
                        UIPortSettings(comPortCombo.SelectedItem.ToString(), Convert.ToInt32(bitRateCombo.SelectedItem),
                            Convert.ToInt32(dataBitsCombo.SelectedItem), (Parity)(parityCombo.SelectedItem),
                            (StopBits)(stopBitsCombo.SelectedItem), ((Handshake)(handshakingCombo.SelectedItem)));
                }
            }

            if (UIMessage != null)
                UIMessage(statusMessage);
            Close();
        }

        #endregion

    }
}

