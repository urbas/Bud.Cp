using System;
using System.IO;
using Moq;
using NUnit.Framework;
using static Bud.Cp;

namespace Bud {
  public class CpTest {
    private TmpDir dir;
    private Mock<Action<string, string>> copyMock;
    private string sourceDir;
    private string fooSrcFile;
    private string fooTargetFile;
    private string targetDir;

    [SetUp]
    public void SetUp() {
      dir = new TmpDir();

      sourceDir = ToUri(dir.CreatePath("source"));
      fooSrcFile = ToUri(dir.CreateFile("foo", "source", "foo.txt"));

      targetDir = ToUri(dir.CreatePath("target"));
      fooTargetFile = ToUri(dir.CreatePath("target", "foo.txt"));

      copyMock = new Mock<Action<string, string>>();
      copyMock.Setup(self => self(It.IsAny<string>(), It.IsAny<string>()))
              .Callback((string sourceFile, string targetFile) => CopyFile(sourceFile, targetFile));
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void CopyDir_no_sources() => Assert.DoesNotThrow(() => CopyDir($"{dir}/invalid_dir", targetDir));

    [Test]
    public void CopyDir_initial_copy() {
      CopyDir(sourceDir, targetDir);
      FileAssert.AreEqual(fooSrcFile, fooTargetFile);
    }

    [Test]
    public void CopyDir_skip_unmodified() {
      CopyDir(sourceDir, targetDir, copyMock.Object);
      CopyDir(sourceDir, targetDir, copyMock.Object);
      copyMock.Verify(s => s(fooSrcFile, fooTargetFile), Times.Once);
    }

    [Test]
    public void CopyDir_overwrite_if_modified() {
      CopyDir(sourceDir, targetDir, copyMock.Object);
      File.WriteAllText(fooSrcFile, "foo v2");
      CopyDir(sourceDir, targetDir, copyMock.Object);
      copyMock.Verify(s => s(fooSrcFile, fooTargetFile), Times.Exactly(2));
    }

    [Test]
    public void CopyDir_remove_deleted_files() {
      CopyDir(sourceDir, targetDir, copyMock.Object);
      File.Delete(fooSrcFile);
      CopyDir(sourceDir, targetDir, copyMock.Object);
      FileAssert.DoesNotExist(fooTargetFile);
    }

    [Test]
    public void CopyDir_from_multiple_source_directories() {
      var sourceDir2 = dir.CreatePath("source2");
      var barSrc2File = dir.CreateFile("bar", "source2", "bar.txt");
      var barTargetFile = dir.CreatePath("target", "bar.txt");

      CopyDir(new[] {sourceDir, sourceDir2}, targetDir);

      FileAssert.AreEqual(fooSrcFile, fooTargetFile);
      FileAssert.AreEqual(barSrc2File, barTargetFile);
    }

    [Test]
    public void CopyDir_conflicting_files() {
      var sourceDir2 = ToUri(dir.CreateDir("sources2"));
      dir.CreateFile("foo2", "sources2", "foo.txt");

      var exception = Assert.Throws<Exception>(() => CopyDir(new[] {sourceDir, sourceDir2}, targetDir));
      Assert.AreEqual($"Could not copy directories '{sourceDir}/' and '{sourceDir2}/' to '{targetDir}/'. Both " +
                      $"directories contain file 'foo.txt'.",
                      exception.Message);
    }

    [Test]
    public void CopyDir_uses_custom_file_signatures() {
      var fileSignaturesMock = new Mock<IFileSignatures>();

      CopyDir(sourceDir, targetDir, fileSignatures: fileSignaturesMock.Object);
      CopyDir(sourceDir, targetDir, fileSignatures: fileSignaturesMock.Object);

      fileSignaturesMock.Verify(self => self.GetSignature(fooSrcFile), Times.Once);
      fileSignaturesMock.Verify(self => self.GetSignature(fooTargetFile), Times.Once);
    }

    private static string ToUri(string path) => new Uri(path).AbsolutePath;
  }
}