using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paramore.Brighter;
using Paramore.Darker;
using TransmogrifierAPI.Application.Ports.Driving;

namespace TransmogrifierAPI.Adapters.Driving
{
    [ApiController]
    [Route("[controller]")]
    public class TransmogrifierController(IAmACommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        : Controller
    {
        [Route("{name}")]
        [HttpGet]
        public async Task<IActionResult> Get(string name)
        {
             var personsGreetings = await queryProcessor.ExecuteAsync(new FindTransmogrificationsForPerson(name));
 
             if (personsGreetings == null) return new NotFoundResult();
 
             return Ok(personsGreetings);
        }
        
        [Route("{name}/new")]
        [HttpPost]
        public async Task<ActionResult<FindPersonTransmogrifications>> Post(string name, NewTransmogrification transmogrification)
        {
            await commandProcessor.SendAsync(new MakeTransmogrification(name, transmogrification.Transmogrification));

            var personsGreetings = await queryProcessor.ExecuteAsync(new FindTransmogrificationsForPerson(name));

            if (personsGreetings == null) return new NotFoundResult();

            return Ok(personsGreetings);
        }
    }
}
