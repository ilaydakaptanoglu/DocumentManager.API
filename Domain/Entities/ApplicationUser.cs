namespace DocumentManager.API.Models
{
    public class ApplicationUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; }
        public string PasswordHash { get; set; } // şifreyi hashlemen lazım
    }
}
