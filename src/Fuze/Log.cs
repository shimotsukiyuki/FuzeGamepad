using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuze
{
    public static class Log
    {
        public enum LogType
        {
            Info = 0,
            warning,
            exception,
            error
        }
        public static void WriteLine(string msg)
        {
            WriteLine(msg, LogType.Info);
        }

        public static void WriteLine(string msg, LogType type)
        {
            string outputStr = "[" + DateTime.Now.ToString("HH:mm:ss") + "]";

            switch (type)
            {
                case LogType.error:
                    outputStr += "[ERROR] " + msg;
                    break;
                case LogType.exception:
                    outputStr += "[EXCE] " + msg;
                    break;
                case LogType.warning:
                    outputStr += "[WARN] " + msg;
                    break;
                case LogType.Info:
                default:
                    outputStr += "[INFO] " + msg;
                    break;
            }

            Console.WriteLine(outputStr);
        }
    }
}
