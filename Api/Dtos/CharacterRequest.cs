namespace Api.Dtos
{
    public class CharacterRequest
    {
        private string _name;
        public string Name => _name;

        public CharacterRequest(string name)
        {
            _name = name;
        }
    }
}