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

using ControllersLibrary;

namespace FACEGui20
{
    /// <summary>
    /// Interaction logic for ECSWin.xaml
    /// </summary>
    public partial class ECSWin : Window
    {
        public ECSController ecscontroller
        {
            get { return ecsController; }
        }
        
        public ECSWin()
        {
            InitializeComponent();
        }

    }
}
