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
    /// Interaction logic for QuestionDialog.xaml
    /// </summary>
    public partial class QuestionDialog : Window
    {
        public delegate void DialogYesButtonEventHandler(object sender, EventArgs e);
        public event DialogYesButtonEventHandler YesButtonClicked;

        public delegate void DialogNoButtonEventHandler(object sender, EventArgs e);
        public event DialogNoButtonEventHandler NoButtonClicked;

        public delegate void DialogCancelButtonEventHandler(object sender, EventArgs e);
        public event DialogCancelButtonEventHandler CancelButtonClicked;

        public QuestionDialog()
        {
            InitializeComponent();
        }

        private void dialogCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogCancelButtonEventHandler cancelClicked = CancelButtonClicked;
            if (cancelClicked != null)
                cancelClicked(this, EventArgs.Empty);
            Close();
        }

        private void dialogNoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogNoButtonEventHandler noClicked = NoButtonClicked;
            if (noClicked != null) 
                noClicked(this, EventArgs.Empty);
            Close();
        }

        private void dialogYesButton_Click(object sender, RoutedEventArgs e)
        {
            //RaiseEvent(new RoutedEventArgs(QuestionDialog.DialogYesButtonClickedEvent));
            DialogYesButtonEventHandler yesClicked = YesButtonClicked;
            if (yesClicked != null) 
                yesClicked(this, EventArgs.Empty);
            DialogResult = true;
            Close();
        }
      
    }
}
