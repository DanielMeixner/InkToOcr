using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dmx.Win.MPC.InkToOcr
{
    public class TextFoundEventArgs : EventArgs
    {
        public TextFoundEventArgs(string textFound)
        {
            this.TextFound = textFound;
        }

        public string TextFound { get; private set; }
    }
}
