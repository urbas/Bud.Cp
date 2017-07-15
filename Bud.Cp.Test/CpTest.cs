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

      sourceDir = new Uri(dir.CreatePath("source"));
      fooSrcFile = new Uri(dir.CreateFile("foo", "source", "foo.txt"));

      targetDir = new Uri(dir.CreatePath("target"));
      fooTargetFile = new Uri(dir.CreatePath("target", "foo.txt"));

      storage = new Mock<IStorage>();
      var localStorage = new LocalStorage();
      storage.Setup(self => self.CopyFile(It.IsAny<Uri>(), It.IsAny<Uri>()))
             .Callback((Uri sourceFile, Uri targetFile) => localStorage.CopyFile(sourceFile, targetFile));
      storage.Setup(self => self.CreateDirectory(It.IsAny<Uri>()))
             .Callback((Uri dir) => localStorage.CreateDirectory(dir));
      storage.Setup(self => self.DeleteFile(It.IsAny<Uri>()))
             .Callback((Uri file) => localStorage.DeleteFile(file));
      storage.Setup(self => self.EnumerateFiles(It.IsAny<Uri>()))
             .Returns((Uri dir) => localStorage.EnumerateFiles(dir));
      storage.Setup(self => self.GetSignature(It.IsAny<Uri>()))
             .Returns((Uri file) => localStorage.GetSignature(file));
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void CopyDir_no_sources() => Assert.DoesNotThrow(() => CopyDir(new Uri($"{dir}/invalid_dir"), targetDir));

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
      var sourceDir2 = new Uri(dir.CreatePath("source2"));
      var barSrc2File = new Uri(dir.CreateFile("bar", "source2", "bar.txt"));
      var barTargetFile = new Uri(dir.CreatePath("target", "bar.txt"));

      CopyDir(new[] {sourceDir, sourceDir2}, targetDir);

      FileAssert.AreEqual(fooSrcFile.AbsolutePath, fooTargetFile.AbsolutePath);
      FileAssert.AreEqual(barSrc2File.AbsolutePath, barTargetFile.AbsolutePath);
    }

    [Test]
    public void CopyDir_conflicting_files() {
      var sourceDir2 = new Uri(dir.CreateDir("sources2"));
      dir.CreateFile("foo2", "sources2", "foo.txt");

      var exception = Assert.Throws<Exception>(() => CopyDir(new[] {sourceDir, sourceDir2}, targetDir));
      Assert.AreEqual($"Could not copy directories '{sourceDir.AbsolutePath}/' and '{sourceDir2.AbsolutePath}/' " +
                      $"to '{targetDir.AbsolutePath}/'. Both source directories contain file 'foo.txt'.",
                      exception.Message);
    }
  }
}