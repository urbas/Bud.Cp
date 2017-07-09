using System.IO;
using System.Security.Cryptography;

namespace Bud {
  public class Sha256FileSignatures : IFileSignatures {
    private readonly byte[] buffer = new byte[16384];

    public byte[] GetSignature(string file) => DigestFile(file, buffer);

    private static byte[] DigestFile(string file, byte[] buffer) {
      var hashAlgorithm = SHA256.Create();
      hashAlgorithm.Initialize();
      using (var fileStream = File.OpenRead(file)) {
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