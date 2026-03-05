namespace KryptoDrive.DTO.DTOs
{
    public class MediaCatalogInfo
    {
        public List<MediaFileInfo> Files { get; set; } = new();
        public List<SecureFolderInfo> Folders { get; set; } = new();
    }
}
