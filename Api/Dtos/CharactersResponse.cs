using Api.Models;

namespace Api.Dtos
{
    public class CharactersResponse
    {
        private Character[] _characters;

        public Character[] Characters => _characters;

        public CharactersResponse(Character[] characters)
        {
            _characters = characters;
        }
    }
}