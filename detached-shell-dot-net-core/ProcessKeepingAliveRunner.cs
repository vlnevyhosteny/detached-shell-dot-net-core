using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace detached_shell_dot_net_core
{
    public class ProcessOutput
    {
        public string[] Output { get; set; }
    }

    public class ProcessKeepingAliveRunnerException : Exception
    {
        public ProcessKeepingAliveRunnerException(string message)
            : base(message) { }
    }

    public class ProcessException : Exception
    {
        private string[] Output { get; }

        public ProcessException(string message, string[] output)
            : base(message)
        {
            this.Output = output;
        }
    }

    public class ProcessKeepingAliveRunner
    {
        private readonly ProcessStartInfo processStartInfo;

        private Process process;

        private SemaphoreSlim semaphore;

        private IList<string> processOutput;

        private readonly string outputEvent;

        public ProcessKeepingAliveRunner(ProcessStartInfo processStartInfo, string outputEvent)
        {
            this.processStartInfo = processStartInfo;
            this.outputEvent = outputEvent;

            this.processStartInfo.UseShellExecute = false;
            this.processStartInfo.RedirectStandardOutput = true;
            this.processStartInfo.RedirectStandardError = true;
        }

        public async Task<ProcessOutput> WaitForEventThanKeepAlive(int milisecondsTimeout)
        {
            this.Initialize();

            this.process = new Process
            {
                StartInfo = this.processStartInfo,
                EnableRaisingEvents = true,
            };

            this.process.OutputDataReceived += Process_OutputDataReceived;
            this.process.ErrorDataReceived += Process_ErrorDataReceived;
            this.process.Exited += Process_Exited;

            // Here you can specify uset etc
            bool started = this.process.Start();
            if (!started)
            {
                throw new ProcessKeepingAliveRunnerException("Could not start process");
            }

            this.process.BeginOutputReadLine();

            var processed = await this.semaphore.WaitAsync(milisecondsTimeout);
            if (!processed)
            {
                throw new ProcessKeepingAliveRunnerException("Process does not reach the event in timeout");
            }

            return new ProcessOutput
            {
                Output = this.processOutput.ToArray(),
            };
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            // Here you can check f.e. exit code of the process.
            throw new ProcessException("Process exited", this.processOutput.ToArray());
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            this.processOutput.Add(e.Data);

            throw new ProcessException(e.Data, this.processOutput.ToArray());
        }

        public void KillAndDisposeProcess()
        {
            if (this.process == null)
            {
                throw new ProcessKeepingAliveRunnerException("No processing running");
            }

            this.process.Kill();
            this.process.Dispose();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            this.processOutput.Add(e.Data);

            if (e.Data == this.outputEvent)
            {
                this.semaphore.Release();
                this.process.CancelOutputRead();
            }
        }

        private void Initialize()
        {
            this.semaphore = new SemaphoreSlim(0, 1);

            this.processOutput = new List<string>();
        }
    }
}
