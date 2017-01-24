using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;
using System.Timers;
using System.IO;
using System.IO.Ports;
using System.Windows.Threading;

namespace StatusMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // public MainWindow mainwindow;
        public Controls controls;
        public ChartWindow[] chartwindow;
        private SettingsDialog setDialog;
        private SerialPort comport;
        public Stopwatch timer;
        bool OutOfRange;
        public float[] ParsedValues = new float[8];
        public float[] power = new float[3];
        bool stopUpdateNOS = false; // NOS sta per number of samples
        bool stopUpdateP = false;   // P sta per position
        public int time = 0;
        bool Jump;
        private float min = 0, max = 9;             //Valore minimo e massimo per i grafici nella direzione y
        private string[] CseriesNames;              //Nomi delle serie per i grafici
        private string[] VseriesNames;              //              "
        private string[] PseriesNames;              //              "
        private string[] TseriesNames;
        string[] seriesname;
        int numberofseries;
        int NewNumberOfSamples;
        int VISIBLE_POINTS = 0;
        double WindowSize = 0;
        double NumberOfSamples;
        double TotalTime = 0;
        double Position;
        // string[] voltage = new string[3];
        //string[] current = new string[3];
        bool PartialOrFull = true;
        float minview = 0;
        float maxview = 0;
        double mi = 0;
        double ma = 0;
        bool ftm = true;
        bool sbm = false;
        bool ptm = false;
        bool[] OpenedChart = new bool[4];
        //public bool DoNotOpenControls = false;
        string LineRead;
        public float[] energy = new float[3];
        int mavaf = 0;
        public MainWindow()
        {
            InitializeComponent();

         

            CseriesNames = new string[] { "I1", "I2", "I3" };
            VseriesNames = new string[] { "V1", "V2", "V3" };
            PseriesNames = new string[] { "P1", "P2", "P3" };
            TseriesNames = new string[] { "T1", "T2" };
            for (int i = 0; i < 4; i++)
            {
                OpenedChart[i] = false;
            }
            InitializeEnergyArray();
            timer = new Stopwatch();
            comport = new SerialPort();
            comport.PortName = "COM6";
            comport.BaudRate = 9600;
            comport.DataBits = 8;
            comport.Parity = Parity.None;
            comport.StopBits = StopBits.One;
            comport.Handshake = Handshake.None;
            comport.DataReceived += new SerialDataReceivedEventHandler(SelectedPort_DataReceived);
            timer.Start();
            long timeElapsedi = timer.ElapsedMilliseconds;
            
        }

        public void InitializeEnergyArray()
        {
            for (int i = 0; i < 3; i++)
            {
                energy[i] = 0;
            }
        }

        void SelectedPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        { /*
            long timeElapsedf = timer.ElapsedMilliseconds;         //  Righe per misurare il tempo tra un evento di-
            long DeltaTimeElapsed = timeElapsedf - timeElapsedi;   // -ricezione e un'altro, utili per sapere qual'è-
            timeElapsedi = timeElapsedf;                           // -la massima risoluzione temporale raggiungibile.
            this.Dispatcher.Invoke                                 //
                (                                                  //
                System.Windows.Threading.DispatcherPriority.Normal,//
                (System.Threading.ThreadStart)delegate             //
                { textBlock.Text += DeltaTimeElapsed.ToString();   //
                textBlock.Text +="\n";}                            //
                );                                                 //
          */
                try { LineRead = comport.ReadLine(); }
                catch (Exception) { }  
                char seps = '#';
                string[] Values = LineRead.Split(seps);
                OutOfRange = false;

                for (int i = 0; i < 6; i++)
                {
                    try
                    {
                        ParsedValues[i] = Single.Parse(Values[i], System.Globalization.CultureInfo.InvariantCulture);  //traduce in float la stringa contenente i valori
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        OutOfRange = true;
                    }
                    catch (System.FormatException)
                    {
                        OutOfRange = true;
                    }
                }

                
                if (OutOfRange == false)
                {
                    this.Dispatcher.Invoke (System.Windows.Threading.DispatcherPriority.Normal, (System.Threading.ThreadStart)delegate
                    {
                        time++;
                        
                        if(controls!=null)
                        controls.ZoomSlider.Maximum = time;
                        for (int i = 0; i < 4; i++)
                        {   
                            if(chartwindow!=null && chartwindow[i]!=null)
                            {
                                NumberOfSamples = chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.ViewMaximum - chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.ViewMinimum + 1;
                                Position = ((int)(chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Position) * 0.5);
                            }
                        }

                        NumberOfSamples = (int)NumberOfSamples;
                        WindowSize = (NumberOfSamples-1) * 0.5; //2campioni/secondo
                        TotalTime = time * 0.5;

                        if (controls != null)
                        {
                            controls.WindowSize.Text = "" + WindowSize + "";
                            controls.WindowSize.Text += "s";
                            controls.TotalTime.Text = "" + TotalTime + "";
                            controls.TotalTime.Text += "s";
                        }
                        if (stopUpdateNOS == false && controls!=null)
                        {
                            controls.NumberOfSamples.Text = "" + NumberOfSamples + "";
                           // controls.NumberOfSamples.Text += "inDataReceived";
                        }
                        if (stopUpdateP == false && controls!=null)
                        {
                            controls.Position.Text = "" + Position + "";
                            controls.Position.Text += "s";
                        }

                        //-20 in ParsedValues[i] indica che per il corrispondente sensore 
                        //non ha fornito un valore perchè la sua frequenza di campionamento è inferiore a quella
                        //del sensore che ha la frequenza di campionamento più alta
                        if (ParsedValues[0] != -20) 
                        {   
                            textBox1.Text = Values[0];
                            progressBar1.Value = ParsedValues[0];// se non è -20 lo copia
                        }
                        else { ParsedValues[0]=(float)progressBar1.Value; }
                        //se è -20 copia il valore della progBar precedentemente aggiornato, cioè il valore non cambia fino a quando 
                        //non arriva un'altro valore valido (diverso da -20),la progBar non ha mai nessun valore in quanto nell codice
                        // XAML è inizializzato con il valore 0 quindi non può dare errore
                        
                        if (ParsedValues[1] != -20)
                        {
                            textBox2.Text = Values[1];
                            progressBar2.Value = ParsedValues[1];
                        }else { ParsedValues[1]=(float)progressBar2.Value;}
                        
                        if (ParsedValues[2] != -20)
                        {
                            textBox3.Text = Values[2];
                            progressBar3.Value = ParsedValues[2];
                        }else { ParsedValues[2]=(float)progressBar3.Value;}

                        if (ParsedValues[3] != -20)
                        {
                            textBox4.Text = Values[3];
                            progressBar4.Value = ParsedValues[3];
                        }else { ParsedValues[3]=(float)progressBar4.Value; }
                        
                        if (ParsedValues[4] != -20)
                        {
                            textBox5.Text = Values[4];
                            progressBar5.Value = ParsedValues[4];
                        }else { ParsedValues[4]=(float)progressBar5.Value;}
                        
                        if (ParsedValues[5] != -20)
                        {
                            textBox6.Text = Values[5];
                            progressBar6.Value = ParsedValues[5];
                        }else { ParsedValues[5]=(float)progressBar6.Value; }

                        mavaf++;
                        Console.WriteLine(mavaf+";"+ Values[0] + ";" + Values[1] + ";" + Values[2] + ";" + Values[3] + ";" + Values[4] + ";" + Values[5]);
                   
                        progressBar7.Value = (ParsedValues[0] * ParsedValues[1]) + (ParsedValues[2] * ParsedValues[3]) + (ParsedValues[4] * ParsedValues[5]);
                        textBox7.Text = (Math.Round((decimal)progressBar7.Value, 3)).ToString();

                        if (Values.Length > 5)
                        {
                            byte lastVal = Byte.Parse(Values[6]);

                            if ((lastVal & 128) != 0)
                            {
                                ch1.IsChecked = true;
                            }
                            else ch1.IsChecked = false;

                            if ((lastVal & 64) != 0)
                            {
                                ch2.IsChecked = true;
                            }
                            else ch2.IsChecked = false;

                            if ((lastVal & 32) != 0)
                            {
                                ch3.IsChecked = true;
                            }
                            else ch3.IsChecked = false;

                            if ((lastVal & 16) != 0)
                            {
                                ch4.IsChecked = true;
                            }
                            else ch4.IsChecked = false;

                            if ((lastVal & 8) != 0)
                            {
                                ch5.IsChecked = true;
                            }
                            else ch5.IsChecked = false;

                            if ((lastVal & 4) != 0)
                            {
                                ch6.IsChecked = true;
                            }
                            else ch6.IsChecked = false;
                        }

                    });

                    //alla fine avremo la sicurezza di avere dei valori validi in parsedvalues in cui non ci sono 
                    //-20 di mezzo e quindi possiamo procedere a calcolare la potenza ed energia e aggiornare i grafici

                    int j = 0;
                    for (int i = 0; i < 5; i = i + 2) //Calcola la potenza e aggiorna i grafici della corrente, voltaggio e potenza
                    {
                        power[j] = ParsedValues[i] * ParsedValues[i + 1];
                        energy[j] += (float)(power[j] * 0.5);
                        if (chartwindow != null)
                        {
                            if (chartwindow[0] != null)   //  corrente
                            { UpdateChart(chartwindow[0].Chart0, ParsedValues[i], time, j); }
                            if (chartwindow[1] != null)   //  voltaggio 
                            { UpdateChart(chartwindow[1].Chart0, ParsedValues[i + 1], time, j); }
                            if (chartwindow[2] != null)   //  potenza
                            { UpdateChart(chartwindow[2].Chart0, power[j], time, j); }
                            j++;
                        }
                    }
                    // aggiorno grafici della temperatura

                    if (chartwindow != null && chartwindow[3] != null) //nota: cambiando l'ordine da eccezione
                    {
                        UpdateChart(chartwindow[3].Chart0, ParsedValues[6], time, 0);
                        UpdateChart(chartwindow[3].Chart0, ParsedValues[7], time, 1);
                    }

                    UpdateEnergyTextBox();
                }
        }

        #region Settings

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            setDialog = new SettingsDialog(comport);
            setDialog.UIPortSettings += new SettingsDialog.UIPortSettingsEventHandler(SaveAndDisplayCurrentPortSettings);
            setDialog.UIMessage += new SettingsDialog.UIMessageEventHandler(DisplayCurrentMessage);
            setDialog.ShowDialog();
        }

        //Data coming from the SettingsDialog are saved and displayed
        private void SaveAndDisplayCurrentPortSettings(string selectedPort, int selectedBitRate, int selectedDataBits, Parity selectedParity, StopBits selectedStopBits, Handshake selectedHandshake)
        {
            int start = selectedPort.IndexOf("(");
            int end = selectedPort.IndexOf(")");
            string result = selectedPort.Substring(start, end - start);
            result = result.Replace("(", "");
            comport.PortName = result;

            comport.BaudRate = (int)setDialog.bitRateCombo.SelectedItem;
            comport.DataBits = (int)setDialog.dataBitsCombo.SelectedItem;
            comport.Parity = (Parity)setDialog.parityCombo.SelectedItem;
            comport.StopBits = (StopBits)setDialog.stopBitsCombo.SelectedItem;
            comport.Handshake = (Handshake)setDialog.handshakingCombo.SelectedItem;

            TextNamePort.Text = selectedPort;
            TextStatusPort.Text = "";

            if (selectedPort != "")
            {
                if (comport.IsOpen)
                {
                    TextStatusPort.Text = "Opened";
                }
                else
                {
                    TextStatusPort.Text = "Closed";
                }
            }
        }

        private void DisplayCurrentMessage(string msg)
        {
            StatusbarInfo.Text = msg;
        }

        #endregion

        public void UpdateEnergyTextBox()
        {
            if (controls != null)
            {
                controls.Dispatcher.Invoke((Action)(() =>
                {
                    controls.Ch1Energy.Text = "" + energy[0] + "";
                    controls.Ch2Energy.Text = "" + energy[1] + "";
                    controls.Ch3Energy.Text = "" + energy[2] + "";

                }));
            }
        }

        public void ZoomSliderFunction()
        {
            //Position = chartwindow[0].Chart0.ChartAreas[0].AxisX.ScaleView.Position * 0.5;
            //Position = (int)controls.PositionSlider.Value; // Position è in numero di campioni!!
            if (sbm == true)
            {
                int VISIBLE_POINTSf = (int)controls.ZoomSlider.Value;
                int DeltaVisiblePoint = VISIBLE_POINTSf - VISIBLE_POINTS;
                for (int i = 0; i < 4; i++)
                {
                    if (chartwindow[i] != null)
                    {
                        mi = chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.ViewMinimum - DeltaVisiblePoint / 2; // se si toglie /2 è più "veloce" lo zoom , il problema è che arrivato al minimo
                        ma = chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.ViewMaximum + DeltaVisiblePoint / 2; // dei punti visualizzabili cambia anche di posizione
                    }
                }
                ChangeView(mi, ma);
            }

            VISIBLE_POINTS = (int)controls.ZoomSlider.Value; // aggiorna il valore dei VISIBLE_POINTS
            controls.PositionSliderValueChanged -= new Controls.PositionSliderValueChangedEventHandler(PositionSliderFunction);
            for (int i = 0; i < 4; i++)
            {
                if (chartwindow[i] != null)
                    controls.PositionSlider.Value = chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Position;
            }
            Position = (int)(controls.PositionSlider.Value) * 0.5;
            for (int i = 0; i < 4; i++)
            {
                if (chartwindow[i] != null)
                    NumberOfSamples = chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.ViewMaximum - chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.ViewMinimum + 1;
            }

            NumberOfSamples = (int)NumberOfSamples;
            WindowSize = (NumberOfSamples - 1) * 0.5; //2campioni/secondo
            controls.WindowSize.Text = "" + WindowSize + "";
            controls.WindowSize.Text += "s";
            controls.NumberOfSamples.Text = "" + NumberOfSamples + "";
            // controls.NumberOfSamples.Text += "inZoom";
            controls.Position.Text = "" + Position + "";
            controls.Position.Text += "s";
            controls.PositionSliderValueChanged += new Controls.PositionSliderValueChangedEventHandler(PositionSliderFunction);

        }

        public void PositionSliderFunction()
        {
            controls.PositionSlider.Maximum = time;
            Position = (int)controls.PositionSlider.Value; // Position è in numero di campioni!!
            controls.Position.Text = "" + Position * 0.5 + ""; // quindi va moltiplicato per 0.5
            for (int i = 0; i < 4; i++)
            {
                if (chartwindow[i] != null)
                { chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Scroll(Position); }
            }
        }


        public void EnterNumberOfSamples()
        {
            if (PartialOrFull == false)
                stopUpdateNOS = true;
        }

        public void LeaveNumberOfSamples()
        {
            if (PartialOrFull == false)
            {
                try
                {
                    Jump = false;
                    NewNumberOfSamples = int.Parse(controls.NumberOfSamples.Text);
                    textBlock.Text = "" + NewNumberOfSamples + "";
                }
                catch (FormatException) { Jump = true; }
                if (Jump == false)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (chartwindow[i] != null)
                            chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Size = NewNumberOfSamples - 3;
                    }
                    /* if (ptm == true)
                     {
                         VISIBLE_POINTS = NewNumberOfSamples;
                         controls.ZoomSliderValueChanged -= new Controls.ZoomSliderValueChangedEventHandler(PositionSliderFunction);
                         controls.ZoomSlider.Value = NewNumberOfSamples;
                         controls.ZoomSliderValueChanged += new Controls.ZoomSliderValueChangedEventHandler(PositionSliderFunction);
                     }*/
                }
                stopUpdateNOS = false;
            }
        }
        
        public void EnterPosition()
        {
            if (PartialOrFull == false)
                stopUpdateP = true;
        }
        
        public void LeavePosition()
        {
            if (PartialOrFull == false)
            {
                Position = double.Parse(controls.Position.Text); //espresso in secondi
                double PositionInSamples = Position * 2;
                for (int i = 0; i < 4; i++)
                {
                    if (chartwindow[i] != null)
                    { chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Scroll(PositionInSamples); }
                }
                stopUpdateP = false;
                controls.PositionSlider.Value = PositionInSamples;
            }
        }
     
        public void ChangeView(double MinView, double MaxView)
        {
            for (int i = 0; i < 4; i++)
            {
                if (chartwindow[i] != null)
                    chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Zoom(MinView, MaxView);
            }
        }

        public void FTM()
        {
            ptm = false;
            sbm = false;
            ftm = true;
            PartialOrFull = true;
            DisablePB();
            if (controls != null)
                controls.PositionSlider.IsEnabled = false;
        }
     
        public void PTM()
        {
            sbm = false;
            ftm = false;
            ptm = true;
            PartialOrFull = true;
            DisablePB();
            if (controls != null)
                controls.PositionSlider.IsEnabled = false;
        }
     
        public void SBM()
        {
            sbm = true;
            ftm = false;
            ptm = false;
            PartialOrFull = false;
            EnablePB();
            if (controls != null)

                controls.PositionSlider.IsEnabled = true;
            /* for (int i = 0; i < 4; i++)//serve per aggiornare lo slider position, se non ci fosse il primo valore di position 
             {// della textbox (position) a porta spenta, nella funzione zoomslider, sarebbe sbagliato
                 if (chartwindow[i] != null)
                     controls.PositionSlider.Value = chartwindow[i].Chart0.ChartAreas[0].AxisX.ScaleView.Position * 0.5;
             }*/
        }
      
        public void EnablePB()
        {
            for (int i = 0; i < 4; i++)
            {
                if (chartwindow != null && chartwindow[i] != null)
                    chartwindow[i].Chart0.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            }
        }

        public void DisablePB()
        {
            if (chartwindow != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (chartwindow[i] != null)
                        chartwindow[i].Chart0.ChartAreas[0].AxisX.ScrollBar.Enabled = false;                   
                }
            }
        }

        private void UpdateChart(Chart chart, float value, int time, int series)
        {
            try
            {

                if (time > chart.Series[series].Points[chart.Series[series].Points.Count - 1].XValue)
                {
                    chart.BeginInvoke((Action)(() =>
                    {
                        chart.Series[series].Points.AddXY(time, value);  // solo con questa, senza il resto, funziona come in fullmode 

                        if (PartialOrFull == true)
                        {
                            if (ftm == true)
                            {
                                minview = 0;
                                maxview = time;

                            }
                            if (ptm == true)
                            {
                                minview = time - VISIBLE_POINTS;
                                maxview = time;
                            }
                            ChangeView(minview, maxview);

                        }

                    }));
                }
            }
            catch (System.Exception)
            { }
        }

        private void InitChart(Chart chart, double min, double max, int numOfSeries, string[] names)
        {
            chart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(211)))), ((int)(((byte)(223)))), ((int)(((byte)(240)))));
            chart.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            chart.BackSecondaryColor = System.Drawing.Color.White;
            chart.BorderlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            chart.BorderlineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chart.BorderlineWidth = 2;
            chart.BorderSkin.SkinStyle = System.Windows.Forms.DataVisualization.Charting.BorderSkinStyle.Emboss;
            ChartArea chartArea1 = new ChartArea();
            chartArea1.Area3DStyle.Inclination = 15;
            chartArea1.Area3DStyle.IsClustered = true;
            chartArea1.Area3DStyle.IsRightAngleAxes = false;
            chartArea1.Area3DStyle.Perspective = 10;
            chartArea1.Area3DStyle.Rotation = 10;
            chartArea1.Area3DStyle.WallWidth = 0;
            chartArea1.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartArea1.AxisX.Interval = 10;
            chartArea1.AxisX.IntervalType = DateTimeIntervalType.Milliseconds;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea1.AxisX.LabelStyle.Format = "hh:mm:ss";
            chartArea1.AxisX.LabelStyle.Interval = 10;
            chartArea1.AxisX.LabelStyle.IntervalType = DateTimeIntervalType.Milliseconds;
            chartArea1.AxisX.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisX.MajorGrid.Interval = 10;
            chartArea1.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Milliseconds;
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisX.MajorTickMark.Interval = 10;
            chartArea1.AxisX.MajorTickMark.IntervalType = DateTimeIntervalType.Milliseconds;
            chartArea1.AxisY.IsLabelAutoFit = false;
            chartArea1.AxisY.IsStartedFromZero = false;
            chartArea1.AxisY.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            chartArea1.AxisY.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.AxisY.Maximum = max;
            chartArea1.AxisY.Minimum = min;
            chartArea1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(165)))), ((int)(((byte)(191)))), ((int)(((byte)(228)))));
            chartArea1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
            chartArea1.BackSecondaryColor = System.Drawing.Color.White;
            chartArea1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
            chartArea1.InnerPlotPosition.Auto = false;
            chartArea1.InnerPlotPosition.Height = 85F;
            chartArea1.InnerPlotPosition.Width = 86F;
            chartArea1.InnerPlotPosition.X = 8.3969F;
            chartArea1.InnerPlotPosition.Y = 5.63068F;
            chartArea1.Name = "Default";

            chartArea1.Position.Auto = false;
            chartArea1.Position.Height = 86.76062F;
            chartArea1.Position.Width = 88F;
            chartArea1.Position.X = 5.089137F;
            chartArea1.Position.Y = 5.895753F;
            chartArea1.ShadowColor = System.Drawing.Color.Transparent;
            chart.ChartAreas.Add(chartArea1);
            Legend legend1 = new Legend();
            legend1.Alignment = System.Drawing.StringAlignment.Far;
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.DockedToChartArea = "Default";
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            legend1.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Bold);
            legend1.IsTextAutoFit = false;
            legend1.LegendStyle = System.Windows.Forms.DataVisualization.Charting.LegendStyle.Row;
            legend1.Name = "Default";
            chart.Legends.Add(legend1);
            //chart.Location = new System.Drawing.Point(16, 48);
            chart.Name = "chart1";
            for (int i = 0; i < numOfSeries; i++)
            {
                Series series = new Series();
                series.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
                series.ChartArea = "Default";
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                series.Legend = "Default";
                series.Name = names[i];
                series.ShadowOffset = 1;
                series.Points.Add(0, 0);
                chart.Series.Add(series);
                series.MarkerStyle = MarkerStyle.Star5;
                series.MarkerSize = 5;
            }
            //chart.Series.MarkerSize = "5";
            Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            title1.Alignment = System.Drawing.ContentAlignment.TopCenter;
            title1.Font = new System.Drawing.Font("Trebuchet MS", 14.25F, System.Drawing.FontStyle.Bold);
            title1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(59)))), ((int)(((byte)(105)))));
            title1.Name = "Title1";
            title1.ShadowColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            title1.ShadowOffset = 3;
            title1.Text = "Chart";
            chart.Titles.Add(title1);
            chart.Size = new System.Drawing.Size(296, 212);
            chart.TabIndex = 13;
            chartArea1.AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            chartArea1.AxisX.ScaleView.SmallScrollSize = 20;
        }


        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (comport.IsOpen)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(10);
                    //sb.Append();  AGGIUNGERE EVENTUALE SEPARATORE TRA I PARAMETRI
                    sb.Append(20.5f);
                    //sb.Append();  AGGIUNGERE EVENTUALE SEPARATORE TRA I PARAMETRI
                    sb.Append("word");
                    //sb.Append();  AGGIUNGERE EVENTUALI SEPARATORI TRA I PARAMETRI                    
                    sb.Append("\r"); //carriage return
                    comport.WriteLine(sb.ToString());
                }
                catch
                {
                    System.Windows.MessageBox.Show("Command not sent. Some errors occured.");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("There are not opened ports.");
            }
        }

        private void PortButton_Click(object sender, RoutedEventArgs e)
        {
            if (comport.PortName != "")
            {
                if (comport.IsOpen)
                {
                    if(ch5.IsChecked!=true){
                        comport.Close();
                        PortButton.Content = "Open";
                        TextStatusPort.Text = "Closed";
                        ch1.IsEnabled = false;
                        ch2.IsEnabled = false;
                        ch3.IsEnabled = false;
                        ch4.IsEnabled = false;
                        ch5.IsEnabled = false;
                        ch6.IsEnabled = false;
                    
                        SBM();
                        //chartwindow = new ChartWindow[4];
                        if (controls != null)
                        {
                            controls.ScrollBarMode.IsChecked = true;
                            controls.FullTimeMode.IsEnabled = false;
                            controls.PartTimeMode.IsEnabled = false;
                            controls.FullTimeMode.IsChecked = false;
                            controls.PartTimeMode.IsChecked = false;
                        }
                    }

                }
                else
                {
                    try { comport.Open(); }
                    catch (IOException)
                    {
                        textBlock.Text += "The selected Port does not exist";
                        textBlock.Text += "\n";
                    }
                    PortButton.Content = "Close";
                    TextStatusPort.Text = "Opened";
                    ch5.IsEnabled = true;
                    ch6.IsEnabled = true;
                    //chartwindow = new ChartWindow[4];
                    //inizialmente l'avevo inserito per non avere eccezioni però
                    //se tolgo il commento:
                    //aprendo in ordine chart->grafico->open non ci sono eccezioni ma il grafico non viene aggiornato
                    //se metto il commento:
                    //aprendo in ordine chart->grafico->open il grafico viene aggiornato ma ci sono eccezioni che possono 
                    //essere eliminate facendo il confronto chartwindow!=null

                    DisablePB();
                    if (controls != null)
                    {
                        controls.FullTimeMode.IsEnabled = true;
                        controls.PartTimeMode.IsEnabled = true;
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("There are not selected ports!");
            }
        }

        private void controls_OnClose()
        {
            controls = null;
            CloseChart();
        }

        private void ChartWindow_Click(object sender, RoutedEventArgs e)
        {
            if (controls == null)
            {
                controls = new Controls();
                controls.Show();
                controls.ZoomSliderValueChanged += new Controls.ZoomSliderValueChangedEventHandler(ZoomSliderFunction);
                controls.PositionSliderValueChanged += new Controls.PositionSliderValueChangedEventHandler(PositionSliderFunction);
                controls.FTModeSelection += new Controls.FTModeSelectionEventHandler(FTM);
                controls.PTModeSelection += new Controls.PTModeSelectionEventHandler(PTM);
                controls.SBModeSelection += new Controls.SBModeSelectionEventHandler(SBM);
                controls.EnterNumberOfSamples += new Controls.EnterNumberOfSamplesEventHandler(EnterNumberOfSamples);
                controls.LeaveNumberOfSamples += new Controls.LeaveNumberOfSamplesEventHandler(LeaveNumberOfSamples);
                controls.EnterPosition += new Controls.EnterPositionEventHandler(EnterPosition);
                controls.LeavePosition += new Controls.LeavePositionEventHandler(LeavePosition);
                chartwindow = new ChartWindow[4];
                controls.FullTimeMode.IsChecked = true;
                controls.Checked += new Controls.CheckedEventHandler(ShowChart);
                controls.Unchecked += new Controls.UncheckedEventHandler(HideChart);
                controls.OnClose += new Controls.OnclosingEventHandler(controls_OnClose);
                controls.Reset += new Controls.ResetEventHandler(Reset_Click);
            }
        }

        public void MainWindow_CloseChartWindow(int id)
        {
            OpenedChart[id] = false; // falso sta per chiuso
            if (controls != null)
            {
                foreach (object obj in controls.CheckBoxGrid.Children)
                {
                    //per ogni elemento contenuto nella Gid, cerchiamo quelli di tipo CheckBox
                    //e tra questi il controllo che ha Uid uguale a quello passato come parametro
                    if (obj.GetType() == typeof(System.Windows.Controls.CheckBox))
                    {
                        System.Windows.Controls.CheckBox cb = (System.Windows.Controls.CheckBox)obj;
                        if (Int32.Parse(cb.Uid) == id)
                            cb.IsChecked = false; //questo assegnaento richiama automaticamente il metodo checkBoxChart_Unchecked che toglie la spunta
                    }
                }
            }
            chartwindow[id] = null; //la finestra del grafico non esiste più
        }

        public void ShowChart(int id)
        {
            if (chartwindow[id] != null && OpenedChart[id] == true)//se è già stato creato e non è stato chiuso mostralo 
            { chartwindow[id].Visibility = System.Windows.Visibility.Visible; }
            if (chartwindow[id] != null && OpenedChart[id] != true)
            { chartwindow[id].Visibility = System.Windows.Visibility.Visible; }
            if (chartwindow[id] == null && OpenedChart[id] != true)// altrimenti crea e mostra, nel caso in cui è stato chiuso lo ricrea
            {
                chartwindow[id] = new ChartWindow(id);
                chartwindow[id].Show();
                chartwindow[id].CloseChartWindow += new ChartWindow.CloseChartWindowEventHandler(MainWindow_CloseChartWindow);
                switch (id)
                {
                    case 0: seriesname = CseriesNames;
                        numberofseries = 3;
                        break;
                    case 1: seriesname = VseriesNames;
                        numberofseries = 3;
                        break;
                    case 2: seriesname = PseriesNames;
                        numberofseries = 3;
                        break;
                    case 3: seriesname = TseriesNames;
                        numberofseries = 2;
                        break;

                }
                InitChart(chartwindow[id].Chart0, min, max, numberofseries, seriesname);
                chartwindow[id].Chart0.Height = 255;
                chartwindow[id].Chart0.Width = 655;
                OpenedChart[id] = true;
                if (sbm == true)
                { SBM(); }
                if (ftm == true)
                { FTM(); }
                if (ptm == true)
                { PTM(); }
            }
        }

        public void HideChart(int id)
        {
            chartwindow[id].Visibility = System.Windows.Visibility.Hidden;
        }

        private void Reset_Click()
        {
            CloseChart();
            time = 0;
            InitializeEnergyArray();
        }

        private void CloseChart()
        {
            for (int i = 0; i < 4; i++)
            {

                if (chartwindow[i] != null)
                {
                    chartwindow[i].Close();
                }
            }
        }

        #region CheckBox
        private void ch1_Checked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("A");
        }

        private void ch1_Unchecked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("a");
        }

        private void ch2_Checked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("B");
        }

        private void ch2_Unchecked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("b");
        }

        private void ch3_Checked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("C");
        }

        private void ch3_Unchecked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("c");
        }

        private void ch4_Checked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("D");
        }

        private void ch4_Unchecked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("d");
        }

        private void ch5_Checked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("E");
            if (ch6.IsChecked == true) 
            {
                ch1.IsEnabled = true;
                ch2.IsEnabled = true;
                ch3.IsEnabled = true;
                ch4.IsEnabled = true;
                textAllert.Visibility = Visibility.Visible;
            }            

        }

        private void ch5_Unchecked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("e");
        }

        private void ch6_Checked(object sender, RoutedEventArgs e)
        {
            comport.WriteLine("F");
            if (ch5.IsChecked == true)
            {
                ch1.IsEnabled = true;
                ch2.IsEnabled = true;
                ch3.IsEnabled = true;
                ch4.IsEnabled = true;
                textAllert.Visibility = Visibility.Visible;
            }
        }

        private void ch6_Unchecked(object sender, RoutedEventArgs e)
        {
            
            comport.WriteLine("f");
        }
        #endregion

    }


}