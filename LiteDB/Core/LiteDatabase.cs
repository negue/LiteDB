using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Interfaces;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public class LiteDatabase : ILiteDatabase
    {
        private LazyLoad<DbEngine> _engine;

        private BsonMapper _mapper;

        private Logger _log = new Logger();

        public ushort Version { get; private set; }

        public Logger Log { get { return _log; } }

       public LiteDatabase()
       {
          
       }

        /// <summary>
        /// Starts LiteDB database using full parameters
        /// </summary>
        public LiteDatabase(IDiskService diskService, ushort version = 0)
        {
         CreateEngine(diskService, version);
        }

       public void CreateEngine(IDiskService diskService, ushort version = 0)
       {
         _engine = new LazyLoad<DbEngine>(
            () => new DbEngine(diskService, _log),
            () => InitializeMapper(),
            () => UpdateDbVersion(version));
      }

      /// <summary>
      /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
      /// </summary>
      /// <param name="name">Collection name (case insensitive)</param>
      public LiteCollection<T> GetCollection<T>(string name)
          where T : new()
      {
         if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

         return new LiteCollection<T>(name, _engine.Value, _mapper, _log);
      }

      /// <summary>
      /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
      /// </summary>
      /// <param name="name">Collection name (case insensitive)</param>
      public LiteCollection<BsonDocument> GetCollection(string name)
      {
         if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

         return new LiteCollection<BsonDocument>(name, _engine.Value, _mapper, _log);
      }

      /// <summary>
      /// Get all collections name inside this database.
      /// </summary>
      public IEnumerable<string> GetCollectionNames()
      {
         return _engine.Value.GetCollectionNames();
      }

      /// <summary>
      /// Checks if a collection exists on database. Collection name is case unsensitive
      /// </summary>
      public bool CollectionExists(string name)
      {
         if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

         return _engine.Value.GetCollectionNames().Contains(name, StringComparer.OrdinalIgnoreCase);
      }

      /// <summary>
      /// Drop a collection and all data + indexes
      /// </summary>
      public bool DropCollection(string name)
      {
         if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

         return _engine.Value.DropCollection(name);
      }

      /// <summary>
      /// Rename a collection. Returns false if oldName does not exists or newName already exists
      /// </summary>
      public bool RenameCollection(string oldName, string newName)
      {
         if (oldName.IsNullOrWhiteSpace()) throw new ArgumentNullException("oldName");
         if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException("newName");

         return _engine.Value.RenameCollection(oldName, newName);
      }

      /// <summary>
      /// Virtual method for update database when a new version (from coneection string) was setted
      /// </summary>
      protected virtual void OnVersionUpdate(int newVersion)
      {
      }

      /// <summary>
      /// Loop in all registered versions and apply all that needs. Update dbversion
      /// </summary>
      private void UpdateDbVersion(ushort recent)
      {
         var dbparams = _engine.Value.GetDbParam();
         this.Version = dbparams.DbVersion;

         for (var newVersion = this.Version + 1; newVersion <= recent; newVersion++)
         {
            _log.Write(Logger.COMMAND, "update database version to {0}", newVersion);

            this.OnVersionUpdate(newVersion);

            this.Version = dbparams.DbVersion = (ushort)newVersion;
            _engine.Value.SetParam(dbparams);
         }
      }

      private LiteFileStorage _fs = null;

      /// <summary>
      /// Returns a special collection for storage files/stream inside datafile
      /// </summary>
      public LiteFileStorage FileStorage
      {
         get { return _fs ?? (_fs = new LiteFileStorage(_engine.Value)); }
      }

       public DbEngine Engine
       {
          get { return _engine.Value; }
          
       }

       /// <summary>
      /// Use mapper cache
      /// </summary>
      private static Dictionary<Type, BsonMapper> _mapperCache = new Dictionary<Type, BsonMapper>();

      private void InitializeMapper()
      {
         var type = this.GetType();

         if (!_mapperCache.TryGetValue(type, out _mapper))
         {
            lock (_mapperCache)
            {
               if (!_mapperCache.TryGetValue(type, out _mapper))
               {
                  _mapper = new BsonMapper();
                  this.OnModelCreating(_mapper);

                  _mapperCache.Add(type, _mapper);
               }
            }
         }
      }

      /// <summary>
      /// Use this method to override and apply rules to map your entities to BsonDocument
      /// </summary>
      protected virtual void OnModelCreating(BsonMapper mapper)
      {
      }

 

      /// <summary>
      /// Reduce datafile size re-creating all collection in another datafile - return how many bytes are reduced.
      /// </summary>
      public long Shrink()
      {
         return _engine.Value.Shrink();
      }

      /// <summary>
      /// Convert a BsonDocument to a class object using BsonMapper rules
      /// </summary>
      public T ToObject<T>(BsonDocument doc)
          where T : new()
      {
         return _mapper.ToObject<T>(doc);
      }

      /// <summary>
      /// Convert a BsonDocument to a class object using BsonMapper rules
      /// </summary>
      public object ToObject(Type type, BsonDocument doc)
      {
         return _mapper.ToObject(type, doc);
      }

      /// <summary>
      /// Convert an entity class instance into a BsonDocument using BsonMapper rules
      /// </summary>
      public BsonDocument ToDocument(object entity)
      {
         return _mapper.ToDocument(entity);
      }

      public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}