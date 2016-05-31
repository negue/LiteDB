#if !PCL
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using LiteDB.Shell;

namespace LiteDB.Tests
{
    public class VerDatabase : LiteDatabase
    {
        public VerDatabase()
        {
            this.Log.Level = Logger.FULL;
            this.Log.Logging += (m) => Debug.Print(m);
        }

        protected override void OnVersionUpdate(int newVersion)
        {
         var shell = new LiteShell(this);
            if (newVersion == 1)
                shell.Run("db.col1.insert {_id:1}");

            if (newVersion == 2)
                shell.Run("db.col2.insert {_id:2}");

            if (newVersion == 3)
                shell.Run("db.col3.insert {_id:3}");
        }
    }

    [TestClass]
    public class DbVersionTest : TestBase
   {
        [TestMethod]
        public void DbVerion_Test()
        {
            var m = new MemoryStream();

            using (var db = LiteDatabaseFactory.Instance.Create<VerDatabase>(m, 1))
            {
                Assert.AreEqual(true, db.CollectionExists("col1"));
                Assert.AreEqual(false, db.CollectionExists("col2"));
                Assert.AreEqual(false, db.CollectionExists("col3"));
            }

         using (var db = LiteDatabaseFactory.Instance.Create<VerDatabase>(m, 2))
            {
                Assert.AreEqual(true, db.CollectionExists("col1"));
                Assert.AreEqual(true, db.CollectionExists("col2"));
                Assert.AreEqual(false, db.CollectionExists("col3"));
            }

         using (var db = LiteDatabaseFactory.Instance.Create<VerDatabase>(m, 3))
            {
                Assert.AreEqual(true, db.CollectionExists("col1"));
                Assert.AreEqual(true, db.CollectionExists("col2"));
                Assert.AreEqual(true, db.CollectionExists("col3"));
            }
        }
    }
}
#endif