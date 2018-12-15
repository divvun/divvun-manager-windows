using System.Windows.Controls;

namespace Pahkat.Service.CoreLib
{
    public enum InstallerTarget: byte
    {
        System,
        User
    }
    public static class InstallerTargetExtensions
    {
        public static byte ToByte(this InstallerTarget target)
        {
            switch (target)
            {
                case InstallerTarget.System:
                    return 0;
                case InstallerTarget.User:
                    return 1;
                default:
                    return 255;
            }
        }
    }
}
