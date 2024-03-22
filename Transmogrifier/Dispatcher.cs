using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Transmogrification;

public class Dispatcher(HttpConfig httpConfig)
{
    public async Task<bool> Transmogrify(TransmogrificationSettings settings)
    {
        using var httpClient = CreateHttpClient();
        httpClient.BaseAddress = httpConfig.Url;
        
        using var findPerson = await httpClient.GetAsync($"People/{settings.Name}");
        if ( findPerson.StatusCode == HttpStatusCode.NotFound)
        {
            using var postPerson = await httpClient.PostAsJsonAsync("/People/new", new { Name = settings.Name });
            if (!postPerson.IsSuccessStatusCode)
            {
                throw new ApplicationException("Failed to create person");
            }
        }
        
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Transmogrification = settings.Transformation
            }),
            Encoding.UTF8,
            "application/json");
        
        using var postTransformation =
             await httpClient.PostAsync(
                 $"/Transmogrifier/{settings.Name}/new", 
                 jsonContent
                 );

        if (postTransformation.IsSuccessStatusCode)
        {
            using var getTransformations = await httpClient.GetAsync($"/Transmogrifier/{settings.Name}");
            if (getTransformations.IsSuccessStatusCode)
            {
                var transformations = await getTransformations.Content.ReadAsStringAsync();
                if (transformations.Contains(settings.Transformation))
                {
                    return true;
                }
            }
        }
        
        return false;

    }
    
    private HttpClient CreateHttpClient()
    {
        var socketHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        };

        return new HttpClient(socketHandler);
    }
}