using CentCom.Dtos;

namespace CentCom.Models
{
    public class Character
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }

        public static Character From(CharacterRequest characterRequest)
        {
            var character = new Character();
            character.Name = characterRequest.Name;
            return character;
        }
    }
}