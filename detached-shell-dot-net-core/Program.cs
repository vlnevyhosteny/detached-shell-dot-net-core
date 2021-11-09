using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace detached_shell_dot_net_core
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string programPath = "sh";

            string scriptPath = "-c ./successfull-script.sh";

            ProcessStartInfo processInfo = new ProcessStartInfo(programPath, scriptPath);

            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            var runner = new ProcessKeepingAliveRunner(processInfo, "Successfull");

            var result = await runner.WaitForEventThanKeepAlive(50000);

            Console.Write(String.Join('\n', result.Output));

            runner.KillAndDisposeProcess();
        }
    }
}
