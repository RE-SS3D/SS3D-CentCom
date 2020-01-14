using System.ComponentModel.DataAnnotations;
using System.Linq;
using CentCom.Helpers;
using CentCom.Interfaces;
using CentCom.Models;
using CentCom.Tupples;

namespace CentCom.Services
{
    public class AuthService : IAuthService
    {
        private DataContext _context;
        private CredentialValidationService _credentialValidationService;

        public AuthService(DataContext context)
        {
            _context = context;
            _credentialValidationService = new CredentialValidationService();
        }
        
        public User Authenticate(string email, string password)
        {
            ValidateCredentials(email, password);

            var user = _context.Users.SingleOrDefault(x => x.Email == email);

            //User does not exist
            if (user == null)
            {
                throw new AppException("Incorrect Email of Password.");
            }

            //Incorrect password
            if (!HashHelper.VerifyPasswordHash(password, user.Password))
            {
                throw new AppException("Incorrect Email of Password.");
            }
                
            return user;
        }
        
        public User Create(User user, string password)
        {
            ValidateCredentials(user.Email, password);

            if (_context.Users.Any(x => x.Email == user.Email))
            {
                throw new AppException($"The email {user.Email} is already taken.");
            }

            PasswordHashWithSalt passwordHashWithSalt = HashHelper.CreatePasswordHash(password);
            user.Password = passwordHashWithSalt;

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }
        
        public User GetById(int id)
        {
            return _context.Users.Find(id);
        }

        private void ValidateCredentials(string email, string password)
        {
            try
            {
                _credentialValidationService.Validate(email, password);
            }
            catch (ValidationException e)
            {
                throw new AppException(e.Message);
            }
        }
    }
}