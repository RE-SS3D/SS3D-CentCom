namespace CentCom.Dtos
{
    public class AuthenticateRequest
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}