using VRage.Utils;

namespace ESThrustKiller
{
    public static class Log
    {
        public static bool DebugLog;
        public static void Msg(string msg)
        {
            MyLog.Default.WriteLine($"ESThrustKiller: {msg}");
        }

        public static void Debug(string msg)
        {
            if (DebugLog)
                Msg($"[DEBUG] {msg}");
        }
    }
}
