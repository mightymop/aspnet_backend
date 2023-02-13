using backend.Model;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspauthtest.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class BackendController : ControllerBase
    {
        private ILog log = LogManager.GetLogger(typeof(BackendController));
        ConfigurationManager _configuration;

        public BackendController(ConfigurationManager configuration)
        {
            log.Debug("Initialisiere Controller");
            this._configuration = configuration;
        }

        [HttpGet("/test/info")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ActionResult<ApiInfo> info()
        {
            ApiInfo apiInfo = new ApiInfo();
            apiInfo.Name = this._configuration["api:name"];
            apiInfo.Version = this._configuration["api:version"];
            apiInfo.Date = DateTime.Now;
            return Ok(apiInfo);
        }

    }
}