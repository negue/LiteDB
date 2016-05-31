using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Storage;
using Windows.Storage.Streams;
using LiteDB.Universal81;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
   public class TestClassAttribute: TestPlatform.UnitTestFramework.TestClassAttribute { }

   public class TestMethodAttribute : TestPlatform.UnitTestFramework.TestMethodAttribute { }

   public class Assert : TestPlatform.UnitTestFramework.Assert { }
}

namespace LiteDB.Tests
{
   public class TestPlatform
   {
      public static string GetTempFilePath(string ext)
      {
         return string.Format("test-{0}.{1}", Guid.NewGuid(), ext);
      }

      public static void DeleteFile(string path)
      {
         var file = AsyncHelpers.RunSync(ApplicationData.Current.TemporaryFolder.GetFileAsync(path));

         file.DeleteAsync();
      }

      public static string FileReadAllText(string path)
      {

         var file = AsyncHelpers.RunSync(ApplicationData.Current.TemporaryFolder.GetFileAsync(path));

         var buffer = AsyncHelpers.RunSync(FileIO.ReadBufferAsync(file));

         var arr = buffer.ToArray();

         var res = Encoding.UTF8.GetString(arr, 0, arr.Length);

         return res;

      }
   }
}