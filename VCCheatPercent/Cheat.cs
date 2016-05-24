using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VCCheatPercent
{
    public class Cheat
    {
        public string name { get; set; }
        public bool delayCheat { get; set; } // if cheat can only be entered every (5?) ... minutes

        public Cheat(string name, bool delayCheat)
        {
            this.name = name;
            this.delayCheat = delayCheat;
        }
    }
}
