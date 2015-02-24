namespace CommonDomainLibrary.Security
{
    public interface ICryptoProvider
    {
        string Hmac(string text, byte[] key);
        byte[] Hmac(byte[] data, byte[] key);
    }
}