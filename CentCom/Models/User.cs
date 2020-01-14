using CentCom.Tupples;

namespace CentCom.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public PasswordHashWithSalt Password { get; set; }
    }
}