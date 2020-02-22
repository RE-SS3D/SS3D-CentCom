using System.Collections.Generic;
using CentCom.Models;

namespace CentCom.Interfaces
{
    public interface ICharacterService
    {
        IEnumerable<Character> GetForUser(long userId);
        Character Create(Character character, long userId);
        void Delete(long userId, long id);
    }
}