using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StatusMonitor
{   
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class Controls : Window
    {    
               //
        public bool ptm = false;                    // Flags per la modalità
        public bool ftm = true;                     //
        public bool sbm = false;                    //
        public MainWindow main;

        public Controls()
        {
            InitializeComponent();
            ZoomSlider.Value = 0;
            main = new MainWindow();
           // main.DoNotOpenControls = true;
        }

        public delegate void ZoomSliderValueChangedEventHandler();
        public event ZoomSliderValueChangedEventHandler ZoomSliderValueChanged;
        public delegate void PositionSliderValueChangedEventHandler();
        public event PositionSliderValueChangedEventHandler PositionSliderValueChanged; 

        public delegate void FTModeSelectionEventHandler();
        public event FTModeSelectionEventHandler FTModeSelection;
        public delegate void PTModeSelectionEventHandler();
        public event PTModeSelectionEventHandler PTModeSelection;
        public delegate void SBModeSelectionEventHandler();
        public event SBModeSelectionEventHandler SBModeSelection;

        public delegate void CheckedEventHandler(int id);
        public event CheckedEventHandler Checked;
        public delegate void UncheckedEventHandler(int id);
        public event UncheckedEventHandler Unchecked;


        public delegate void EnterNumberOfSamplesEventHandler();
        public event EnterNumberOfSamplesEventHandler EnterNumberOfSamples;
        public delegate void LeaveNumberOfSamplesEventHandler();
        public event LeaveNumberOfSamplesEventHandler LeaveNumberOfSamples;
        public delegate void EnterPositionEventHandler();
        public event EnterPositionEventHandler EnterPosition;
        public delegate void LeavePositionEventHandler();
        public event LeavePositionEventHandler LeavePosition;

        public delegate void OnclosingEventHandler();
        public event OnclosingEventHandler OnClose;
        public delegate void ResetEventHandler();
        public event ResetEventHandler Reset;

        public void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ZoomSliderValueChanged != null)
                ZoomSliderValueChanged();
        }

        private void ScrollBarMode_Checked(object sender, RoutedEventArgs e)
        {
            //qui come nelle altre avevo messo p=f, f=f, s=t però non funzianava
            ZoomSlider.IsEnabled = true;
            if (SBModeSelection != null)
                SBModeSelection();
        }

        private void PartTimeMode_Checked(object sender, RoutedEventArgs e)
        {
                  
            ZoomSlider.IsEnabled = true;
            if (PTModeSelection != null)
                PTModeSelection();
        }

        private void FullTimeMode_Checked(object sender, RoutedEventArgs e)
        {
            
            ZoomSlider.IsEnabled = false;
            if (FTModeSelection != null)
                FTModeSelection();
        }

        private void LeaveNumberSaplesB_Click(object sender, RoutedEventArgs e)
        {
            if (LeaveNumberOfSamples != null)
                LeaveNumberOfSamples();
        }

        
        private void LeavePositionB_Click(object sender, RoutedEventArgs e)
        {
            if (LeavePosition != null)
                LeavePosition();
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PositionSliderValueChanged != null)
                PositionSliderValueChanged();
        }

        private void NumberOfSamples_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnterNumberOfSamples != null)
                EnterNumberOfSamples();
        }

        private void Position_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnterPosition != null)
                EnterPosition();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int idx = Int32.Parse(((System.Windows.Controls.CheckBox)sender).Uid);
            if (Checked != null)
                Checked(idx);
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            int idx = Int32.Parse(((System.Windows.Controls.CheckBox)sender).Uid);
            if (Unchecked != null)
                Unchecked(idx);
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //main.DoNotOpenControls = false;
            
        }

        public void Window_Closed(object sender, EventArgs e)
        {
            if (OnClose != null)
                OnClose();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (Reset != null)
                Reset();
        }
       
    }
}
