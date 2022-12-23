using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

using Contracts;


namespace WindowApp
{
    public class Service
    {
        private static readonly string serverAddress = "https://localhost:7262/api/faceEmbeddings/";
        private static readonly int maxRetries = 5;

        private Random jitterer;
        private readonly AsyncRetryPolicy retryPolicy;

        public Service()
        {
            jitterer = new();
            retryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(
                maxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                      + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)));
        }

        public async Task<List<int>?> ProcessImagesAsync(List<ImageDetails> images)
        {
            HttpResponseMessage response = new();
            try
            {
                await retryPolicy.ExecuteAsync(async () => {
                    using var client = new HttpClient()
                    {
                        BaseAddress = new Uri(serverAddress)
                    };

                    var content = CreateImageContent(images);
                    response = await client.PostAsync("images", content);
                    response.EnsureSuccessStatusCode();
                });
            }
            catch
            {
                return null;
            }

            var response_str = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<int>>(response_str);
        }

        public async Task<bool> Cancel()
        {
            HttpResponseMessage response = new();
            try
            {
                await retryPolicy.ExecuteAsync(async () => {
                    using var client = new HttpClient()
                    {
                        BaseAddress = new Uri(serverAddress)
                    };

                    response = await client.GetAsync("cancel");
                    response.EnsureSuccessStatusCode();
                });
            }
            catch
            {
                return false;
            }

            var response_str = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<bool>(response_str);
        }

        public async Task<Dictionary<string, float?>?> Compare(int id1, int id2)
        {
            HttpResponseMessage response = new();
            try
            {
                await retryPolicy.ExecuteAsync(async () => {
                    using var client = new HttpClient()
                    {
                        BaseAddress = new Uri(serverAddress)
                    };

                    response = await client.GetAsync($"compare?id1={id1}&id2={id2}");

                    response.EnsureSuccessStatusCode();
                });
            }
            catch (Exception ex)
            {
                return null;
            }

            var response_str = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, float?>>(response_str);
        }

        public async Task<List<ImageDetails>?> GetImages()
        {
            HttpResponseMessage response = new();
            try
            {
                await retryPolicy.ExecuteAsync(async () => {
                    using var client = new HttpClient()
                    {
                        BaseAddress = new Uri(serverAddress)
                    };

                    response = await client.GetAsync("images");
                    response.EnsureSuccessStatusCode();
                });
            }
            catch
            {
                return null;
            }

            var response_str = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ImageDetails>>(response_str);
        }

        public async Task<bool> DeleteImageById(int id)
        {
            HttpResponseMessage response = new();
            try
            {
                await retryPolicy.ExecuteAsync(async () => {
                    using var client = new HttpClient()
                    {
                        BaseAddress = new Uri(serverAddress)
                    };

                    response = await client.DeleteAsync($"images/id?id={id}");
                    response.EnsureSuccessStatusCode();
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Clear()
        {
            HttpResponseMessage response = new();
            try
            {
                await retryPolicy.ExecuteAsync(async () => {
                    using var client = new HttpClient()
                    {
                        BaseAddress = new Uri(serverAddress)
                    };

                    response = await client.DeleteAsync("images");

                    response.EnsureSuccessStatusCode();
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static HttpContent CreateImageContent(List<ImageDetails> images)
        {
            var content = new StringContent(JsonConvert.SerializeObject(images));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }
    }
}
