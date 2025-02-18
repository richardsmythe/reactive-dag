using System.Net;
using System.Net.Http.Json;

namespace ReactiveDAG.tests.TestHelpers
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            if (request.RequestUri.AbsolutePath.Contains("/user/") &&
                !request.RequestUri.AbsolutePath.Contains("/posts"))
            {
                var userDetails = new UserDetails { UserId = "user123" };
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(userDetails)
                };
            }
            else if (request.RequestUri.AbsolutePath.Contains("/posts"))
            {
                var userPosts = new UserPosts
                {
                    UserId = "user123",
                    Posts = new List<Post>
                    {
                        new Post { Title = "Title1", Content = "Content1" },
                        new Post { Title = "Title2", Content = "Content2" }
                    }
                };
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(userPosts)
                };
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return Task.FromResult(response);
        }
    }
}