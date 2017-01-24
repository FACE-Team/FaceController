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
    /// Interaction logic for GamepadWin.xaml
    /// </summary>
    public partial class GamepadWin : Window
    {
        private GamepadController gamepad0 = null;
        public GamepadController Gamepad0
        {
            get { return gamepad0; }
        }

        private GamepadController gamepad1 = null;
        public GamepadController Gamepad1
        {
            get { return gamepad1; }
        }


        private string[] gamepad0Labels = null;
        private string[] gamepad1Labels = null;

        public GamepadWin()
        {
            InitializeComponent();

            gamepad0 = new GamepadController(0);
            gamepad0.JoyID = "Controller " + gamepad0.Id;
            gamepad0Labels = new string[] { "Arousal+", "Pleasure+", "Arousal-", "Pleasure-", "", "", "", "", "Blink", "FaceTrack", 
                "FaceTrackFreq-", "BlinkFreq-", "FaceTrackFreq+", "BlinkFreq+" };
            gamepad0.IsEnabled = false;

            Grid.SetColumn(gamepad0, 1);
            Grid.SetRow(gamepad0, 1);
            MainGrid.Children.Add(gamepad0);

            gamepad1 = new GamepadController(1);
            gamepad1.JoyID = "Controller " + gamepad1.Id;
            gamepad1Labels = new string[] { "", "", "", "", "", "", "", "", "", "", "TurnLeft", "Up", "TurnRight", "Down" };
            gamepad1.IsEnabled = false;

            Grid.SetColumn(gamepad1, 3);
            Grid.SetRow(gamepad1, 1);
            MainGrid.Children.Add(gamepad1);

            LoadButtConfig();
        }


        #region Init

        private void LoadButtConfig()
        {
            foreach (GamepadController.ButtonType t in Enum.GetValues(typeof(GamepadController.ButtonType) ))
            {
                gamepad0.SetButtonLabel(t, gamepad0Labels[(int)t-1]);
                gamepad1.SetButtonLabel(t, gamepad1Labels[(int)t-1]);
            }
        }

        #endregion


        #region Gamepad 0

        private void Gamepad0ActiveChkboxChecked(object sender, RoutedEventArgs e)
        {
            if (gamepad0 != null)
            {
                gamepad0.IsActive = true;
                gamepad0.IsEnabled = true;
            }
        }

        private void Gamepad0ActiveChkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gamepad0 != null)
            {
                gamepad0.IsActive = false;
                gamepad0.IsEnabled = false;
            }
        }

        #endregion 


        #region Gamepad 1

        private void Gamepad1ActiveChkboxChecked(object sender, RoutedEventArgs e)
        {
            if (gamepad1 != null)
            {
                gamepad1.IsActive = true;
                gamepad1.IsEnabled = true;
            }
        }
                
        private void Gamepad1ActiveChkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gamepad1 != null)
            {
                gamepad1.IsActive = false;
                gamepad1.IsEnabled = false;
            }
        }

        #endregion

    }
}
