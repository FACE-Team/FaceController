using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FACEBodyControl
{
    public enum StdType { /*MiniSSC = 0,*/ SSC32 };

    public enum MotorsNames
    {
        Jaw = 0, FrownLeft = 1, Empty = 2, EELeft = 3, SmileLeft = 4, LipUpperCenter = 5, BrowOuterLeft = 6, BrowInnerLeft = 7,
        SquintLeft = 8, SneerLeft = 9, LipUpperLeft = 10, EyeTurnLeft = 11, LipLowerLeft = 12, EyesUpDown = 13, UpperNod = 14,
        LowerNod = 15, LipLowerCenter = 16, SneerRight = 17, EERight = 18, FrownRight = 19, SmileRight = 20, EyeLidsLower = 21,
        EyeLidsUpper = 22, BrowOuterRight = 23, BrowInnerRight = 24, BrowCenter = 25, LipLowerRight = 26, LipUpperRight = 27,
        Turn = 28, SquintRight = 29, EyeTurnRight = 30, Tilt = 31
    };

    //public enum Expressions
    //{
    //    Neutral = 32, Anger, Disgust, Fear, Happiness, Sadness, Surprise
    //};

    //public enum BrainModules { FaceTrack = 0, Blink = 1 }


    public static class Enums
    {
        private static Dictionary<string, string> standardType;


        public static void InitializeStdMap()
        {
            standardType = new Dictionary<string, string>();

            Array names = Enum.GetNames(typeof(StdType));

            foreach (string enumVal in names)
                standardType.Add(enumVal, enumVal);
        }

        public static string[] getStdType()
        {
            InitializeStdMap();

            List<string> types = new List<string>();
            foreach (var pair in standardType)
            {
                types.Add(pair.Value);
            }

            return types.ToArray();
        }

    }
}