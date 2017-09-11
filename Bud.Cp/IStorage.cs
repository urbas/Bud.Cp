using System;
using System.Collections.Generic;

namespace Bud {
  /// <summary>
  /// The API needed for
  /// <see cref="Cp.CopyDir(System.Collections.Generic.IEnumerable{System.Uri},System.Uri,Bud.IStorage)"/> to do the
  /// copying.
  /// 
  /// See <see cref="LocalStorage"/> for an implementation of local filesystem storage.
  /// </summary>
  public interface IStorage {
  /// <summary>
  ///   Creates a directory at the given URI.
  /// </summary>
  /// <param name="dir">the URI of the directory that should be created.</param>
  void CreateDirectory(Uri dir);

  /// <summary>
  ///   Returns a list of all the files in the given directory. This method must enumerate files recursively.
  /// </summary>
  /// <param name="dir">the directory whose files to enumerate.</param>
  /// <returns>an enumerable of all files found in the given directory.</returns>
  IEnumerable<Uri> EnumerateFiles(Uri dir);

  /// <summary>
  ///   Returns a list of all subdirectories of the given directory. This method must enumerate directories
  ///   recursively.
  /// </summary>
  /// <param name="dir">the directory whose subdirectories to enumerate.</param>
  /// <returns>an enumerable of all subdirectories found in the given directory.</returns>
  IEnumerable<Uri> EnumerateDirectories(Uri dir);

  /// <summary>
  /// Copies a single file from the source path to the target path.
  /// </summary>
  /// <param name="sourceFile">the file to copy to the new location.</param>
  /// <param name="targetFile">the location where to place a copy of the source file.</param>
  void CopyFile(Uri sourceFile, Uri targetFile);

  /// <summary>
  ///   Deletes the file at the given location.
  /// </summary>
  /// <param name="file">the file to delete.</param>
  void DeleteFile(Uri file);

  /// <summary>
  ///   Deletes the directory and all of its contents recursively.
  /// </summary>
  /// <param name="dir">the directory to delete.</param>
  void DeleteDirectory(Uri dir);
  }
}