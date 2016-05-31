using LiteDB.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
   public class TestBase
   {
      public TestBase()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformFullDotNet());
      }
   }
}
