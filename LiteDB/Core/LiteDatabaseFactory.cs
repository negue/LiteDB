using System.IO;
using LiteDB.Core;
using LiteDB.Interfaces;

namespace LiteDB
{
   public class LiteDatabaseFactory
   {
      private static LiteDatabaseFactory m_instance;

      public static LiteDatabaseFactory Instance
      {
         get { return m_instance ?? (m_instance = new LiteDatabaseFactory()); }
      }

      private Logger _log = new Logger();

      public ILiteDatabase Create(string connectionString)
      {
         return Create<LiteDatabase>(connectionString);
      }

      public ILiteDatabase Create(Stream stream, ushort version = 0)
      {
         return Create<LiteDatabase>(stream, version);
      }

      public T Create<T>(string connectionString) where T : ILiteDatabase, new()
      {
         var conn = new ConnectionString(connectionString);
         var version = conn.GetValue<ushort>("version", 0);

         var db = new T();
         db.CreateEngine(LiteDbPlatform.Platform.CreateFileDiskService(conn, _log), version);

         return db;
      }

      public T Create<T>(Stream stream, ushort version = 0) where T : ILiteDatabase, new()
      {
         var db = new T();
         db.CreateEngine(new StreamDiskService(stream), version);

         return db;
      }
   }
}