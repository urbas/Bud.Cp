using NUnit.Framework;
using static Bud.Cp;

namespace Bud {
  public class CpTest {
    [Test]
    public void CopyDir_no_source() {
      using (var dir = new TmpDir()) {
        Assert.DoesNotThrow(() => CopyDir(dir.CreatePath("source"), dir.CreatePath("target"),
                                          dir.CreatePath(".target.cp_info")));
      }
    }

    [Test]
    public void CopyDir_initial_copy() {
      using (var dir = new TmpDir()) {
        var srcFile = dir.CreateFile("foo", "source", "foo.txt");
        CopyDir(dir.CreatePath("source"), dir.CreatePath("target"), dir.CreatePath(".target.cp_info"));
        FileAssert.AreEqual(srcFile, dir.CreatePath("target", "foo.txt"));
      }
    }
  }
}