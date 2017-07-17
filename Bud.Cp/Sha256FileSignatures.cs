using System;
using System.IO;
using System.Security.Cryptography;

namespace Bud {
  internal class Sha256FileSignatures {
    private readonly byte[] buffer = new byte[16384];

    public byte[] GetSignature(Uri file) {
      var hashAlgorithm = SHA256.Create();
      hashAlgorithm.Initialize();
      using (var fileStream = File.OpenRead(file.AbsolutePath)) {
        int readBytes;
        do {
          readBytes = fileStream.Read(buffer, 0, buffer.Length);
          hashAlgorithm.TransformBlock(buffer, 0, readBytes, buffer, 0);
        } while (readBytes == buffer.Length);
      }
      hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
      return hashAlgorithm.Hash;
    }
  }
}