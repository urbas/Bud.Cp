using System;
using System.IO;
using Moq;
using NUnit.Framework;
using static Bud.Cp;

namespace Bud {
  public class CpTest {
    private TmpDir dir;
    private Mock<IStorage> storage;
    private Uri sourceDir;
    private Uri fooSrcFile;
    private Uri fooTargetFile;
    private Uri targetDir;

    [SetUp]
    public void SetUp() {
      dir = new TmpDir();
      sourceDir = CreatePath("source");
      fooSrcFile = CreateFile("foo", "source", "foo.txt");
      targetDir = CreatePath("target");
      fooTargetFile = CreatePath("target", "foo.txt");
      storage = SpiedLocalStorage();
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void CopyDir_no_sources() {
      CopyDir(new Uri($"{dir}/invalid_dir"), targetDir);
      DirectoryAssert.Exists(targetDir.AbsolutePath);
    }

    [Test]
    public void CopyDir_initial_copy() {
      CopyDir(sourceDir, targetDir);
      FileAssert.AreEqual(fooSrcFile.AbsolutePath, fooTargetFile.AbsolutePath);
    }

    [Test]
    public void CopyDir_skip_unmodified() {
      CopyDir(sourceDir, targetDir, storage.Object);
      CopyDir(sourceDir, targetDir, storage.Object);
      storage.Verify(s => s.CopyFile(fooSrcFile, fooTargetFile), Times.Once);
      storage.Verify(s => s.GetSignature(fooSrcFile), Times.Once);
      storage.Verify(s => s.GetSignature(fooTargetFile), Times.Once);
    }

    [Test]
    public void CopyDir_overwrite_if_modified() {
      CopyDir(sourceDir, targetDir, storage.Object);
      File.WriteAllText(fooSrcFile.AbsolutePath, "foo v2");
      CopyDir(sourceDir, targetDir, storage.Object);
      storage.Verify(s => s.CopyFile(fooSrcFile, fooTargetFile), Times.Exactly(2));
      FileAssert.AreEqual(fooSrcFile.AbsolutePath, fooTargetFile.AbsolutePath);
    }

    [Test]
    public void CopyDir_remove_deleted_files() {
      CopyDir(sourceDir, targetDir, storage.Object);
      File.Delete(fooSrcFile.AbsolutePath);
      CopyDir(sourceDir, targetDir, storage.Object);
      FileAssert.DoesNotExist(fooTargetFile.AbsolutePath);
    }

    [Test]
    public void CopyDir_from_multiple_source_directories() {
      var sourceDir2 = CreatePath("source2");
      var barSrc2File = CreateFile("bar", "source2", "bar.txt");
      var barTargetFile = CreatePath("target", "bar.txt");

      CopyDir(new[] {sourceDir, sourceDir2}, targetDir);

      FileAssert.AreEqual(fooSrcFile.AbsolutePath, fooTargetFile.AbsolutePath);
      FileAssert.AreEqual(barSrc2File.AbsolutePath, barTargetFile.AbsolutePath);
    }

    [Test]
    public void CopyDir_conflicting_files() {
      var sourceDir2 = CreateDir("sources2");
      dir.CreateFile("foo2", "sources2", "foo.txt");

      var exception = Assert.Throws<CopyClashException>(() => CopyDir(new[] {sourceDir, sourceDir2}, targetDir));
      Assert.AreEqual($"Could not copy directories '{sourceDir}/' and '{sourceDir2}/' " +
                      $"to '{targetDir}/'. Both source directories contain file 'foo.txt'.",
                      exception.Message);
    }

    [Test]
    public void CopyDir_subdirectories() {
      var nestedSrcFile = CreateFile("42", "source", "bar", "baz.txt");
      var nestedTargetFile = CreatePath("target", "bar", "baz.txt");
      CopyDir(sourceDir, targetDir);
      FileAssert.AreEqual(nestedSrcFile.AbsolutePath, nestedTargetFile.AbsolutePath);
    }

    [Test]
    public void CopyDir_delete_removed_subdirectories() {
      var sourceSubDir = CreateDir("source", "bar");
      var targetSubDir = CreatePath("target", "bar");
      CopyDir(sourceDir, targetDir);
      Directory.Delete(sourceSubDir.AbsolutePath);
      CopyDir(sourceDir, targetDir);
      DirectoryAssert.DoesNotExist(targetSubDir.AbsolutePath);
    }

    private Uri CreatePath(params string[] subPath) => new Uri(dir.CreatePath(subPath));
    private Uri CreateDir(params string[] subDir) => new Uri(dir.CreateDir(subDir));
    private Uri CreateFile(string contents, params string[] subPath) => new Uri(dir.CreateFile(contents, subPath));

    private static Mock<IStorage> SpiedLocalStorage() {
      var mockStorage = new Mock<IStorage>();
      var localStorage = new LocalStorage();
      mockStorage.Setup(self => self.CopyFile(It.IsAny<Uri>(), It.IsAny<Uri>()))
                 .Callback((Uri sourceFile, Uri targetFile) => localStorage.CopyFile(sourceFile, targetFile));
      mockStorage.Setup(self => self.CreateDirectory(It.IsAny<Uri>()))
                 .Callback((Uri dir) => localStorage.CreateDirectory(dir));
      mockStorage.Setup(self => self.DeleteFile(It.IsAny<Uri>()))
                 .Callback((Uri file) => localStorage.DeleteFile(file));
      mockStorage.Setup(self => self.DeleteDirectory(It.IsAny<Uri>()))
                 .Callback((Uri dir) => localStorage.DeleteDirectory(dir));
      mockStorage.Setup(self => self.EnumerateFiles(It.IsAny<Uri>()))
                 .Returns((Uri dir) => localStorage.EnumerateFiles(dir));
      mockStorage.Setup(self => self.EnumerateDirectories(It.IsAny<Uri>()))
                 .Returns((Uri dir) => localStorage.EnumerateDirectories(dir));
      mockStorage.Setup(self => self.GetSignature(It.IsAny<Uri>()))
                 .Returns((Uri file) => localStorage.GetSignature(file));
      return mockStorage;
    }
  }
}