using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;

namespace ReactiveDAG.tests.TestHelpers
{
    public interface IApiService
    {
        Task<UserDetails> FetchDataAsync(string userId);
        Task<UserPosts> GetUserPostsAsync(string userId);
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserDetails?> FetchDataAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.example.com/user/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserDetails>();
                }
                else
                {
                    throw new Exception($"Failed to fetch user details. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user details: {ex.Message}");
                throw;
            }
        }

        public async Task<UserPosts?> GetUserPostsAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.example.com/user/{userId}/posts");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserPosts>();
                }
                else
                {
                    throw new Exception($"Failed to fetch user posts. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user posts: {ex.Message}");
                throw;
            }
        }
    }
}
