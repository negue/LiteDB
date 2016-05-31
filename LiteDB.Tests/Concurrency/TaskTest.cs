using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using LiteDB.Interfaces;

namespace LiteDB.Tests
{
   public class TestPocoClass
    {
        [BsonId]
        public string Key { get; set; }
        public int Info { get; set; }
    }

    [TestClass]
    public class Task_Test : TestBase
   {
        private static ILiteDatabase db;
        private static LiteCollection<TestPocoClass> col;

      public Task_Test()
        {
            db = LiteDatabaseFactory.Instance.Create(new MemoryStream());
            col = db.GetCollection<TestPocoClass>("col1");
            col.EnsureIndex(o => o.Key);
        }

      ~Task_Test()
        {
            db.Dispose();
        }

        [TestMethod]
        public void FindLocker_Test()
        {
            Assert.AreEqual(col.Count(), 0);

            // insert data
            Task.Factory.StartNew(InsertData).Wait();

            // test inserted data :: Info = 1
            var data = col.FindOne(o => o.Key == "Test1");
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Info);

            // update data :: Info = 77
            Task.Factory.StartNew(UpdateData).Wait();

            // find updated data
            data = col.FindOne(o => o.Key == "Test1");
            Assert.IsNotNull(data);
            Assert.AreEqual(77, data.Info);

            // drop collection
            db.DropCollection("col1");
            Assert.AreEqual(db.CollectionExists("col1"), false);
        }

        private void InsertData()
        {
            var data = new TestPocoClass()
            {
                Key = "Test1",
                Info = 1
            };
            col.Insert(data);
        }

        private void UpdateData()
        {
            var data = col.FindOne(o => o.Key == "Test1");
            Assert.IsNotNull(data);
            data.Info = 77;
            col.Update(data);
        }
    }
}