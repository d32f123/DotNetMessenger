using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

namespace DotNetMessenger.Logger
{
    public static class NLogger
    {
        public static NLog.Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    }
}
