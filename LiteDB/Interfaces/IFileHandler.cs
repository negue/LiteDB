﻿using System.IO;

namespace LiteDB.Interfaces
{
   public interface IFileHandler
   {
      Stream ReadFileAsStream(string file);
   }
}