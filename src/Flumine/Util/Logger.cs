using System;
using System.Diagnostics;

namespace Flumine.Util
{    
    public static class Logger
    {               
        public static ILog GetLogger(Type type)
        {
            return new Log4NetWrapper(log4net.LogManager.GetLogger(type));
        }

        public static ILog GetLoggerForDeclaringType()
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            return GetLogger(type);
        }     
    }
}
