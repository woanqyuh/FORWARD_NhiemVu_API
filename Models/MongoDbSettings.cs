namespace ForwardMessage.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
    }
    public class AdminAccountSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string Fullname { get; set; }

        public string TeleUser { get; set; }

        public string Webhook { get; set; }
    }
}
