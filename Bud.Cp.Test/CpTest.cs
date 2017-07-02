using System;
using System.IO;
using Moq;
using NUnit.Framework;
using static Bud.Cp;

namespace Bud {
  public class CpTest {
    private TmpDir dir;
    private string fooSrcFile;
    private Mock<Action<string, string>> copyMock;
    private string fooTargetFile;
    private string sourceDir;
    private string targetDir;

    [SetUp]
    public void SetUp() {
      dir = new TmpDir();
      fooSrcFile = dir.CreateFile("foo", "source", "foo.txt");
      fooTargetFile = dir.CreatePath("target", "foo.txt");
      sourceDir = dir.CreatePath("source");
      targetDir = dir.CreatePath("target");
      copyMock = new Mock<Action<string, string>>();
      copyMock.Setup(self => self(It.IsAny<string>(), It.IsAny<string>()))
              .Callback((string sourceFile, string targetFile) => CopyFile(sourceFile, targetFile));
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void CopyDir_no_sources()
      => Assert.DoesNotThrow(() => CopyDir($"{dir}/invalid_dir", targetDir));

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
    public void CopyDir_copy_if_modified() {
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
  }
}