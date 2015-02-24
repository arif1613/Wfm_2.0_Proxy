namespace CommonDomainLibrary
{
    public interface ICredentialsEncryptionService
    {
        string Encrypt(string value);
        string Decrypt(string cypher);
    }
}
