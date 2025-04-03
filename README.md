# 🔐 KryptoDrive

**KryptoDrive** is a mobile app that encrypts files before saving them to external drives, ensuring that only authorized users with the correct password can access them.  

## 🚀 Features
- **AES-256-GCM Encryption**: Strong encryption ensures file security.
- **Per-File Passphrase Protection**: Each file is individually protected with a unique passphrase.
- **Secure External Storage**: Encrypt and store files on external drives (USB, SD cards, etc.).
- **Cross-Platform Support**: Built with .NET MAUI for Android and iOS.
- **User-Friendly Interface**: Simple and intuitive file encryption and decryption.

## 🛠️ How It Works
1. **Select a file** to encrypt.
2. **Enter a passphrase** to protect the file.
3. **Save the encrypted file** to an external drive.
4. **To decrypt**, enter the correct passphrase.

## 📌 Security Overview
- **AES-256-GCM** encryption ensures data confidentiality and integrity.
- **PBKDF2 Key Derivation** with 100,000 iterations for enhanced password security.
- **Unique salt for each file** to prevent precomputed attacks.

## 🏗️ Tech Stack
- **.NET MAUI** (Cross-platform mobile development)
- **C#** (Core application logic)
- **Xamarin.Essentials** (For file system access)
- **System.Security.Cryptography** (For AES encryption)

## 🔧 Installation
### Prerequisites
- .NET 8 SDK
- MAUI workload installed
- Android/iOS emulator or physical device

### Setup
1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/KryptoDrive.git
   cd KryptoDrive
   ```

2. Restore dependencies:
   ```sh
   dotnet restore
   ```

3. Build and run:
   ```sh
   dotnet build
   dotnet run
   ```

## 🎯 Roadmap
- [ ] Cloud backup for encrypted files
- [ ] Support for biometric authentication
- [ ] File sharing with encrypted links
- [ ] Desktop version (Windows/Linux/Mac)

## 🛡️ License
This project is licensed under the MIT License.

## 🤝 Contributing
Pull requests are welcome! Feel free to submit issues and suggestions.

---

💡 **KryptoDrive – Your Data, Secure Forever!** 🔒
