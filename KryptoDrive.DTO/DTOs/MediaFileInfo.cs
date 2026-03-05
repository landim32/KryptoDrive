namespace KryptoDrive.DTO.DTOs
{
    public class MediaFileInfo
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string EncryptedFileName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = "/";
        public string MediaType { get; set; } = "photo";
        public string FileExtension { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long FileSize { get; set; }
    }
}
