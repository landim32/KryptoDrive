using KryptoDrive.Domain.Models;

namespace KryptoDrive.Tests.Domain
{
    public class MediaCatalogTests
    {
        [Fact]
        public void AddFolder_ShouldAddToList()
        {
            var catalog = new MediaCatalog();
            var folder = SecureFolder.Create("Photos", "/");

            catalog.AddFolder(folder);

            Assert.Single(catalog.Folders);
            Assert.Equal("/Photos", catalog.Folders[0].Path);
        }

        [Fact]
        public void AddFolder_Duplicate_ShouldThrow()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));

            Assert.Throws<InvalidOperationException>(() =>
                catalog.AddFolder(SecureFolder.Create("Photos", "/")));
        }

        [Fact]
        public void RemoveFolder_Empty_ShouldSucceed()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));

            catalog.RemoveFolder("/Photos");

            Assert.Empty(catalog.Folders);
        }

        [Fact]
        public void RemoveFolder_WithChildren_ShouldThrow()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));
            catalog.AddFile(MediaFile.Create("img.jpg", "abc.enc", "/Photos", "photo", ".jpg", 100));

            Assert.Throws<InvalidOperationException>(() =>
                catalog.RemoveFolder("/Photos"));
        }

        [Fact]
        public void RemoveFolder_WithSubfolders_ShouldThrow()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));
            catalog.AddFolder(SecureFolder.Create("2024", "/Photos"));

            Assert.Throws<InvalidOperationException>(() =>
                catalog.RemoveFolder("/Photos"));
        }

        [Fact]
        public void AddFile_ShouldAddToList()
        {
            var catalog = new MediaCatalog();
            var file = MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100);

            catalog.AddFile(file);

            Assert.Single(catalog.Files);
        }

        [Fact]
        public void RemoveFile_ShouldRemoveFromList()
        {
            var catalog = new MediaCatalog();
            var file = MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100);
            catalog.AddFile(file);

            catalog.RemoveFile(file.Id);

            Assert.Empty(catalog.Files);
        }

        [Fact]
        public void FindFileById_Exists_ShouldReturnFile()
        {
            var catalog = new MediaCatalog();
            var file = MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100);
            catalog.AddFile(file);

            var found = catalog.FindFileById(file.Id);

            Assert.NotNull(found);
            Assert.Equal(file.Id, found.Id);
        }

        [Fact]
        public void FindFileById_NotExists_ShouldReturnNull()
        {
            var catalog = new MediaCatalog();

            Assert.Null(catalog.FindFileById("nonexistent"));
        }

        [Fact]
        public void FolderExists_ShouldReturnCorrectly()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));

            Assert.True(catalog.FolderExists("/Photos"));
            Assert.False(catalog.FolderExists("/Videos"));
        }

        [Fact]
        public void FolderHasChildren_WithFiles_ShouldReturnTrue()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));
            catalog.AddFile(MediaFile.Create("img.jpg", "abc.enc", "/Photos", "photo", ".jpg", 100));

            Assert.True(catalog.FolderHasChildren("/Photos"));
        }

        [Fact]
        public void FolderHasChildren_Empty_ShouldReturnFalse()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));

            Assert.False(catalog.FolderHasChildren("/Photos"));
        }
    }
}
