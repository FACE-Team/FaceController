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

namespace StatusMonitor
{
    /// <summary>
    /// Logica di interazione per ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        int id;
        public delegate void CloseChartWindowEventHandler(int id);
        public event CloseChartWindowEventHandler CloseChartWindow;
        public ChartWindow(int idobject)
        {   
            InitializeComponent();
            id = idobject;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (CloseChartWindow != null)
                CloseChartWindow(id);
        }

    }

}
