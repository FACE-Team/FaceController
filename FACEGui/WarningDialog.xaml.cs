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

namespace FACEGui20
{
    /// <summary>
    /// Interaction logic for WarningDialog.xaml
    /// </summary>
    public partial class WarningDialog : Window
    {
        public WarningDialog()
        {
            InitializeComponent();
        }

        private void warningDialogOkButton_Click(object sender, RoutedEventArgs e)
        {
            //base.OnClosed(e);
            Close();
        }

    }
}
