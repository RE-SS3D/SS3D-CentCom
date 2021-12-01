using Api.Models;

namespace Api.Interfaces
{
    public interface IUserService
    {
        User Authenticate(string username, string email, string password);
        User Create(User user, string password);
        User GetById(long id);
    }
}