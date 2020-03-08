using System.Collections.Generic;
using Api.Models;

namespace Api.Interfaces
{
    public interface ICharacterService
    {
        IEnumerable<Character> GetForUser(long userId);
        Character Create(Character character, long userId);
        void Delete(long userId, long id);
    }
}