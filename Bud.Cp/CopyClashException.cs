using System;

namespace Bud {
  /// <summary>
  ///   This exception is thrown when copying multiple source directories into a single targets directory and when two
  ///   source directories contain a file with the same relative path.
  /// </summary>
  public class CopyClashException : Exception {
    /// <summary>
    ///   The first source directory that contains the clashing file.
    /// </summary>
    public string SourceDir1 { get; }
    
    /// <summary>
    ///   The second source directory that contains the clashing file.
    /// </summary>
    public string SourceDir2 { get; }
    
    /// <summary>
    ///   The target directory into which we tried to copy.
    /// </summary>
    public string TargetDir { get; }
    /// <summary>
    ///   The relative path of the file that is present in both <see cref="SourceDir1"/> and <see cref="SourceDir2"/>
    ///   and is the cause of the clash.
    /// </summary>
    public Uri FileRelPath { get; }

    /// <summary>
    ///   This constructor stores the detailed information of the clash and creates the message of the exception.
    /// </summary>
    public CopyClashException(string sourceDir1, string sourceDir2, string targetDir, Uri fileRelPath)
      : base($"Could not copy directories '{sourceDir1}' and '{sourceDir2}' to '{targetDir}'. " +
             $"Both source directories contain file '{fileRelPath}'.") {
      SourceDir1 = sourceDir1;
      SourceDir2 = sourceDir2;
      TargetDir = targetDir;
      FileRelPath = fileRelPath;
    }
  }
}