using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;

namespace System.IO.Abstractions.XUnitTests
{
    public class DeletionsTests
    {
        [Fact]
        public void Test1()
        {

        }

        [Fact]
        public void DeleteDirectoryRecursive_WithReadOnlyFile_ShouldThrowExceptionAndNotDeleteDirectory()
        {
            var sut = new MockFileSystem();
            sut.Directory.CreateDirectory("foo");
            sut.File.WriteAllText("foo/bar.txt", "xyz");
            sut.File.SetAttributes("foo/bar.txt", FileAttributes.ReadOnly);

            var exception = Record.Exception(() =>
            {
                sut.Directory.Delete("foo", true);
            });

            exception.Should().BeAssignableTo<UnauthorizedAccessException>();
            sut.File.Exists("foo/bar.txt").Should().BeTrue();
            sut.Directory.Exists("foo").Should().BeTrue();
        }

        [Fact]
        public void DeleteDirectory_WithReadOnlyFlag_ShouldThrowExceptionAndNotDeleteDirectory()
        {
            var sut = new MockFileSystem();
            var dir = sut.Directory.CreateDirectory("foo");
            dir.Attributes = FileAttributes.ReadOnly | FileAttributes.Directory;
            
            var attributes = dir.Attributes;
            var exception = Record.Exception(() =>
            {
                sut.Directory.Delete("foo");
            });

            exception.Should().BeAssignableTo<UnauthorizedAccessException>();
            sut.Directory.Exists("foo").Should().BeTrue();
        }

        [Fact]
        public void DeleteFile_WithReadOnlyFlag_ShouldThrowExceptionAndNotDeleteFile()
        {
            const string folder = @"c:\Users\marku\tests";
            const string path = @"c:\Users\marku\tests\foo.txt";

            var fileList = new List<string>();
            fileList.Add(folder);
            fileList.Add(path);

            Directory.CreateDirectory(fileList[0]);
            using (var file = File.Create(fileList[1]))
            {
                File.SetAttributes(fileList[1], FileAttributes.ReadOnly);
            };
            
            Directory.Delete(fileList[0], true);
            //lock(fileList)
            //{
            //    Directory.Delete(fileList[0], true);
            //}

        }
    }
}