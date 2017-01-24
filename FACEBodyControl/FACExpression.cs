using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FACEBodyControl
{
    public class FACExpression
    {
        private ServoMotorGroup face;
        public ServoMotorGroup Face
        {
            get { return face; }
            set { face = value; }
        }

        private DateTime sentTime;
        public DateTime SentTime
        {
            get { return sentTime; }
            set { sentTime = value; }
        }

        private int priority;
        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        public FACExpression()
        {
            face = new ServoMotorGroup(32);  // FIX PARAMETER
            sentTime = DateTime.Now;
            priority = 10;
        }

        public FACExpression(ServoMotorGroup smg)
        {
            face = smg;
            sentTime = DateTime.Now;
            priority = 10;
        }

        public FACExpression(ServoMotorGroup smg, int pr)
        {
            face = smg;
            sentTime = DateTime.Now;
            priority = pr;
        }

        public FACExpression(ServoMotorGroup smg, DateTime t, int pr)
        {
            face = smg;
            sentTime = t;
            priority = pr;
        }

    }
}
