using System.Collections.Generic;
using System.Linq;
using CentCom.Interfaces;
using CentCom.Models;

namespace CentCom.Services
{
    public class CharacterService : ICharacterService
    {
        private DataContext _context;

        public CharacterService(DataContext context)
        {
            _context = context;
        }
        
        public IEnumerable<Character> GetForUser(long userId)
        {
            return _context.Characters.Where(character => character.UserId == userId);
        }

        public Character Create(Character character, long userId)
        {
            character.UserId = userId;
            
            _context.Characters.Add(character);
            _context.SaveChanges();

            return character;
        }

        public void Delete(long userId, long id)
        {
            var character = _context.Characters.Find(id);
            if (character != null && userId == character.UserId)
            {
                _context.Characters.Remove(character);
                _context.SaveChanges();
            }
        }
    }
}