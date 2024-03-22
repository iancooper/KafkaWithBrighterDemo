using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paramore.Brighter;
using Paramore.Darker;
using TransmogrifierAPI.Application.Ports.Driving;

namespace TransmogrifierAPI.Adapters.Driving
{
    [ApiController]
    [Route("[controller]")]
    public class PeopleController(IAmACommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        : Controller
    {
        [Route("{name}")]
         [HttpGet]
         public async Task<ActionResult<FindPersonResult>> Get(string name)
        {
            var foundPerson = await queryProcessor.ExecuteAsync<FindPersonResult>(new FindPersonByName(name));

            if (foundPerson.Person == null) return new NotFoundResult();

            return Ok(foundPerson);
        }

        [Route("{name}")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string name)
        {
            await commandProcessor.SendAsync(new DeletePerson(name));

            return Ok();
        }

        [Route("new")]
        [HttpPost]
        public async Task<ActionResult<FindPersonResult>> Post(NewPerson newPerson)
        {
            await commandProcessor.SendAsync(new AddPerson(newPerson.Name));

            var addedPerson = await queryProcessor.ExecuteAsync<FindPersonResult>(new FindPersonByName(newPerson.Name));

            if (addedPerson == null) return new NotFoundResult(); 

            return Ok(addedPerson);
        }
        
    }
}
