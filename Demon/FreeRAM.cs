using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demon
{
    class FreeRAM
    {
        public void Free()
        {
            GC.Collect();
            GC.GetTotalMemory(true);
        }
    }
}
