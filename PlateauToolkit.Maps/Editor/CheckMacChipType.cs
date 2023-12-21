using UnityEngine;
using System.Diagnostics;
using System;

namespace PlateauToolkit.Maps.Editor
{
    public static class CheckMacChipType
    {
        public static string GetMacChipType()
        {
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                return GetMacArchitecture();
            }
            else
            {
                return "";
            }
        }

        static string GetMacArchitecture()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "/usr/sbin/sysctl";
            startInfo.Arguments = "-n machdep.cpu.brand_string";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            string result = "";
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    result = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Error fetching Mac architecture: " + ex.Message);
            }

            if (result.Contains("Apple"))
            {
                return "M1"; // or other Apple silicon chips
            }
            else if (result.Contains("Intel"))
            {
                return "Intel";
            }
            else
            {
                return "Unknown";
            }
        }
    }
}