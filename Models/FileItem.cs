namespace KryptoDrive.Models
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "folder_icon.png";
        public string Subtitle { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public string? FileId { get; set; }
        public string FolderPath { get; set; } = "/";
        public string MediaType { get; set; } = "photo";
        public string? EncryptedFileName { get; set; }
    }
}
