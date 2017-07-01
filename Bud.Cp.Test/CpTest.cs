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

    [SetUp]
    public void SetUp() {
      dir = new TmpDir();
      fooSrcFile = dir.CreateFile("foo", "source", "foo.txt");
      fooTargetFile = dir.CreatePath("target", "foo.txt");
      copyMock = new Mock<Action<string, string>>();
      copyMock.Setup(self => self(It.IsAny<string>(), It.IsAny<string>()))
              .Callback((string sourceFile, string targetFile) => CopyFile(sourceFile, targetFile));
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void CopyDir_no_sources()
      => Assert.DoesNotThrow(() => CopyDir($"{dir}/invalid_dir", $"{dir}/target"));

    [Test]
    public void CopyDir_initial_copy() {
      CopyDir($"{dir}/source", $"{dir}/target");
      FileAssert.AreEqual(fooSrcFile, fooTargetFile);
    }

    [Test]
    public void CopyDir_skip_unmodified() {
      CopyDir($"{dir}/source", $"{dir}/target", copyMock.Object);
      CopyDir($"{dir}/source", $"{dir}/target", copyMock.Object);
      copyMock.Verify(s => s(fooSrcFile, fooTargetFile), Times.Once);
    }

    [Test]
    public void CopyDir_copy_if_modified() {
      CopyDir($"{dir}/source", $"{dir}/target", copyMock.Object);
      File.WriteAllText(fooSrcFile, "foo v2");
      CopyDir($"{dir}/source", $"{dir}/target", copyMock.Object);
      copyMock.Verify(s => s(fooSrcFile, fooTargetFile), Times.Exactly(2));
    }
  }
}