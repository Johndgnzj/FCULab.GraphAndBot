using log4net;
using System;

namespace Wistron.Bot.Sample.Helpers
{
    public class logHelper
    {
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static bool Log2File(String Ip, String ClassName, String MethodName, String content, String logType = "INFO")
        {
            bool result = false;
            try
            {
                String log = String.Format("{0,15} {1,20} {2,20} {3}", Ip, ClassName, MethodName, content);
                switch (logType)
                {
                    case "ERROR":
                        logger.Error(log);
                        break;
                    case "DEBUG":
                        logger.Debug(log);
                        break;
                    default:
                        logger.Info(log);
                        break;
                }
                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return result;
        }
    }

}