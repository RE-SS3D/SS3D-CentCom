using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CentCom.Interfaces;

namespace CentCom.Services
{
    public class CredentialValidationService : ICredentialValidationService
    {
        public void Validate(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ValidationException("Email can not be empty.");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ValidationException("Password can not be empty.");
            }

            if (!Regex.IsMatch(email, "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
            {
                throw new ValidationException($"{email} is not a valid email address.");
            }

            if (!Regex.IsMatch(password, "[a-zA-Z0-9@.!_+=#$%^&*?|{}()]"))
            {
                throw new ValidationException("Password may only contain latin alphanumeric and . ! _ + = # $ % ^ & * ? | { } ( ) characters.");
            }
        }
    }
}