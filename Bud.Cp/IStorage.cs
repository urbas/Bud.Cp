using System;
using System.Collections.Generic;

namespace Bud {
  public interface IStorage {
    void CreateDirectory(Uri dir);
    IEnumerable<Uri> EnumerateFiles(Uri dir);
    IEnumerable<Uri> EnumerateDirectories(Uri dir);
    void CopyFile(Uri sourceFile, Uri targetFile);
    byte[] GetSignature(Uri file);
    void DeleteFile(Uri file);
  }
}