namespace CentCom.Tupples
{
    public class PasswordHashWithSalt
    {
        private byte[] _passwordHash;
        private byte[] _passwordSalt;

        private PasswordHashWithSalt(byte[] passwordHash, byte[] passwordSalt)
        {
            _passwordHash = passwordHash;
            _passwordSalt = passwordSalt;
        }

        public byte[] PasswordHash => _passwordHash;

        public byte[] PasswordSalt => _passwordSalt;

        public static PasswordHashWithSalt Of(byte[] passwordHash, byte[] passwordSalt)
        {
            return new PasswordHashWithSalt(passwordHash, passwordSalt);
        }
    }
}