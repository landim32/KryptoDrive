using AutoMapper;
using KryptoDrive.Domain.Models;
using KryptoDrive.DTO.DTOs;
using KryptoDrive.Infra.Mappers;

namespace KryptoDrive.Tests.Mappers
{
    public class VaultMapperProfileTests
    {
        private readonly IMapper _mapper;

        public VaultMapperProfileTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<VaultMapperProfile>());
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Configuration_ShouldBeValid()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<VaultMapperProfile>());
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void MediaFile_To_MediaFileInfo_ShouldMapAllFields()
        {
            var file = MediaFile.Create("photo.jpg", "abc.enc", "/folder", "photo", ".jpg", 2048);
            file.UpdateKeywords(new[] { "tag1", "tag2" });

            var info = _mapper.Map<MediaFileInfo>(file);

            Assert.Equal(file.Id, info.Id);
            Assert.Equal("photo.jpg", info.OriginalFileName);
            Assert.Equal("abc.enc", info.EncryptedFileName);
            Assert.Equal("/folder", info.FolderPath);
            Assert.Equal("photo", info.MediaType);
            Assert.Equal(".jpg", info.FileExtension);
            Assert.Equal(2048, info.FileSize);
            Assert.Equal(2, info.Keywords.Count);
        }

        [Fact]
        public void SecureFolder_To_SecureFolderInfo_ShouldMapAllFields()
        {
            var folder = SecureFolder.Create("Photos", "/");

            var info = _mapper.Map<SecureFolderInfo>(folder);

            Assert.Equal("Photos", info.Name);
            Assert.Equal("/Photos", info.Path);
        }

        [Fact]
        public void MediaCatalog_To_MediaCatalogInfo_ShouldMapCollections()
        {
            var catalog = new MediaCatalog();
            catalog.AddFile(MediaFile.Create("a.jpg", "a.enc", "/", "photo", ".jpg", 100));
            catalog.AddFile(MediaFile.Create("b.mp4", "b.enc", "/", "video", ".mp4", 200));
            catalog.AddFolder(SecureFolder.Create("Folder1", "/"));

            var info = _mapper.Map<MediaCatalogInfo>(catalog);

            Assert.Equal(2, info.Files.Count);
            Assert.Single(info.Folders);
            Assert.Equal("a.jpg", info.Files[0].OriginalFileName);
            Assert.Equal("Folder1", info.Folders[0].Name);
        }
    }
}
