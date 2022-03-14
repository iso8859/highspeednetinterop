using System.Runtime.InteropServices;

namespace aspnetcoreapi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct exchangeStruct
    {
        public IntPtr sendEvent;
        public IntPtr receiveEvent;

        public IntPtr sendData; // Caller is responsible to free sendData memory
        public Int32 sendDataLength;

        public IntPtr responseData; // The DLL is responsible to free responseData memory
        public Int32 responseDataLength;
    };

    static public class Interop
    {
        [DllImport("cpp_processing.dll")]
        public static extern IntPtr Init(IntPtr pData);
        [DllImport("cpp_processing.dll")]
        public static extern void Dispose(IntPtr context);
    }

    public class FastIPC : IDisposable
    {
        public object Context { get; set; }

        exchangeStruct exchange;
        ManualResetEvent send, receive;
        IntPtr pContext, pExchange;
        public void Init()
        {
            exchange = new exchangeStruct();
            send = new ManualResetEvent(false);
            receive = new ManualResetEvent(false);
            exchange.sendEvent = send.SafeWaitHandle.DangerousGetHandle();
            exchange.receiveEvent = receive.SafeWaitHandle.DangerousGetHandle();
            pExchange = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(exchangeStruct)));
            Marshal.StructureToPtr(exchange, pExchange, false);
            pContext = Interop.Init(pExchange);
        }

        public async Task DoProcessingAsync(HttpContext context)
        {
            // Copy request body
            MemoryStream ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms).ConfigureAwait(false);
            byte[] data = ms.GetBuffer();
            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            exchangeStruct exchange = Marshal.PtrToStructure<exchangeStruct>(pExchange);
            exchange.sendData = pinnedArray.AddrOfPinnedObject();
            exchange.sendDataLength = data.Length;
            Marshal.StructureToPtr(exchange, pExchange, false);
            // Start worker and wait for finish
            receive.Reset();
            send.Set();
            receive.WaitOne();
            // Free object
            pinnedArray.Free();
            ms.Dispose();
            // Copy reponse to body
            exchange = Marshal.PtrToStructure<exchangeStruct>(pExchange);
            data = new byte[exchange.responseDataLength];
            Marshal.Copy(exchange.responseData, data, 0, exchange.responseDataLength);
            await context.Response.Body.WriteAsync(data, 0, exchange.responseDataLength);
        }
        public async Task DoProcessingAsync(string test, HttpContext context)
        {
            // Copy string
            exchangeStruct exchange = Marshal.PtrToStructure<exchangeStruct>(pExchange);
            exchange.sendData = Marshal.StringToHGlobalAnsi(test);
            exchange.sendDataLength = test.Length;
            Marshal.StructureToPtr(exchange, pExchange, false);
            // Start worker and wait for finish
            receive.Reset();
            send.Set();
            receive.WaitOne();
            // Free object
            Marshal.FreeHGlobal(exchange.sendData);
            // Copy reponse to body
            exchange = Marshal.PtrToStructure<exchangeStruct>(pExchange);
            byte[] data = new byte[exchange.responseDataLength];
            Marshal.Copy(exchange.responseData, data, 0, exchange.responseDataLength);
            await context.Response.Body.WriteAsync(data, 0, exchange.responseDataLength);
        }

        public void Dispose()
        {
            Interop.Dispose(pContext);
            Marshal.FreeHGlobal(pExchange);
        }
    }

    public class FastIPCFactory
    {
        List<FastIPC> instances = new List<FastIPC>();
        SemaphoreSlim _slotFree;

        public FastIPCFactory(int instanceCount)
        {
            _slotFree = new SemaphoreSlim(instanceCount, instanceCount);
            for (int i=0; i< instanceCount; i++)
            {
                FastIPC fipc = new FastIPC() { Context = true }; // Context = available or nor
                fipc.Init();
                instances.Add(fipc);
            }
        }

        public async Task<FastIPC> GetInstanceAsync()
        {
            await _slotFree.WaitAsync();
            var result = instances.Find(_ => (bool)_.Context == true);
            result.Context = false;
            return result;
        }

        public void ReleaseInstance(FastIPC instance)
        {
            instance.Context = true;
            _slotFree.Release();
        }
    }
}
