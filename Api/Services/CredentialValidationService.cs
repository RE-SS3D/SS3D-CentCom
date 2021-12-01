using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Api.Interfaces;

namespace Api.Services
{
    public class CredentialValidationService : ICredentialValidationService
    {
        public void Validate(string username, string email, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ValidationException("User can not be empty.");
            }

            if (Regex.IsMatch(username, "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
            {
                throw new ValidationException("Username is not valid.");
            }
            
            if (string.IsNullOrEmpty(password))
            {
                throw new ValidationException("Password can not be empty.");
            }

            if (email.Length > 1 && !Regex.IsMatch(email, "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
            {
                throw new ValidationException($"{email} is not a valid email address.");
            }

            if (Regex.IsMatch(password, "[<>'\"`]"))
            {
                throw new ValidationException("Password may not contain < > \" ' ` characters.");
            }
        }
    }
}