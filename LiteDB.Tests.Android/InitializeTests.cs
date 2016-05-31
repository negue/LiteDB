﻿using LiteDB.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace LiteDB.Tests
{
   [TestClass]
   public class InitializeTests
   {
      [AssemblyInitialize]
      public void AssemblyLoaded()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformFullDotNet(new EncryptionFactory(),
            new ExpressionReflectionHandler(), new FileHandler()));
      }

      [AssemblyCleanup]
      public void AssemblyCleanup()
      {
         // wait all threads close FileDB
         System.Threading.Thread.Sleep(2000);

         DB.DeleteFiles();
      }
   }
}