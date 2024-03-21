using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter;
using Transmogrification.Application.Ports.Driving;

namespace Transmogrification.Mappers
{
    public class TransmogrificationMessageMapperAsync : IAmAMessageMapperAsync<TransmogrificationMade>
    {
        public Task<Message> MapToMessageAsync(TransmogrificationMade request, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task<TransmogrificationMade> MapToRequestAsync(Message message, CancellationToken cancellationToken = default)
        {
            //NOTE: We are showing an async pipeline here, but it is often overkill by comparison to using 
            //TaskCompletionSource for a Task over sync instead
            using var ms = new MemoryStream(message.Body.Bytes); 
            return await JsonSerializer.DeserializeAsync<TransmogrificationMade>(ms, JsonSerialisationOptions.Options, cancellationToken);
        }
    }
}
