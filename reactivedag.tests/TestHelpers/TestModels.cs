namespace ReactiveDAG.tests.TestHelpers
{
    public class UserDetails
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
    }

    public class UserPosts
    {
        public string? UserId { get; set; }
        public List<Post>? Posts { get; set; }
    }


    public class Post
    {
        public string? Content { get; set; }
        public string? Title { get; set; }
    }

    public class AdditionalData
    {
        public bool Processed { get; set; }
        public int PostCount { get; set; }
    }

    public class ProcessedPost
    {
        public Post? Post { get; set; }
        public bool Processed { get; set; }
    }

    public class FinalResult
    {
        public string? UserId { get; set; }
        public int PostCount { get; set; }
        public bool AdditionalDataProcessed { get; set; }
    }
}