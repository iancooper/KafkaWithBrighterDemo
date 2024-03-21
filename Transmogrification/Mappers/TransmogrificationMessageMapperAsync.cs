using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter;

namespace Transmogrification.Mappers
{
    public class TransmogrificationMessageMapperAsync : IAmAMessageMapperAsync<Application.Ports.Driving.TransmogrificationMade>
    {
        public Task<Message> MapToMessageAsync(Application.Ports.Driving.TransmogrificationMade request, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Application.Ports.Driving.TransmogrificationMade> MapToRequestAsync(Message message, CancellationToken cancellationToken = default)
        {
            //NOTE: We are showing an async pipeline here, but it is often overkill by comparison to using 
            //TaskCompletionSource for a Task over sync instead
            using var ms = new MemoryStream(message.Body.Bytes); 
            return await JsonSerializer.DeserializeAsync<Application.Ports.Driving.TransmogrificationMade>(ms, JsonSerialisationOptions.Options, cancellationToken);
        }
    }
}
