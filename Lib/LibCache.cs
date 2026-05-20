using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{

    public static class LibCache
    {
        public static void LiberarMemoria()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { };
        }
    }
}