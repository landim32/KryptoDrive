using System.Security.Cryptography;
using System.Text;
using KryptoDrive.Infra.Interfaces;

namespace KryptoDrive.Infra.Services
{
    public class CryptoService : ICryptoService
    {
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;
        private const string VerificationToken = "KRYPTODRIVE_VALID_V1";

        private byte[]? _masterKey;
        private readonly string _vaultPath;

        public bool HasPassword => _masterKey != null;

        public CryptoService(string basePath)
        {
            _vaultPath = Path.Combine(basePath, "vault");
            Directory.CreateDirectory(_vaultPath);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize);
        }

        public void SetPassword(string password)
        {
            var saltPath = Path.Combine(_vaultPath, ".salt");
            byte[] salt;

            if (File.Exists(saltPath))
            {
                salt = File.ReadAllBytes(saltPath);
            }
            else
            {
                salt = RandomNumberGenerator.GetBytes(SaltSize);
                File.WriteAllBytes(saltPath, salt);
            }

            _masterKey = DeriveKey(password, salt);
        }

        public void ClearPassword()
        {
            if (_masterKey != null)
            {
                CryptographicOperations.ZeroMemory(_masterKey);
                _masterKey = null;
            }
        }

        public bool IsVaultInitialized()
        {
            return File.Exists(Path.Combine(_vaultPath, ".verify"));
        }

        public void InitializeVault(string password)
        {
            SetPassword(password);
            var verifyData = Encrypt(Encoding.UTF8.GetBytes(VerificationToken));
            File.WriteAllBytes(Path.Combine(_vaultPath, ".verify"), verifyData);
        }

        public bool VerifyPassword(string password)
        {
            var verifyPath = Path.Combine(_vaultPath, ".verify");
            if (!File.Exists(verifyPath))
                return false;

            SetPassword(password);
            try
            {
                var verifyData = File.ReadAllBytes(verifyPath);
                var decrypted = Decrypt(verifyData);
                var token = Encoding.UTF8.GetString(decrypted);
                if (token == VerificationToken)
                    return true;

                ClearPassword();
                return false;
            }
            catch (CryptographicException)
            {
                ClearPassword();
                return false;
            }
        }

        public byte[] Encrypt(byte[] plainData)
        {
            if (_masterKey == null)
                throw new InvalidOperationException("Senha nao definida.");

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plainData.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_masterKey, TagSize);
            aes.Encrypt(nonce, plainData, ciphertext, tag);

            // Format: [nonce 12b][tag 16b][ciphertext]
            var result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

            return result;
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            if (_masterKey == null)
                throw new InvalidOperationException("Senha nao definida.");

            if (encryptedData.Length < NonceSize + TagSize)
                throw new CryptographicException("Dados criptografados invalidos.");

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ciphertextLength = encryptedData.Length - NonceSize - TagSize;
            var ciphertext = new byte[ciphertextLength];
            var plaintext = new byte[ciphertextLength];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(encryptedData, NonceSize + TagSize, ciphertext, 0, ciphertextLength);

            using var aes = new AesGcm(_masterKey, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
    }
}
