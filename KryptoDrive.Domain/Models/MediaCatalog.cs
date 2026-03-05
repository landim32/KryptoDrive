namespace KryptoDrive.Domain.Models
{
    public class MediaCatalog
    {
        public List<MediaFile> Files { get; private set; } = new();
        public List<SecureFolder> Folders { get; private set; } = new();

        public void AddFolder(SecureFolder folder)
        {
            if (FolderExists(folder.Path))
                throw new InvalidOperationException("Ja existe uma pasta com esse nome.");

            Folders.Add(folder);
        }

        public void RemoveFolder(string folderPath)
        {
            if (FolderHasChildren(folderPath))
                throw new InvalidOperationException("A pasta contem arquivos ou subpastas. Remova-os primeiro.");

            Folders.RemoveAll(f => f.Path == folderPath);
        }

        public void AddFile(MediaFile file)
        {
            Files.Add(file);
        }

        public void RemoveFile(string fileId)
        {
            Files.RemoveAll(f => f.Id == fileId);
        }

        public MediaFile? FindFileById(string fileId)
        {
            return Files.FirstOrDefault(f => f.Id == fileId);
        }

        public bool FolderExists(string path)
        {
            return Folders.Any(f => f.Path == path);
        }

        public bool FolderHasChildren(string folderPath)
        {
            return Files.Any(f => f.FolderPath.StartsWith(folderPath)) ||
                   Folders.Any(f => f.Path != folderPath && f.Path.StartsWith(folderPath));
        }
    }
}
