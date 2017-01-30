using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;

namespace MyCodeCamp.Controllers
{
    [Authorize]
    [Route("api/camps/{moniker}/speakers/{speakerId}/talks")]
    [ValidateModel]
    public class TalksController : BaseController
    {
        private ILogger<TalksController> _logger;
        private IMapper _mapper;
        private readonly IMemoryCache _cache;
        private ICampRepository _repo;

        public TalksController(ICampRepository repo, ILogger<TalksController> logger, IMapper mapper, IMemoryCache cache)
        {
            _repo = repo;
            _logger = logger;
            _mapper = mapper;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Get(string moniker, int speakerId)
        {
            var talks = _repo.GetTalks(speakerId);

            if (talks.Any(t => t.Speaker.Camp.Moniker != moniker)) return BadRequest("Invalid talks for the speaker selected");

            return Ok(_mapper.Map<IEnumerable<TalkModel>>(talks));
        }

        [HttpGet("{id}", Name = "GetTalk")]
        public IActionResult Get(string moniker, int speakerId, int id)
        {
            if (Request.Headers.ContainsKey("If-None-Match"))
            {
                var oldEtag = Request.Headers["If-None-Match"].First();
                if (_cache.Get($"talk-{id}-{oldEtag}") != null)
                {
                    return StatusCode((int)HttpStatusCode.NotModified);
                }
            }
            var talk = _repo.GetTalk(id);

            if (talk.Speaker.Id != speakerId || talk.Speaker.Camp.Moniker != moniker) return BadRequest("Invalid talk for the speaker selected");

            AddEtag(talk);
   
            return Ok(_mapper.Map<TalkModel>(talk));
        }

        [HttpPost()]
        public async Task<IActionResult> Post(string moniker, int speakerId, [FromBody] TalkModel model)
        {
            try
            {
            var speaker = _repo.GetSpeaker(speakerId);
            if (speaker != null)
            {
                var talk = _mapper.Map<Talk>(model);

                talk.Speaker = speaker;
                _repo.Add(talk);

                if (await _repo.SaveAllAsync())
                {
                    AddEtag(talk);
                    return Created(Url.Link("GetTalk", new { moniker = moniker, speakerId = speakerId, id = talk.Id }), _mapper.Map<TalkModel>(talk));
                }
            }

            }
            catch (Exception ex)
            {

            _logger.LogError($"Failed to save new talk: {ex}");
            }

            return BadRequest("Failed to save new talk");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string moniker, int speakerId, int id, [FromBody] TalkModel model)
        {
            try
            {
                var talk = _repo.GetTalk(id);
                if (talk == null) return NotFound();
                if (Request.Headers.ContainsKey("If-Match"))
                {
                    var etag = Request.Headers["If-Match"].First();
                    if (etag != Convert.ToBase64String(talk.RowVersion))
                    {
                        return StatusCode((int)HttpStatusCode.PreconditionFailed);
                    }
                }
                _mapper.Map(model, talk);
                if (await _repo.SaveAllAsync())
                {
                    AddEtag(talk);
                    return Ok(_mapper.Map<TalkModel>(talk));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update talk: {ex}");
            }
            return BadRequest("Failed to update talk");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string moniker, int speakerId, int id)
        {
            try
            {
            var talk = _repo.GetTalk(id);
            if (talk == null) return NotFound();

            _repo.Delete(talk);

            if (await _repo.SaveAllAsync())
            {
                return Ok();
            }

            }
            catch (Exception ex)
            {

            _logger.LogError($"Failed to delete talk: {ex}");
            }

            return BadRequest("Failed to delete talk");
        }


        private void AddEtag(Talk talk)
        {
            var etag = Convert.ToBase64String(talk.RowVersion);
            Response.Headers.Add("ETag", etag);
            _cache.Set($"talk-{talk.Id}-{etag}", talk);
        }
    }
}
