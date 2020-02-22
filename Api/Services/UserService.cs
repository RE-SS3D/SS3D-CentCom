using System.ComponentModel.DataAnnotations;
using System.Linq;
using CentCom.Helpers;
using CentCom.Interfaces;
using CentCom.Models;

namespace CentCom.Services
{
    public class UserService : IUserService
    {
        private DataContext _context;
        private CredentialValidationService _credentialValidationService;

        public UserService(DataContext context)
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
                throw new AppException("Incorrect Email or Password.");
            }

            //Incorrect password
            if (!HashHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                throw new AppException("Incorrect Email or Password.");
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

            byte[] passwordHash;
            byte[] passwordSalt;
            HashHelper.CreatePasswordHash(password, out passwordHash, out passwordSalt);
            User newUser = new User();
            newUser.Email = user.Email;
            newUser.PasswordHash = passwordHash;
            newUser.PasswordSalt = passwordSalt;

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return user;
        }
        
        public User GetById(long id)
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