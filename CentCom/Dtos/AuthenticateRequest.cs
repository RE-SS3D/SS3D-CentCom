namespace CentCom.Dtos
{
    public class AuthenticateRequest
    {
        private string _email;
        private string _password;

        public string Email => _email;
        public string Password => _password;

        public AuthenticateRequest(string email, string password)
        {
            _email = email;
            _password = password;
        }
    }
}