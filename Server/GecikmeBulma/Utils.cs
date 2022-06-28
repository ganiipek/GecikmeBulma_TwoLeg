using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GecikmeBulma.Trade;

namespace GecikmeBulma
{
    internal static class Utils
    {
        static List<LoggerService.ILoggerService> loggerServices = new List<LoggerService.ILoggerService>()
        {
            new LoggerService.ConsoleLoggerService()
        };

        //public static bool DatabaseIsConnected()
        //{
        //    return BreakoutManager.databaseManager.IsConnected();
        //}

        public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public static void SendLog(LoggerService.LoggerType type, string message)
        {
            //foreach (LoggerService.ILoggerService loggerService in loggerServices)
            //{
            //    loggerService.Send(type, message);
            //}

            Console.WriteLine(message);
        }
    }
}
