using KryptoDrive.Domain.Models;

namespace KryptoDrive.Tests.Domain
{
    public class MediaFileTests
    {
        [Fact]
        public void Create_WithValidData_ShouldReturnMediaFile()
        {
            var file = MediaFile.Create("photo.jpg", "abc123.enc", "/", "photo", ".jpg", 1024);

            Assert.Equal("photo.jpg", file.OriginalFileName);
            Assert.Equal("abc123.enc", file.EncryptedFileName);
            Assert.Equal("/", file.FolderPath);
            Assert.Equal("photo", file.MediaType);
            Assert.Equal(".jpg", file.FileExtension);
            Assert.Equal(1024, file.FileSize);
            Assert.NotEmpty(file.Id);
            Assert.Empty(file.Keywords);
        }

        [Fact]
        public void Create_WithEmptyFileName_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() =>
                MediaFile.Create("", "abc.enc", "/", "photo", ".jpg", 100));
        }

        [Fact]
        public void Create_WithInvalidMediaType_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() =>
                MediaFile.Create("file.txt", "abc.enc", "/", "document", ".txt", 100));
        }

        [Fact]
        public void UpdateKeywords_ShouldSetKeywords()
        {
            var file = MediaFile.Create("photo.jpg", "abc.enc", "/", "photo", ".jpg", 100);

            file.UpdateKeywords(new[] { " viagem ", "familia", "", " praia " });

            Assert.Equal(3, file.Keywords.Count);
            Assert.Contains("viagem", file.Keywords);
            Assert.Contains("familia", file.Keywords);
            Assert.Contains("praia", file.Keywords);
        }

        [Fact]
        public void MatchesSearch_ByFileName_ShouldReturnTrue()
        {
            var file = MediaFile.Create("vacation_photo.jpg", "abc.enc", "/", "photo", ".jpg", 100);

            Assert.True(file.MatchesSearch("vacation"));
            Assert.True(file.MatchesSearch("PHOTO"));
        }

        [Fact]
        public void MatchesSearch_ByKeyword_ShouldReturnTrue()
        {
            var file = MediaFile.Create("img001.jpg", "abc.enc", "/", "photo", ".jpg", 100);
            file.UpdateKeywords(new[] { "beach", "summer" });

            Assert.True(file.MatchesSearch("beach"));
            Assert.True(file.MatchesSearch("SUMMER"));
        }

        [Fact]
        public void MatchesSearch_NoMatch_ShouldReturnFalse()
        {
            var file = MediaFile.Create("img001.jpg", "abc.enc", "/", "photo", ".jpg", 100);

            Assert.False(file.MatchesSearch("vacation"));
        }

        [Fact]
        public void MatchesSearch_EmptyQuery_ShouldReturnTrue()
        {
            var file = MediaFile.Create("img001.jpg", "abc.enc", "/", "photo", ".jpg", 100);

            Assert.True(file.MatchesSearch(""));
            Assert.True(file.MatchesSearch(null!));
        }

        [Fact]
        public void Reconstitute_ShouldPreserveAllFields()
        {
            var created = new DateTime(2024, 1, 15, 10, 30, 0);
            var keywords = new List<string> { "tag1", "tag2" };

            var file = MediaFile.Reconstitute(
                "abc123", "photo.jpg", "enc.enc", "/folder",
                "video", ".mp4", keywords, created, 5000);

            Assert.Equal("abc123", file.Id);
            Assert.Equal("photo.jpg", file.OriginalFileName);
            Assert.Equal("/folder", file.FolderPath);
            Assert.Equal("video", file.MediaType);
            Assert.Equal(created, file.CreatedAt);
            Assert.Equal(5000, file.FileSize);
            Assert.Equal(2, file.Keywords.Count);
        }
    }
}
