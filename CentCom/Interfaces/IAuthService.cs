using CentCom.Models;

namespace CentCom.Interfaces
{
    public interface IAuthService
    {
        User Authenticate(string username, string password);
        User Create(User user, string password);
        User GetById(int id);
    }
}