using System.IO;
using LiteDB.Interfaces;

namespace LiteDB
{
   public class LiteDbPlatformFullDotNet : LiteDbPLatformBase
   {
      public override FileDiskServiceBase CreateFileDiskService(ConnectionString conn, Logger log)
      {
         return new FileDiskServiceDotNet(conn, log);
      }

      public LiteDbPlatformFullDotNet(IEncryptionFactory encryptionFactory, IReflectionHandler reflectionHandler, IFileHandler fileHandler) 
         : base(() => encryptionFactory, () => reflectionHandler, () => fileHandler)
      {
      }

      public LiteDbPlatformFullDotNet() : base(() => new EncryptionFactory(), 
         () => new EmitReflectionHandler(), () => new FileHandler())
      {
      }
   }

   public class FileHandler : IFileHandler
   {
      public Stream ReadFileAsStream(string filename)
      {
         return File.OpenRead(filename);
      }
   }

   public class EncryptionFactory : IEncryptionFactory
   {
      public IEncryption CreateEncryption(string password)
      {
         return new SimpleAES(password);
      }

      public byte[] HashSHA1(string str)
      {
         return SimpleAES.HashSHA1(str);
      }
   }
}
