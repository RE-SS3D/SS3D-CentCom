namespace Api.Interfaces
{
    public interface ICredentialValidationService
    {
         void Validate(string username, string email, string password);
    }
}