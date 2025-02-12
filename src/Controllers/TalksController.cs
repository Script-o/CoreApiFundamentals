using CoreCodeCamp.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace CoreCodeCamp.Controllers
{
    [ApiController]
    [Route("api/camps/{moniker}/talks")]
    public class TalksController: ControllerBase
    {
        private readonly ICampRepository _repository;
        private readonly LinkGenerator _linkGenerator;

        public TalksController (ICampRepository repository, LinkGenerator linkGenerator)
        {
            _repository = repository;
            _linkGenerator = linkGenerator;
        }

        [HttpGet]
        public async Task<ActionResult<Talk[]>> Get(string moniker)
        {
            try
            {
                var talks = await _repository.GetTalksByMonikerAsync(moniker, true);
                return talks;
            }
            catch (System.Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get Target");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Talk>> Get(string moniker, int id)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id);
                if (talk == null) return NotFound("");
                return talk;
            }
            catch (System.Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get Target");
            }
        }

        //This is currently broken because I cannot use Mapping DI. Instead of going
        //through an iterface like object to display information it's going into talks
        // which has camps which has talks etc etc...
        [HttpPost]
        public async Task<ActionResult<Talk>> Post(string moniker, Talk model)
        {
            try
            {
                var camp = await _repository.GetCampAsync(moniker);
                if (camp == null) return BadRequest("Camp does not exist");

                model.Camp = camp;

                if (model.Speaker == null) return BadRequest("Speaker ID is required");
                var speaker = await _repository.GetSpeakerAsync(model.Speaker.SpeakerId);
                if (speaker == null) return BadRequest("Speaker could not be found");
                model.Speaker = speaker;

                _repository.Add(model);

                if(await _repository.SaveChangesAsync())
                {
                    var url = _linkGenerator.GetPathByAction(HttpContext,
                        "Get",
                        values: new { moniker, id = model.TalkId });

                    return Created(url, model);
                }
                else
                {
                    return BadRequest("FGailed to save new Talk");
                }
            }
            catch (System.Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get Target");
            }
        }

        //Overall this is working simi correctly. For some reason it isn't adding the campId back
        //And that is messing with it. 
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Talk>> Put(string moniker, int id, Talk model)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id, true);
                if (talk == null) return NotFound("Couldn't find the talk");

                if (model.Speaker != null)
                {
                    var speaker = await _repository.GetSpeakerAsync(model.Speaker.SpeakerId);
                    if (speaker != null)
                    {
                        model.Speaker = speaker;
                    }
                }

                _repository.Delete(talk);
                _repository.Add(model);

                if (await _repository.SaveChangesAsync())
                {
                    return model;
                }
                else
                {
                    return BadRequest("Failed to update database");
                }
            }
            catch (System.Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get Target");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(string moniker, int id)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id);
                if (talk == null) return NotFound("Failed to find talk to delete");
                _repository.Delete(talk);

                if (await _repository.SaveChangesAsync())
                {
                    return Ok();
                }
                else
                {
                    return BadRequest("Failed to delete talk");
                }
            }
            catch (System.Exception)
            {

                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get Target");
            }
        }
    }
}