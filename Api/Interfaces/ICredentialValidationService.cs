namespace Api.Interfaces
{
    public interface ICredentialValidationService
    {
         void Validate(string username, string password);
    }
}