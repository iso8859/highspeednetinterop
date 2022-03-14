using System.Runtime.InteropServices;

namespace aspnetcoreapi
{
    static public class InteropSlow
    {
        [DllImport("cpp_processing.dll")]
        public static extern void SlowMarshalingProcess(IntPtr pData);
        [DllImport("cpp_processing.dll")]
        public static extern void SlowMarshalingFree(IntPtr context);
    }

    public class SlowIPC : IDisposable
    {
        exchangeStruct exchange;
        IntPtr pExchange;

        public SlowIPC()
        {
            exchange = new exchangeStruct();
            pExchange = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(exchangeStruct)));
            Marshal.StructureToPtr(exchange, pExchange, false);
        }

        public void Dispose()
        {
            InteropSlow.SlowMarshalingFree(pExchange);
            Marshal.FreeHGlobal(pExchange);
        }

        public async Task SlowMarshalingProcessAsync(string test, HttpContext context)
        {
            // Copy string
            exchangeStruct exchange = Marshal.PtrToStructure<exchangeStruct>(pExchange);
            exchange.sendData = Marshal.StringToHGlobalAnsi(test);
            exchange.sendDataLength = test.Length;
            Marshal.StructureToPtr(exchange, pExchange, false);
            // Start worker and wait for finish
            InteropSlow.SlowMarshalingProcess(pExchange);
            // Free object
            Marshal.FreeHGlobal(exchange.sendData);
            // Copy reponse to body
            exchange = Marshal.PtrToStructure<exchangeStruct>(pExchange);
            byte[] data = new byte[exchange.responseDataLength];
            Marshal.Copy(exchange.responseData, data, 0, exchange.responseDataLength);
            await context.Response.Body.WriteAsync(data, 0, exchange.responseDataLength);

        }
    }
}
