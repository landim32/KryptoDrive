using KryptoDrive.Domain.Models;

namespace KryptoDrive.Tests.Domain
{
    public class SecureFolderTests
    {
        [Fact]
        public void Create_AtRoot_ShouldBuildCorrectPath()
        {
            var folder = SecureFolder.Create("Photos", "/");

            Assert.Equal("Photos", folder.Name);
            Assert.Equal("/Photos", folder.Path);
        }

        [Fact]
        public void Create_Nested_ShouldBuildCorrectPath()
        {
            var folder = SecureFolder.Create("2024", "/Photos");

            Assert.Equal("2024", folder.Name);
            Assert.Equal("/Photos/2024", folder.Path);
        }

        [Fact]
        public void Create_WithSlashes_ShouldSanitize()
        {
            var folder = SecureFolder.Create("my/folder\\name", "/");

            Assert.Equal("my_folder_name", folder.Name);
            Assert.Equal("/my_folder_name", folder.Path);
        }

        [Fact]
        public void Create_WithEmptyName_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => SecureFolder.Create("", "/"));
            Assert.Throws<ArgumentException>(() => SecureFolder.Create("   ", "/"));
        }

        [Fact]
        public void IsChildOf_DirectChild_ShouldReturnTrue()
        {
            var folder = SecureFolder.Create("Photos", "/");

            Assert.True(folder.IsChildOf("/"));
        }

        [Fact]
        public void IsChildOf_NestedChild_ShouldReturnTrue()
        {
            var folder = SecureFolder.Create("2024", "/Photos");

            Assert.True(folder.IsChildOf("/Photos"));
            Assert.False(folder.IsChildOf("/"));
        }

        [Fact]
        public void GetParentPath_Root_ShouldReturnEmpty()
        {
            Assert.Equal("", SecureFolder.GetParentPath("/"));
        }

        [Fact]
        public void GetParentPath_TopLevel_ShouldReturnRoot()
        {
            Assert.Equal("/", SecureFolder.GetParentPath("/Photos"));
        }

        [Fact]
        public void GetParentPath_Nested_ShouldReturnParent()
        {
            Assert.Equal("/Photos", SecureFolder.GetParentPath("/Photos/2024"));
        }

        [Fact]
        public void Reconstitute_ShouldPreserveAllFields()
        {
            var created = new DateTime(2024, 6, 1);
            var folder = SecureFolder.Reconstitute("MyFolder", "/MyFolder", created);

            Assert.Equal("MyFolder", folder.Name);
            Assert.Equal("/MyFolder", folder.Path);
            Assert.Equal(created, folder.CreatedAt);
        }
    }
}
