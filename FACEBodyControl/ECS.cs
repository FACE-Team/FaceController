using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace FACEBodyControl
{
    [Serializable()]
    public class ECS
    {        
        public struct ECSCoordinate
        {
            public float Pleasure;
            public float Arousal;
            public float Dominance;

            public ECSCoordinate(float p, float a, float d)
            {
                Pleasure = p;
                Arousal = a;
                Dominance = d;
            }
        }

        public List<ECSMotor> ECSMotorList;


        public ECS() 
        {
            ECSMotorList = new List<ECSMotor>();
        }


        public void SendECSCommand(float pleasure, float arousal, float dominance, int duration)
        {
            List<ServoMotor> smList = FACEBody.CurrentMotorState;
            for (int i = 0; i < smList.Count; i++)
            {
                smList[i].PulseWidthNormalized = ECSMotorList[i].GetMotorValue(pleasure, arousal);
            }

            FACEMotion motion = new FACEMotion(smList, TimeSpan.FromMilliseconds(duration), 10);
            motion.ECSCoord = new ECSCoordinate(pleasure, arousal, dominance);
            motion.Name = "p"+pleasure+"a"+arousal+"d"+dominance;
            FACEBody.ExecuteMotion(motion);
        }

        public static ECS LoadFromXmlFormat(string fileName)
        {
            XmlSerializer xmlFormat = new XmlSerializer(typeof(ECS), new Type[] { typeof(ECSMotor) });
            Stream filestream = null;
            ECS ecs = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Open);
                ecs = xmlFormat.Deserialize(filestream) as ECS;
                filestream.Close();
                return ecs;
            }
            catch
            {
                filestream.Close();
            }

            return ecs;
        }

        public static void SaveAsXmlFormat(object objGraph, string fileName)
        {
            XmlSerializer xmlFormat = new XmlSerializer(typeof(ECS), new Type[] { typeof(ECSMotor) });
            Stream fStream = null;
            StreamWriter xmlWriter = null;

            try
            {
                fStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                xmlWriter = new StreamWriter(fStream);
                xmlFormat.Serialize(xmlWriter, objGraph);
                xmlWriter.Close();
                fStream.Close();
            }
            catch
            {
                xmlWriter.Close();
                fStream.Close();
            }
        }
        
        public float GetECSValue(int idx, float pleasure, float arousal)
        {
            return ECSMotorList[idx].GetMotorValue(pleasure, arousal);
        }
    
    }
}
