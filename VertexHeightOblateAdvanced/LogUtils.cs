using System;
using UnityEngine;

namespace VertexHeightOblateAdvanced
{
    internal static class LogUtils
    {
        internal static void LogInfo(string message) => Debug.Log("[VHOA][INFO] " + message);
        internal static void LogAPI(string message) => Debug.Log("[VHOA][API] " + message);
        internal static void LogWarning(string message) => Debug.LogWarning("[VHOA][WARNING] " + message);
        internal static void LogError(string message) => Debug.LogError("[VHOA][ERROR] " + message);
    }
}
