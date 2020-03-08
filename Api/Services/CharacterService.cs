using System.Collections.Generic;
using System.Linq;
using Api.Helpers;
using Api.Interfaces;
using Api.Models;

namespace Api.Services
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
            Character newCharacter = new Character();
            newCharacter.Id = character.Id;
            newCharacter.UserId = userId;
            newCharacter.Name = character.Name;
            
            _context.Characters.Add(newCharacter);
            _context.SaveChanges();

            return newCharacter;
        }

        public void Delete(long userId, long id)
        {
            var character = _context.Characters.Find(id);
            if (character == null)
            {
                throw new AppException($"Could not find character with ID {id}");
            }

            if (userId != character.UserId)
            {
                throw new AppException("Deleting other people's characters is forbidden.");
            }
            _context.Characters.Remove(character);
            _context.SaveChanges();
        }
    }
}