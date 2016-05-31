using LiteDB.Core;
using LiteDB.Interfaces;
using Windows.Storage;

namespace LiteDB.Universal81
{
   public class LiteDbPlatformWindowsStore : LiteDbPLatformBase
   {
      private readonly StorageFolder m_folder;

      public LiteDbPlatformWindowsStore(StorageFolder folder, IEncryptionFactory encryptionFactory = null) 
         : base(() => encryptionFactory ?? new EncryptionFactory(), () => new ExpressionReflectionHandler(), 
              () => new FileHandlerWindowsStore(folder))
      {
         m_folder = folder;
      }

      public override FileDiskServiceBase CreateFileDiskService(ConnectionString conn, Logger log)
      {
         return new FileDiskService(m_folder, conn, log);
      }
   }
}
