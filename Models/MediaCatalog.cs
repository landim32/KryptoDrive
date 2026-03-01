namespace KryptoDrive.Models
{
    public class MediaCatalog
    {
        public List<EncryptedFileInfo> Files { get; set; } = new();
        public List<SecureFolder> Folders { get; set; } = new();
    }
}
