using Microsoft.AspNetCore.Mvc;

namespace aspnetcoreapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        FastIPCFactory _fipcs;

        public ImageController(ILogger<ImageController> logger, FastIPCFactory fipcs)
        {
            _logger = logger;
            _fipcs = fipcs;
        }

        [HttpGet("int1/{test}")]
        public async Task GetInt1([FromRoute] string test)
        {
            var fipc = await _fipcs.GetInstanceAsync();
            await fipc.DoProcessingAsync(test, HttpContext);
            _fipcs.ReleaseInstance(fipc);
        }

        [HttpGet("int2/{test}")]
        public async Task GetInt2([FromRoute] string test)
        {
            var sc = new SlowIPC();
            await sc.SlowMarshalingProcessAsync(test, HttpContext); // Not that slow because it's faster than FastIPC :-)
            sc.Dispose();
        }

        [HttpGet("managed/{test}")]
        public async Task GetManaged([FromRoute] string test)
        {
            await Response.Body.WriteAsync(System.Text.Encoding.ASCII.GetBytes(test));
        }
    }
}