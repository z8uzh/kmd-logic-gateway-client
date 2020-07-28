using System.Diagnostics;
using System.Reflection;

namespace GatewayPublishing
{
    public class GatewayAutomationAppVersion
    {
        public static string Current =>
           FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
    }
}
