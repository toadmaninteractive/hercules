using System;
using System.Buffers;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules
{
    public class NamedPipeServer : IDisposable
    {
        private readonly CancellationTokenSource cts;
        private readonly Action<string> callback;
        private const int MaxInstances = 16;

        public NamedPipeServer(string pipeName, Action<string> callback)
        {
            cts = new CancellationTokenSource();
            StartAsync(pipeName, cts.Token).Track();
            StartAsync(pipeName, cts.Token).Track();
            this.callback = callback;
        }

        public void Dispose()
        {
            cts.Cancel();
        }

        private async Task StartAsync(string pipeName, CancellationToken ct)
        {
            using var memory = MemoryPool<byte>.Shared.Rent(4096);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, MaxInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        await pipeServer.WaitForConnectionAsync(ct);
                        var bytesRead = await pipeServer.ReadAsync(memory.Memory, ct);
                        if (bytesRead > 0)
                        {
                            var message = Encoding.UTF8.GetString(memory.Memory.Span[..bytesRead]);
                            callback(message);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (System.IO.IOException iox)
                {
                    Logger.LogException("IPC pipe failure", iox);
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return;
                }
            }
        }

        public static void SendMessage(string pipeName, string message)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            pipe.Connect();
            var bytes = Encoding.UTF8.GetBytes(message);
            pipe.Write(bytes);
            pipe.Flush();
        }
    }
}
