using System.IO;

namespace LiteDB.Extensions
{
   public static class LiteFileInfoExtensions
   {
      /// <summary>
      /// Save file content to a external file
      /// </summary>
      public static void SaveAs(this LiteFileInfo fileInfo, string filename, bool overwritten = true)
      {
         using (var file = new FileStream(filename, overwritten ? FileMode.Create : FileMode.CreateNew))
         {
            fileInfo.OpenRead().CopyTo(file);
         }
      }
   }
}
