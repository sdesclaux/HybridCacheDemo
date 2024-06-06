using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HybridCacheDemo
{
    public class WeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly HybridCache _hybridCache;
            
        public WeatherService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IDistributedCache distributedCache, HybridCache hybridCache)
        {
            _httpClientFactory = httpClientFactory;
            _distributedCache = distributedCache;
            _hybridCache = hybridCache;
        }

         //*** In Memory cache *** Caution : not thread safe and atomic operation
        public async Task<WeatherResponse?> GetInMemoryCurentWeatherAsync(string city)
        {
            var cachekey = $"weather:{city}";
            var weather = await _memoryCache.GetOrCreateAsync(cachekey,
                async _ => await GetWeatherAsync(city));
            return weather;
        }

        // *** Distributed Cache (Redis or Garnet) *** Old School : GetOrCreate doesn't exists, you need to do it manually + Caution : not thread safe and atomic operation
        public async Task<WeatherResponse?> GetIDistributedCachedCurentWeatherAsync(string city)
        {
            var cachekey = $"weather:{city}";

            var weatherJson = await _distributedCache.GetStringAsync(cachekey);

            if (weatherJson is null)
            {
                var weather = await GetWeatherAsync(city);
                await _distributedCache.SetStringAsync(cachekey,
                    JsonSerializer.Serialize(weather));
                return weather;
            }

            return JsonSerializer.Deserialize<WeatherResponse>(weatherJson);
        }

        // *** Hybrid Cache with GetOrCreateAsync (Redis or Garnet or InMemory can be used) *** New way
        public async Task<WeatherResponse?> GetHybridCachedCurentWeatherAsync(string city, CancellationToken token = default)
        {
            var cachekey = $"weather:{city}";

            return await _hybridCache.GetOrCreateAsync(
                cachekey,
                async cancel => await GetWeatherAsync(city),
                new HybridCacheEntryOptions
                {
                    Expiration = new TimeSpan(0, 5, 0)
                },
                token: token);
        }

        private async Task<WeatherResponse?> GetWeatherAsync(string city)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid=d881505ac8fccf2b039eaca8ed72172f";
            var httpClient = _httpClientFactory.CreateClient();
            var weatherResponse = await httpClient.GetAsync(url);
            if (weatherResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            return await weatherResponse.Content.ReadFromJsonAsync<WeatherResponse>();
        }
    }
}
