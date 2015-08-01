/*
 * Copyright (c) 2015 Tomonori Tamagawa

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cactbot
{

    public enum ObjectType : byte
    {
        Unknown    = 0x00,
        PC         = 0x01,
        Monster    = 0x02,
        NPC        = 0x03,
        Aetheryte  = 0x05,
        Gathering  = 0x06,
        Minion     = 0x09
    }

    public class CombatantEP
    {
        public uint ID;
        public uint OwnerID;
        public int Order;
        public ObjectType type;
        public uint TargetID;

        public byte Job;
        public byte Level;
        public string Name;

        public int CurrentHP;
        public int MaxHP;
        public int CurrentMP;
        public int MaxMP;
        public short MaxTP;
        public short CurrentTP;

        public Single PosX;
        public Single PosY;
        public Single PosZ;
        public byte EffectiveDistance;
        public string Distance;
        public string HorizontalDistance;

        public string HPPercent
        {
            get
            {
                try
                {
                    if (MaxHP == 0) return "0.00";
                    return (Convert.ToDouble(CurrentHP) / Convert.ToDouble(MaxHP) * 100).ToString("0.00");
                }
                catch
                {
                    return "0.00";
                }
            }
        }

        public float GetDistanceTo(Combatant target)
        {
            var distanceX = (float)Math.Abs(PosX - target.PosX);
            var distanceY = (float)Math.Abs(PosY - target.PosY);
            var distanceZ = (float)Math.Abs(PosZ - target.PosZ);
            return (float)Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY) + (distanceZ * distanceZ));
        }

        public float GetHorizontalDistanceTo(Combatant target)
        {
            var distanceX = (float)Math.Abs(PosX - target.PosX);
            var distanceY = (float)Math.Abs(PosY - target.PosY);
            return (float)Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
        }
    }

    ///
    /// Job enum
    ///
    public enum JobEnum : byte
    {
        UNKNOWN,
        GLD, // 1
        PGL, // 2
        MRD, // 3
        LNC, // 4
        ARC, // 5
        CNJ, // 6
        THM, // 7
        CRP, // 8
        BSM, // 9
        ARM, // 10
        GSM, // 11
        LTW, // 12
        WVR, // 13
        ALC, // 14
        CUL, // 15
        MIN, // 15
        BTN, // 17
        FSH, // 18
        PLD, // 19
        MNK, // 20
        WAR, // 21
        DRG, // 22
        BRD, // 23
        WHM, // 24
        BLM, // 25
        ACN, // 26
        SMN, // 27
        SCH, // 28
        ROG, // 29
        NIN, // 30
        MCH, // 31
        DRK, // 32
        AST  // 33
    }

    //// 敵視されてるキャラエントリ
    public class EnmityEntry
    {
        public uint ID { get; set; }
        public uint OwnerID { get; set; }
        public string Name { get; set; }
        public uint Enmity { get; set; }
        public bool isMe { get; set; }
        public int HateRate { get; set; }
        public byte Job { get; set; }
        public string JobName
        {
            get
            {
                return Enum.GetName(typeof(JobEnum), Job);
            }
        }
        public string EnmityString
        {
            get
            {
                return Enmity.ToString("##,#");
            }
        }
        public bool isPet
        {
            get
            {
                return (OwnerID != 0);
            }
        }
    }
  
    //// 敵視リストエントリ
    public class AggroEntry
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public int HateRate { get; set; }
        public int Order { get; set; }
        public bool isCurrentTarget { get; set; }

        public int CurrentHP;
        public int MaxHP;
        public string HPPercent
        {
            get
            {
                try
                {
                    if (MaxHP == 0) return "0.00";
                    return (Convert.ToDouble(CurrentHP) / Convert.ToDouble(MaxHP) * 100).ToString("0.00");
                }
                catch
                {
                    return "0.00";
                }
            }
        }

        // Target of Enemy
        public EnmityEntry Target;
    }
}
