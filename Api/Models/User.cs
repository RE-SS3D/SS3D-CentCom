using Api.Dtos;

namespace Api.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash{ get; set; }
        public byte[] PasswordSalt{ get; set; }

        public static User From(AuthenticateRequest authenticateRequest)
        {
            User user = new User();
            user.Username = authenticateRequest.Username;
            user.Email = authenticateRequest.Email;
            return user;
        }
    }
}