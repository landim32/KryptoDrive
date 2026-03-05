using KryptoDrive.Domain.Models;
using KryptoDrive.Infra.AppServices;
using KryptoDrive.Infra.Interfaces;
using KryptoDrive.Infra.Mappers;
using AutoMapper;
using Moq;

namespace KryptoDrive.Tests.AppServices
{
    public class VaultAppServiceTests
    {
        private readonly Mock<IVaultRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly VaultAppService _service;

        public VaultAppServiceTests()
        {
            _repoMock = new Mock<IVaultRepository>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<VaultMapperProfile>());
            _mapper = config.CreateMapper();
            _service = new VaultAppService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task GetCatalogAsync_ShouldReturnMappedCatalog()
        {
            var catalog = new MediaCatalog();
            catalog.AddFile(MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100));
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));
            _repoMock.Setup(r => r.GetCatalogAsync()).ReturnsAsync(catalog);

            var result = await _service.GetCatalogAsync();

            Assert.Single(result.Files);
            Assert.Single(result.Folders);
            Assert.Equal("img.jpg", result.Files[0].OriginalFileName);
            Assert.Equal("/Photos", result.Folders[0].Path);
        }

        [Fact]
        public async Task StoreMediaAsync_ShouldStoreAndReturnInfo()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            _repoMock.Setup(r => r.StoreFileAsync(It.IsAny<Stream>(), "photo.jpg", "photo"))
                .ReturnsAsync("abc123.enc");
            _repoMock.Setup(r => r.GetFilesPath()).Returns(Path.GetTempPath());

            // Create a fake encrypted file so FileInfo can read its size
            var tempFile = Path.Combine(Path.GetTempPath(), "abc123.enc");
            await File.WriteAllBytesAsync(tempFile, new byte[100]);

            var emptyCatalog = new MediaCatalog();
            _repoMock.Setup(r => r.GetCatalogAsync()).ReturnsAsync(emptyCatalog);

            try
            {
                var result = await _service.StoreMediaAsync(stream, "photo.jpg", "photo", "/");

                Assert.Equal("photo.jpg", result.OriginalFileName);
                Assert.Equal("abc123.enc", result.EncryptedFileName);
                _repoMock.Verify(r => r.SaveCatalogAsync(It.IsAny<MediaCatalog>()), Times.Once);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldDeleteFromRepoAndCatalog()
        {
            var catalog = new MediaCatalog();
            var file = MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100);
            catalog.AddFile(file);
            _repoMock.Setup(r => r.GetCatalogAsync()).ReturnsAsync(catalog);

            await _service.DeleteFileAsync(file.Id, "abc.enc");

            _repoMock.Verify(r => r.DeleteFileAsync("abc.enc"), Times.Once);
            _repoMock.Verify(r => r.SaveCatalogAsync(It.Is<MediaCatalog>(c => c.Files.Count == 0)), Times.Once);
        }

        [Fact]
        public async Task CreateFolderAsync_ShouldAddFolderToCatalog()
        {
            var catalog = new MediaCatalog();
            _repoMock.Setup(r => r.GetCatalogAsync()).ReturnsAsync(catalog);

            await _service.CreateFolderAsync("Photos", "/");

            _repoMock.Verify(r => r.SaveCatalogAsync(It.Is<MediaCatalog>(c =>
                c.Folders.Count == 1 && c.Folders[0].Path == "/Photos")), Times.Once);
        }

        [Fact]
        public async Task CreateFolderAsync_Duplicate_ShouldThrow()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));
            _repoMock.Setup(r => r.GetCatalogAsync()).ReturnsAsync(catalog);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateFolderAsync("Photos", "/"));
        }

        [Fact]
        public async Task UpdateKeywordsAsync_ShouldUpdateAndSave()
        {
            var catalog = new MediaCatalog();
            var file = MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100);
            catalog.AddFile(file);
            _repoMock.Setup(r => r.GetCatalogAsync()).ReturnsAsync(catalog);

            await _service.UpdateKeywordsAsync(file.Id, new[] { "beach", "summer" });

            _repoMock.Verify(r => r.SaveCatalogAsync(It.Is<MediaCatalog>(c =>
                c.Files[0].Keywords.Count == 2)), Times.Once);
        }

        [Fact]
        public void GetFileItems_Browse_ShouldReturnFoldersAndFiles()
        {
            var catalog = new MediaCatalog();
            catalog.AddFolder(SecureFolder.Create("Photos", "/"));
            catalog.AddFile(MediaFile.Create("img.jpg", "abc.enc", "/", "photo", ".jpg", 100));
            var catalogInfo = _mapper.Map<DTO.DTOs.MediaCatalogInfo>(catalog);

            var items = _service.GetFileItems(catalogInfo, "/", null);

            Assert.Equal(2, items.Count);
            Assert.True(items[0].IsFolder);
            Assert.False(items[1].IsFolder);
        }

        [Fact]
        public void GetFileItems_Search_ShouldFilterByQuery()
        {
            var catalog = new MediaCatalog();
            var file1 = MediaFile.Create("vacation.jpg", "a.enc", "/", "photo", ".jpg", 100);
            var file2 = MediaFile.Create("work.jpg", "b.enc", "/", "photo", ".jpg", 100);
            catalog.AddFile(file1);
            catalog.AddFile(file2);
            var catalogInfo = _mapper.Map<DTO.DTOs.MediaCatalogInfo>(catalog);

            var items = _service.GetFileItems(catalogInfo, "/", "vacation");

            Assert.Single(items);
            Assert.Equal("vacation.jpg", items[0].Name);
        }

        [Fact]
        public void FormatFileSize_ShouldFormatCorrectly()
        {
            Assert.Equal("500 B", VaultAppService.FormatFileSize(500));
            Assert.Contains("KB", VaultAppService.FormatFileSize(1536));
            Assert.Contains("MB", VaultAppService.FormatFileSize(2 * 1024 * 1024));
            Assert.Contains("GB", VaultAppService.FormatFileSize(1L * 1024 * 1024 * 1024));
        }
    }
}
