using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Realivation_Companion
{
    internal static class RVersioning
    {
        public static int GetProtocolNum()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version
                ?? throw new Exception("Couldn't get my own version number");
            return v.Major;
        }
        public static int GetVersionNum()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version
                ?? throw new Exception("Couldn't get my own version number");
            return v.Minor;
        }
        public static string GetVersionStr()
        {
            return $"{GetProtocolNum()}.{GetVersionNum()}";
        }
    }
}
