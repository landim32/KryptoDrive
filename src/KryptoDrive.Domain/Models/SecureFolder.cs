namespace KryptoDrive.Domain.Models
{
    public class SecureFolder
    {
        public string Name { get; private set; } = string.Empty;
        public string Path { get; private set; } = "/";
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private SecureFolder() { }

        public static SecureFolder Create(string name, string parentPath)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name is required.");

            var sanitizedName = name.Trim().Replace("/", "_").Replace("\\", "_");

            var fullPath = parentPath == "/"
                ? $"/{sanitizedName}"
                : $"{parentPath}/{sanitizedName}";

            return new SecureFolder
            {
                Name = sanitizedName,
                Path = fullPath,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static SecureFolder Reconstitute(string name, string path, DateTime createdAt)
        {
            return new SecureFolder
            {
                Name = name,
                Path = path,
                CreatedAt = createdAt
            };
        }

        public bool IsChildOf(string parentPath)
        {
            return GetParentPath(Path) == parentPath;
        }

        public static string GetParentPath(string path)
        {
            if (path == "/") return "";
            var trimmed = path.TrimEnd('/');
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash <= 0 ? "/" : trimmed[..lastSlash];
        }
    }
}
