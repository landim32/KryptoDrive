namespace KryptoDrive.Domain.Models
{
    public class MediaFile
    {
        public string Id { get; private set; } = string.Empty;
        public string OriginalFileName { get; private set; } = string.Empty;
        public string EncryptedFileName { get; private set; } = string.Empty;
        public string FolderPath { get; private set; } = "/";
        public string MediaType { get; private set; } = "photo";
        public string FileExtension { get; private set; } = string.Empty;
        public List<string> Keywords { get; private set; } = new();
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public long FileSize { get; private set; }

        private MediaFile() { }

        public static MediaFile Create(
            string originalFileName,
            string encryptedFileName,
            string folderPath,
            string mediaType,
            string fileExtension,
            long fileSize)
        {
            var file = new MediaFile
            {
                Id = Guid.NewGuid().ToString("N"),
                OriginalFileName = originalFileName,
                EncryptedFileName = encryptedFileName,
                FolderPath = folderPath,
                MediaType = mediaType,
                FileExtension = fileExtension,
                FileSize = fileSize,
                CreatedAt = DateTime.UtcNow,
                Keywords = new List<string>()
            };
            file.Validate();
            return file;
        }

        public static MediaFile Reconstitute(
            string id,
            string originalFileName,
            string encryptedFileName,
            string folderPath,
            string mediaType,
            string fileExtension,
            List<string> keywords,
            DateTime createdAt,
            long fileSize)
        {
            return new MediaFile
            {
                Id = id,
                OriginalFileName = originalFileName,
                EncryptedFileName = encryptedFileName,
                FolderPath = folderPath,
                MediaType = mediaType,
                FileExtension = fileExtension,
                Keywords = keywords ?? new List<string>(),
                CreatedAt = createdAt,
                FileSize = fileSize
            };
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OriginalFileName))
                throw new ArgumentException("Original file name is required.");
            if (string.IsNullOrWhiteSpace(EncryptedFileName))
                throw new ArgumentException("Encrypted file name is required.");
            if (string.IsNullOrWhiteSpace(FolderPath))
                throw new ArgumentException("Folder path is required.");
            if (MediaType != "photo" && MediaType != "video")
                throw new ArgumentException("Media type must be 'photo' or 'video'.");
        }

        public void UpdateKeywords(IEnumerable<string> keywords)
        {
            Keywords = keywords
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList();
        }

        public bool MatchesSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;

            return OriginalFileName.Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                   Keywords.Any(k => k.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
