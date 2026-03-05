namespace KryptoDrive.Infra.Interfaces
{
    public interface ICryptoService
    {
        void SetPassword(string password);
        byte[] Encrypt(byte[] plainData);
        byte[] Decrypt(byte[] encryptedData);
        bool VerifyPassword(string password);
        void InitializeVault(string password);
        bool IsVaultInitialized();
        void ClearPassword();
        bool HasPassword { get; }
    }
}
