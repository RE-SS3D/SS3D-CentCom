using System.ComponentModel.DataAnnotations;
using System.Linq;
using Api.Helpers;
using Api.Interfaces;
using Api.Models;

namespace Api.Services
{
    public class  UserService : IUserService
    {
        private DataContext _context;
        private CredentialValidationService _credentialValidationService;

        public UserService(DataContext context)
        {
            _context = context;
            _credentialValidationService = new CredentialValidationService();
        }
        
        public User Authenticate(string username, string password)
        {
            ValidateCredentials(username, password);

            var user = _context.Users.SingleOrDefault(x => x.Username == username);

            //User does not exist
            if (user == null)
            {
                throw new AppException("Incorrect Username or Password.");
            }

            //Incorrect password
            if (!HashHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                throw new AppException("Incorrect Username or Password.");
            }
                
            return user;
        }
        
        public User Create(User user, string password)
        {
            ValidateCredentials(user.Username, password);

            if (_context.Users.Any(x => x.Email == user.Email))
            {
                throw new AppException($"The email {user.Email} is already taken.");
            }
            
            if (_context.Users.Any(x => x.Username == user.Username))
            {
                throw new AppException($"The username {user.Username} is already taken.");
            }

            byte[] passwordHash;
            byte[] passwordSalt;
            HashHelper.CreatePasswordHash(password, out passwordHash, out passwordSalt);
            User newUser = new User();
            newUser.Email = user.Email;
            newUser.Username = user.Username;
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

        private void ValidateCredentials(string username, string password)
        {
            try
            {
                _credentialValidationService.Validate(username, password);
            }
            catch (ValidationException e)
            {
                throw new AppException(e.Message);
            }
        }
    }
}