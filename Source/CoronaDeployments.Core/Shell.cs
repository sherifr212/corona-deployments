using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public static class Shell
    {
        public static async Task<string> Execute(string cmd)
        {
            return await Task.Run(() =>
            {
                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = $"/C {cmd}",
                        RedirectStandardOutput = true
                    };

                    process.Start();

                    var output = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();

                    return output;
                }
            });
        }
    }
}
