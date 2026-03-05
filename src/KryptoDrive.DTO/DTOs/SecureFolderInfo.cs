namespace KryptoDrive.DTO.DTOs
{
    public class SecureFolderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
