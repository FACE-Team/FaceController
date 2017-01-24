using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShowLib;

namespace FACEBodyControl
{
    public class Webcams
    {
        private static int webcamId;
        public static int WebcamId
        {
            get { return webcamId; }
            set { webcamId = value; }
        }

        public static string[] getWebcamDevice()
        {
            // Get the collection of video devices
            DsDevice[] capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
          
            string[] webcamNames = new string[capDevices.Length];
            for (int i = 0; i < capDevices.Length; i++)
            {
                webcamNames[i] = capDevices[i].Name;
            }

            /*
            List<string> webcams = new List<string>(capDevices.Length);
            foreach (DsDevice ds in capDevices)
            {
                webcams.Add(ds.Name);
            }
            webcamNames.ToArray();
            */

            return webcamNames;
        }

    }
}