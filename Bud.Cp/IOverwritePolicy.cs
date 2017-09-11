using System;

namespace Bud {
  /// <summary>
  /// Provides file signatures will be used to compare files. 
  /// </summary>
  public interface IOverwritePolicy {
    /// <summary>
    /// This function compares the source file (<paramref name="sourceAbsPath"/>) and the target file
    /// (<paramref name="targetAbsPath"/>). If the files differ this function will return <c>true</c>, otherwise the
    /// function will return <c>false</c>.
    /// </summary>
    /// <param name="sourceAbsPath">the absolute path of the source file.</param>
    /// <param name="targetAbsPath">the absolute path of the target file</param>
    bool ShouldOverwrite(Uri sourceAbsPath, Uri targetAbsPath);
  }
}