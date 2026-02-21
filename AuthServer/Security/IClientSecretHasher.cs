namespace AuthServer.Security
{
    public interface IClientSecretHasher
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);
    }
}
