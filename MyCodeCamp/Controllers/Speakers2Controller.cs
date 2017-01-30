using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;

namespace MyCodeCamp.Controllers
{
    [Authorize]
    [Route("api/camps/{moniker}/speakers")]
    [ValidateModel]
    [ApiVersion("2.0")]
    public class Speakers2Controller : SpeakersController
    {
        
        public Speakers2Controller(ICampRepository repository,
            ILogger<SpeakersController> logger, IMapper mapper) : 
            base(repository, logger, mapper )
        {

        }

        [HttpGet]
        public override IActionResult GetWithCount(string moniker, bool includeTalks = false)
        {
            var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);

            return Ok(new
            {
                date = DateTime.UtcNow,
                count = speakers.Count(),
                result = _mapper.Map<IEnumerable<Speaker2Model>>(speakers)
            });
        }
    }
}
