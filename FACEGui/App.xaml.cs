using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Windows;


namespace FACEGui20
{
   
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // give the mutex a  unique name
        private const string MutexName = "FACEGui20Win";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        bool createdNew;
        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                // if the mutex already exists, notify and quit
                MessageBox.Show("This program is already running","",MessageBoxButton.OK,MessageBoxImage.Information);
                Application.Current.Shutdown(0);
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!createdNew) return;
            // overload the OnStartup so that the main window 
            // is constructed and visible
            FACEGui20Win mw = new FACEGui20Win();
            mw.Show();
        }
    }

   

  



}
