namespace Api.Dtos
{
    public class AuthenticateRequest
    {
        private string _username;
        private string _email;
        private string _password;

        public string Username => _username;
        public string Email => _email;
        public string Password => _password;

        public AuthenticateRequest(string username, string email, string password)
        {
            _username = username;
            _email = email;
            _password = password;
        }
    }
}