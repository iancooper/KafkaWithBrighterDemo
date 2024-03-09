﻿using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter;
using Transmogrification.Application.Ports.Driven;

namespace Transmogrification.Mappers
{
    public class TransmogrificationRequestedMessageMapperAsync : IAmAMessageMapperAsync<TransmogrificationRequested>
    {
        public async Task<Message> MapToMessageAsync(TransmogrificationRequested request, CancellationToken cancellationToken = default)
        {
            //NOTE: We are showing an async pipeline here, but it is often overkill by comparison to using 
            //TaskCompletionSource for a Task over sync instead
            var header = new MessageHeader(messageId: request.Id, topic: "TransmogrificationRequested", messageType: MessageType.MT_EVENT);
            using var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, request, new JsonSerializerOptions(JsonSerializerDefaults.General), cancellationToken);
            var body = new MessageBody(ms.ToArray());
            return new Message(header, body);
         }

        public Task<TransmogrificationRequested> MapToRequestAsync(Message message, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}