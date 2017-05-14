using System;
using Moq;
using NUnit.Framework;
using static Bud.Cp;

namespace Bud {
  public class CpTest {
    private TmpDir dir;
    private string fooSrcFile;

    [SetUp]
    public void SetUp() {
      dir = new TmpDir();
      fooSrcFile = dir.CreateFile("foo", "source", "foo.txt");
    }

    [TearDown]
    public void TearDown() => dir.Dispose();

    [Test]
    public void CopyDir_no_sources()
      => Assert.DoesNotThrow(() => CopyDir($"{dir}/invalid_dir", $"{dir}/target", $"{dir}/.target.cp_info"));

    [Test]
    public void CopyDir_initial_copy() {
      CopyDir($"{dir}/source", $"{dir}/target", $"{dir}/.target.cp_info");
      FileAssert.AreEqual(fooSrcFile, $"{dir}/target/foo.txt");
    }

    [Test]
    public void CopyDir_skip_copy() {
      var copyFunctionMock = new Mock<Action<string, string>>();
      CopyDir($"{dir}/source", $"{dir}/target", $"{dir}/.target.cp_info");
      CopyDir($"{dir}/source", $"{dir}/target", $"{dir}/.target.cp_info", copyFunctionMock.Object);
      copyFunctionMock.Verify(s => s(fooSrcFile, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CopyDir_copy_if_modified() {
      var copyFunctionMock = new Mock<Action<string, string>>();
      CopyDir($"{dir}/source", $"{dir}/target", $"{dir}/.target.cp_info");
      dir.CreateFile("foo v2", "source", "foo.txt");
      CopyDir($"{dir}/source", $"{dir}/target", $"{dir}/.target.cp_info", copyFunctionMock.Object);
      copyFunctionMock.Verify(s => s(fooSrcFile, It.IsAny<string>()), Times.Once);
    }
  }
}