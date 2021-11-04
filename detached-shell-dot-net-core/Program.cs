using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace detached_shell_dot_net_core
{
    class Program
    {
        private static readonly string successfullPoint = "successfull";

        private static bool processReachSuccessfullPoint = false;

        static async Task Main(string[] args)
        {
            bool runPowershell = false;

            string programPath = runPowershell ?
                "powershell.exe" :
                "sh";

            string scriptPath = runPowershell ?
                "./dummy-script.ps1" :
                "-c ./dummy-script.sh";

            ProcessStartInfo processInfo = new ProcessStartInfo(programPath, scriptPath);

            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            using (var process = new Process
            {
                StartInfo = processInfo,
                EnableRaisingEvents = true
            })
            {
                await RunProcessAsync(process).ConfigureAwait(false);
            }

            Console.WriteLine("End");
        }

        private static Task<int> RunProcessAsync(Process process)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != "")
            {
                Console.WriteLine("ERR: " + e.Data);
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);

            if (e.Data == successfullPoint)
            {
                processReachSuccessfullPoint = true;
            }
        }
    }
}
