namespace CoronaDeployments.Core.Deploy
{
    public class IISDeployTargetExtraInfo : IDeployTargetExtraInfo
    {
        public string SiteName { get; set; }
        public int Port { get; set; }

        public IISDeployTargetExtraInfo()
        {
        }

        public IISDeployTargetExtraInfo(string siteName, int port)
        {
            SiteName = siteName;
            Port = port;
        }

        public static bool Validate(IISDeployTargetExtraInfo i)
        {
            if (i == default) return false;

            if (string.IsNullOrWhiteSpace(i.SiteName)) return false;

            if (i.Port <= 0) return false;

            return true;
        }
    }
}