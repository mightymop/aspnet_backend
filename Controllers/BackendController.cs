using backend.Model;
using fahrtenbuch_service.Services;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace aspauthtest.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class BackendController : ControllerBase
    {
        private ILog log = LogManager.GetLogger(typeof(BackendController));

        protected ConfigService _config;

        protected DatabaseService _db;

        public BackendController( ConfigService configService, DatabaseService db)
        {
            log.Debug("Initialisiere Controller");

            this._config = configService;

            this._db = db;
        }


        [HttpGet("/test/info")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableCors]
        public ActionResult<ApiInfo> info()
        {
            ApiInfo apiInfo = new ApiInfo();
            apiInfo.Name = this._config.get("api:name");
            apiInfo.Version = this._config.get("api:version");
            apiInfo.Date = DateTime.Now;
            return Ok(apiInfo);
        }

        [HttpGet("/test/get")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        [EnableCors]
        public IActionResult get(string id)
        {
            try
            {
                string error;
                log.Debug("get: " + id);
                object data = _db.getData(id, out error);

                if (data!=null && error ==null)
                {
                    return Ok(data);
                }
                else
                {
                    if (error==null)
                    {
                        return NotFound(id + " nicht gefunden!");
                    }
                    else
                    {
                        return Problem(
                            detail: error,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }                    
                }
            }
            catch (HttpRequestException ex)
            {
                log.Error(ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpGet("/test/list")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        [EnableCors]
        public ActionResult<ApiInfo> list()
        {
            try
            {
                string error;
              
                object[] data = _db.getAllData( out error);

                if (data != null && error == null)
                {
                    return Ok(data);
                }
                else
                {
                    if (error == null)
                    {
                        return NotFound("Nix nicht gefunden!");
                    }
                    else
                    {
                        return Problem(
                            detail: error,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                log.Error(ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPost("/test/insert")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableCors]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public IActionResult insert([FromBody] JsonElement jdata)
        {
            try
            {
                // JSON in einen String serialisieren
                string requestString = jdata.GetRawText();

                string error;
                string result = null;

                log.Debug(requestString);

                JObject jrequest = JObject.Parse(requestString);
               
                string data = (string)jrequest["data"];

                string idResult;
                if ((idResult=_db.insertOrUpdateData(null, data, out error))!=null)
                {
                    return Ok(idResult);
                }
                else
                {
                    return Problem(
                        detail: error,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                log.Error(ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPatch("/test/update")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableCors]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public IActionResult update(TestModel req)
        {
            try
            {
                string error;

                string idResult;
                if ((idResult = _db.insertOrUpdateData(req.id, req.data, out error)) != null)
                {
                    return Ok(idResult);
                }               
                else
                {
                    return Problem(
                        detail: error,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                log.Error(ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpDelete("/test/delete")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableCors]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult delete(string id)
        {
            try
            {
                string error;
                if (_db.deleteData(id, out error))
                {
                    return Ok("Daten gel�scht");
                }
                else
                {
                    return Problem(
                        detail: error,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                log.Error(ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

    }
}