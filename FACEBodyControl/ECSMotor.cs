using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace FACEBodyControl
{
    [Serializable()]
    public class ECSMotor
    {
        private int channel;
        public int Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        private int au;
        public int AU
        {
            get { return au; }
            set { au = value; }
        }

        private int sizeX;
        public int SizeX
        {
            get { return sizeX; }
            set { sizeX = value; }
        }

        private int sizeY;
        public int SizeY
        {
            get { return sizeY; }
            set { sizeY = value; }
        }

        private string ECSvalues;
        public string ECSValues
        {
            get { return ECSvalues; }
            set { ECSvalues = value; }
        }

        [XmlIgnoreAttribute()]
        private float[,] ECSmap;
        public float[,] ECSMap
        {
            get { return ECSmap; }
        }

        public ECSMotor() { }
                

        public void FillMap()
        {
            ECSmap = new float[sizeX, sizeY];
            string[] strings = ECSValues.Split(' ');

            int row = 0, column = 0;

            for(int i=0; i < strings.Length; i++)
            {
                row = (int)(i / sizeY);
                column = (int)(i % sizeX);
                ECSMap.SetValue(Single.Parse(strings[i], System.Globalization.CultureInfo.InvariantCulture.NumberFormat), row, column);
            }
        }


        internal float GetMotorValue(float pleasure, float arousal)
        {
            float xP = ((float)pleasure + 1) / (2f / (float)(sizeX - 1));
            //int xPleasure = (int)(xP + 1);
            int xPleasure = (int)(xP);

            float yA = ((float)(arousal + 1)) / (2f / (float)(sizeY - 1));
            //int yArousal = (int) (yA + 1);
            int yArousal = (int)(yA);

            return ECSmap[xPleasure, yArousal];
        }

    }
}
