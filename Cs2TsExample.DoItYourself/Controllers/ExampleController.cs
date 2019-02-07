using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cs2TsExample.DoItYourself.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cs2TsExample.DoItYourself.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class ExampleController : ControllerBase
    {
        [HttpGet("[action]")]
        public async Task<ListExamplesResponse> ListExamples([FromQuery]ListExamplesRequest request)
        {
            await Task.Delay(3000);

            return new ListExamplesResponse
            {

            };
        }
    }
}
