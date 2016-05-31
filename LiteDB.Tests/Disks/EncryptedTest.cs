using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;
using LiteDB.Shell;

namespace LiteDB.Tests
{
    [TestClass]
    public class EncryptedTest : TestBase
   {
        [TestMethod]
        public void Encrypted_Order()
        {
            var encrypt = DB.RandomFile();
            var plain = DB.RandomFile();

            var cs_enc = "password=abc;filename=" + encrypt;
            var cs_enc_wrong = "password=abcd;filename=" + encrypt;

            // create a database with no password - plain data
            using (var db = LiteDatabaseFactory.Instance.Create(plain))
            {
            var shell = new LiteShell(db);
                shell.Run("db.col1.insert {name:\"Mauricio David\"}");
            }

            // read datafile to find "Mauricio" string
            Assert.IsTrue(TestPlatform.FileReadAllText(plain).Contains("Mauricio David"));

            // create a database with password
            using (var db = LiteDatabaseFactory.Instance.Create(cs_enc))
         {
            var shell = new LiteShell(db);
            shell.Run("db.col1.insert {name:\"Mauricio David\"}");
            }

            // test if is possible find "Mauricio" string
            Assert.IsFalse(TestPlatform.FileReadAllText(encrypt).Contains("Mauricio David"));

            // try access using wrong password
            using (var db = LiteDatabaseFactory.Instance.Create(cs_enc_wrong))
            {
                try
            {
               var shell = new LiteShell(db);
               shell.Run("show collections");

                    Assert.Fail(); // can't work
                }
                catch(LiteException ex)
                {
                    Assert.IsTrue(ex.ErrorCode == 123); // wrong password
                }
            }
        }
    }
}