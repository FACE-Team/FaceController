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
using System.Windows.Navigation;
using System.Windows.Shapes;
using FACEBodyControl;
using System.Globalization;
using System.Windows.Threading;

namespace FACEGui20
{
    /// <summary>
    /// Interaction logic for ManualControl.xaml
    /// </summary>
    public partial class ManualControl : UserControl
    {
        //private ServoMotorGroup defConfig;
        private List<ServoMotor> defConfig;

        //Stores the value of the ProgressBar
        private double value;
        private double step;
        private bool mouseDown;

        // Create a new instance of our ProgressBar Delegate that points
        // to the ProgressBar's SetValue method.
        private UpdateProgressBarDelegate updatePbDelegate;

        //Create a Delegate that matches the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, Object value);

        // Event registration
        public static readonly RoutedEvent SetMinButtonClickedEvent = EventManager.RegisterRoutedEvent("SetMinButtonClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ManualControl));
        public static readonly RoutedEvent SetMaxButtonClickedEvent = EventManager.RegisterRoutedEvent("SetMaxButtonClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ManualControl));
        public static readonly RoutedEvent TestValueButtonClickedEvent = EventManager.RegisterRoutedEvent("TestValueButtonClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ManualControl));
        public static readonly RoutedEvent DelButtonClickedEvent = EventManager.RegisterRoutedEvent("DelButtonClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ManualControl));
        public static readonly RoutedEvent AddButtonClickedEvent = EventManager.RegisterRoutedEvent("AddButtonClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ManualControl));

        // Custom events
        public event RoutedEventHandler SetMinButtonClicked
        {
            add { AddHandler(SetMinButtonClickedEvent, value); }
            remove { RemoveHandler(SetMinButtonClickedEvent, value); }
        }

        public event RoutedEventHandler SetMaxButtonClicked
        {
            add { AddHandler(SetMaxButtonClickedEvent, value); }
            remove { RemoveHandler(SetMaxButtonClickedEvent, value); }
        }

        public event RoutedEventHandler TestValueButtonClicked
        {
            add { AddHandler(TestValueButtonClickedEvent, value); }
            remove { RemoveHandler(TestValueButtonClickedEvent, value); }
        }
        public event RoutedEventHandler DelButtonClicked
        {
            add { AddHandler(DelButtonClickedEvent, value); }
            remove { RemoveHandler(DelButtonClickedEvent, value); }
        }

        public event RoutedEventHandler AddButtonClicked
        {
            add { AddHandler(AddButtonClickedEvent, value); }
            remove { RemoveHandler(AddButtonClickedEvent, value); }
        }


        public ManualControl(List<ServoMotor> defaulConfig, int index)
        {
            InitializeComponent();

            defConfig = defaulConfig;
            updatePbDelegate = new UpdateProgressBarDelegate(PBManualControl.SetValue);
            Uid = Convert.ToString(index);

            // Default position
            PositionBox.Text = "0.500";
            value = Convert.ToDouble(PositionBox.Text, NumberFormatInfo.InvariantInfo);

            //Configure the ProgressBar
            PBManualControl.Minimum = 0;
            PBManualControl.Maximum = 1;
            PBManualControl.Value = value;

            step = 0.001;

            EventManager.RegisterClassHandler(typeof(FACEGui20Win), FACEGui20Win.SetMinMaxButtonClickedEvent, new RoutedEventHandler(SetMaxButton_Click));

            DelButton.PreviewMouseDown += new MouseButtonEventHandler(DelButton_MouseDown);
            DelButton.PreviewMouseUp += new MouseButtonEventHandler(DelButton_MouseUp);

            AddButton.PreviewMouseDown += new MouseButtonEventHandler(AddButton_MouseDown);
            AddButton.PreviewMouseUp += new MouseButtonEventHandler(AddButton_MouseUp);
        }


        /// <summary>
        /// Update the textbox of the manual control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PositionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                value = Convert.ToSingle(PositionBox.Text, NumberFormatInfo.InvariantInfo);

                if (value >= PBManualControl.Minimum && value <= PBManualControl.Maximum)
                //if (value >= min && value <= max)
                {
                    /*Update the Value of the ProgressBar:
                      1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
                      2)  Set the DispatcherPriority to "Background"
                      3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
                    Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });
                }
                else if (value < PBManualControl.Minimum)
                {
                    value = PBManualControl.Minimum;
                    PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));
                }
                else
                {
                    value = PBManualControl.Maximum;
                    PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));
                }
            }
            catch
            {
                value = 0.000;
                PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, new object[] { 
                    ProgressBar.ValueProperty, value 
                });
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetMinButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ManualControl.SetMinButtonClickedEvent, this.Uid));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetMaxButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ManualControl.SetMaxButtonClickedEvent, this.Uid));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestValueButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
        }


        /// <summary>
        /// Manage the mouse down event pressing the Del button
        /// (Add button decreases the value in the textbox)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            try
            {
                //if (selectedEnum != null && servomotorsCombo.SelectedIndex != -1)
                //{
                while (mouseDown)
                {
                    //Tight Loop:  Loop until the ProgressBar.Value reaches the max
                    if (PBManualControl.Value > PBManualControl.Minimum)
                    {
                        value -= step;

                        /*Update the Value of the ProgressBar:
                          1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
                          2)  Set the DispatcherPriority to "Background"
                          3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
                        Dispatcher.Invoke(updatePbDelegate,
                            System.Windows.Threading.DispatcherPriority.Background,
                            new object[] { ProgressBar.ValueProperty, value });

                        PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));

                        //RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
                    }
                    else
                    {
                        mouseDown = false;
                    }
                }
            }
            //}
            catch
            {
                value = 0.000;
                Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });
                PositionBox.Text = "0.000";
            }
        }


        /// <summary>
        /// Manage the mouse up event pressing the Del button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
            mouseDown = false;
        }


        /// <summary>
        /// Manage the mouse down event pressing the Add button
        /// (Add button increments the value in the textbox)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            try
            {
                //if (selectedEnum != null && servomotorsCombo.SelectedIndex != -1)
                //{
                while (mouseDown)
                {
                    //Tight Loop:  Loop until the ProgressBar.Value reaches the max
                    if (PBManualControl.Value < PBManualControl.Maximum)
                    {
                        value += step;

                        /*Update the Value of the ProgressBar:
                          1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
                          2)  Set the DispatcherPriority to "Background"
                          3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
                        Dispatcher.Invoke(updatePbDelegate,
                            System.Windows.Threading.DispatcherPriority.Background,
                            new object[] { ProgressBar.ValueProperty, value });

                        PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));

                        //RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
                    }
                    else
                    {
                        mouseDown = false;
                    }
                }
                //}
            }
            catch
            {
                value = 0.000;
                Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });
                PositionBox.Text = "0.000";
            }
        }


        /// <summary>
        /// Manage the mouse up event pressing the Add button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
            mouseDown = false;
        }





        /// <summary>
        ///                 NOT USED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Tight Loop:  Loop until the ProgressBar.Value reaches the max
                if (PBManualControl.Value > PBManualControl.Minimum)
                {
                    value -= step;

                    /*Update the Value of the ProgressBar:
                      1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
                      2)  Set the DispatcherPriority to "Background"
                      3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
                    Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });

                    PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));

                    RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
                }
            }
            catch
            {
                value = 0.000;
                Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });
                PositionBox.Text = "0.000";
            }
        }


        /// <summary>
        ///                 NOT USED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Tight Loop:  Loop until the ProgressBar.Value reaches the max
                if (PBManualControl.Value < PBManualControl.Maximum)
                {
                    value += step;

                    /*Update the Value of the ProgressBar:
                      1)  Pass the "updatePbDelegate" delegate that points to the ProgressBar1.SetValue method
                      2)  Set the DispatcherPriority to "Background"
                      3)  Pass an Object() Array containing the property to update (ProgressBar.ValueProperty) and the new value */
                    Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });

                    PositionBox.Text = String.Format(value.ToString("0.000", CultureInfo.InvariantCulture));

                    RaiseEvent(new RoutedEventArgs(ManualControl.TestValueButtonClickedEvent, this.Uid));
                }
            }
            catch
            {
                value = 0.000;
                Dispatcher.Invoke(updatePbDelegate,
                        System.Windows.Threading.DispatcherPriority.Background,
                        new object[] { ProgressBar.ValueProperty, value });
                PositionBox.Text = "0.000";
            }
        }
    }
}