using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WootingAnalogVS
{
    internal class Config
    {
        //auto transition to sprinting when key is more than half pressed
        public bool autosprint = true;
        //when autosprint is on you will not run when pressing sprint
        public bool ReverseSprint = true;
    }
}
