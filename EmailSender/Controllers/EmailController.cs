using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Net.Mime;
using System.Threading.Tasks;

using EmailUtility;

namespace EmailBackend.Controllers
{
  [ApiController]
  [Route("api/email")]
  public class EmailController : ControllerBase
  {
    [HttpGet]
    public string Get()
    {
      return "Running: " + DateTime.Now.ToString();
    }

    [HttpPost]
    [Route("send")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmailMessage))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] EmailMessage msg)
    {
      EmailUtil emailUtil = new EmailUtil();

      // Could be expanded to filter out all address/message formatting errors
      if (!msg.address.Contains('@') || msg.message == "")
      {
        return BadRequest();
      }
      
      try
      {
        await emailUtil.SendEmailAsync(msg);
        return Ok(msg);
      }
      catch
      {
        return StatusCode(StatusCodes.Status500InternalServerError);
      }
    }
  }
}
