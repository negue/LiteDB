using System;
using System.Collections.Generic;
using System.IO;
using LiteDB.Core;
using LiteDB.Interfaces;

namespace LiteDB
{
   public abstract class FileDiskServiceBase : IDiskService
   {
       /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        protected Stream _stream;
        private string _filename;
        protected long _lockLength;

        private string _tempFilename;
        protected Stream _journal;
        private string _journalFilename;
        private bool _journalEnabled;
        private HashSet<uint> _journalPages = new HashSet<uint>();

        protected Logger _log;
      protected TimeSpan _timeout;
        protected bool _readonly;
        private long _initialSize;
        private long _limitSize;

      private IEncryption _crypto;

      private byte[] _password;
      private bool _useEncryption;

      #region Initialize disk

      protected FileDiskServiceBase(ConnectionString conn, Logger log)
        {
            _filename = conn.GetValue<string>("filename", "");
            var journalEnabled = conn.GetValue<bool>("journal", true);
            _timeout = conn.GetValue<TimeSpan>("timeout", new TimeSpan(0, 1, 0));
            _readonly = conn.GetValue<bool>("readonly", false);
            _initialSize = conn.GetFileSize("initial size", 0);
            _limitSize = conn.GetFileSize("limit size", 0);
            var level = conn.GetValue<byte?>("log", null);

         // initialize AES with passoword
         var password = conn.GetValue<string>("password", null);

         if (!string.IsNullOrEmpty(password))
         {
            _useEncryption = true;

            var encryptionFactory = LiteDbPlatform.Platform.EncryptionFactory;

            // hash password to store in header to check
            _password = encryptionFactory.HashSHA1(password);

            _crypto = encryptionFactory.CreateEncryption(password);
         }

         // simple validations
         if (_filename.IsNullOrWhiteSpace()) throw new ArgumentNullException("filename");
            if (_initialSize > 0 && _initialSize < BasePage.GetSizeOfPages(10)) throw new ArgumentException("initial size too low");
            if (_limitSize > 0 && _limitSize < BasePage.GetSizeOfPages(10)) throw new ArgumentException("limit size too low");
            if (_initialSize > 0 && _limitSize > 0 && _initialSize > _limitSize) throw new ArgumentException("limit size less than initial size");

            // setup log + log-level
            _log = log;
            if (level.HasValue) _log.Level = level.Value;

            _journalEnabled = !_readonly && journalEnabled; // readonly? no journal
            _journalFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-journal" + Path.GetExtension(_filename));
            _tempFilename = Path.Combine(Path.GetDirectoryName(_filename), Path.GetFileNameWithoutExtension(_filename) + "-temp" + Path.GetExtension(_filename));
        }

      protected abstract Stream CreateStream(string filename);

        /// <summary>
        /// Open datafile - returns true if new
        /// </summary>
        public virtual bool Initialize()
        {
            _log.Write(Logger.DISK, "open datafile '{0}', page size {1}", Path.GetFileName(_filename), BasePage.PAGE_SIZE);

           _stream = CreateStream(_filename);

            if (_stream.Length == 0)
            {
                _log.Write(Logger.DISK, "initialize new datafile");

                // if has a initial size, reserve this space
                if (_initialSize > 0)
                {
                    _log.Write(Logger.DISK, "initial datafile size {0}", _initialSize);

                    _stream.SetLength(_initialSize);
                }

                return true;
            }

         this.TryRecovery();
           return false;
        }

        /// <summary>
        /// Create new database - just create empty header page
        /// </summary>
        public virtual void CreateNew()
        {

         var header = new HeaderPage();
           if (_useEncryption)
           {
              header.DbParams.Password = _password;
           }
         this.WritePage(0, header.WritePage());
        }

#endregion Initialize disk

#region Lock/Unlock

      protected abstract void InnerLock();
      protected abstract void InnerUnlock();

        /// <summary>
        /// Lock datafile agains other process read/write
        /// </summary>
        public void Lock()
        {
            InnerLock();
        }

        /// <summary>
        /// Release lock
        /// </summary>
        public void Unlock()
        {
            _log.Write(Logger.DISK, "unlock datafile");
           InnerUnlock();
        }

#endregion Lock/Unlock

#region Read/Write

        /// <summary>
        /// Read first 2 bytes from datafile - contains changeID (avoid to read all header page)
        /// </summary>
        public ushort GetChangeID()
        {
            var bytes = new byte[2];

            this.TryExec(() =>
            {
                _stream.Seek(HeaderPage.CHANGE_ID_POSITION, SeekOrigin.Begin);
                _stream.Read(bytes, 0, 2);
            });

            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public virtual byte[] ReadPage(uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            this.TryExec(() =>
            {
                // position cursor
                if (_stream.Position != position)
                {
                    _stream.Seek(position, SeekOrigin.Begin);
                }

                // read bytes from data file
                _stream.Read(buffer, 0, BasePage.PAGE_SIZE);
            });

            _log.Write(Logger.DISK, "read page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);


           if (_useEncryption)
           {
            // when read header, checks passoword
            if (pageID == 0)
            {
               // I know, header page will be double read (it's the price for isolated concerns)
               var header = (HeaderPage)BasePage.ReadPage(buffer);

               if (header.DbParams.Password.BinaryCompareTo(_password) != 0)
               {
                  throw LiteException.DatabaseWrongPassword();
               }

               return buffer;
            }

            return _crypto.Decrypt(buffer);
         }

            return buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public virtual void WritePage(uint pageID, byte[] buffer)
        {
           if (_useEncryption)
           {
              buffer = pageID == 0 ? buffer : _crypto.Encrypt(buffer);
           }

           var position = BasePage.GetSizeOfPages(pageID);

            _log.Write(Logger.DISK, "write page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
        }

        /// <summary>
        /// Set datafile length
        /// </summary>
        public void SetLength(long fileSize)
        {
            // checks if new fileSize will exceed limit size
            if (_limitSize > 0 && fileSize > _limitSize) throw LiteException.FileSizeExceeds(_limitSize);

            // fileSize parameter tell me final size of data file - helpful to extend first datafile
            _stream.SetLength(fileSize);
        }

#endregion Read/Write

#region Journal file

        public void WriteJournal(uint pageID, byte[] buffer)
        {
            if (_journalEnabled == false) return;

            // test if this page is not in journal file
            if (_journalPages.Contains(pageID)) return;

            // open journal file if not used yet
            if (_journal == null)
            {
                // open journal file in EXCLUSIVE mode
                this.TryExec(() =>
                {
                    _log.Write(Logger.JOURNAL, "create journal file");

                   _journal = CreateStream(_journalFilename);
                });
            }

            _log.Write(Logger.JOURNAL, "write page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            // just write original bytes in order that are changed
            _journal.Write(buffer, 0, BasePage.PAGE_SIZE);

            _journalPages.Add(pageID);
        }

        public void DeleteJournal()
        {
            if (_journalEnabled == false) return;

            if (_journal != null)
            {
                _log.Write(Logger.JOURNAL, "delete journal file");

                // clear pages in journal file
                _journalPages.Clear();

                // close journal stream and delete file
                _journal.Dispose();
                _journal = null;

                // remove journal file
                DeleteFile(_journalFilename);
            }
        }

        public virtual void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
            }

           if (_crypto != null)
           {
              _crypto.Dispose();
           }
        }

#endregion Journal file

#region Recovery datafile

      protected abstract bool FileExists(string filename);

        private void TryRecovery()
        {
            if (!_journalEnabled) return;

            // avoid debug window always throw an exception if file didn't exists

           if (!FileExists(_journalFilename)) return;

            // if I can open journal file, test FINISH_POSITION. If no journal, do not call action()
            this.OpenExclusiveFile(_journalFilename, (journal) =>
            {
                _log.Write(Logger.RECOVERY, "journal file detected");

                // copy journal pages to datafile
                this.Recovery(journal);

                // close stream for delete file
                journal.Dispose();

               // delete journal - datafile finish

               DeleteFile(_journalFilename);

                _log.Write(Logger.RECOVERY, "recovery finish");
            });
        }

        private void Recovery(Stream journal)
        {
            var fileSize = _stream.Length;
            var buffer = new byte[BasePage.PAGE_SIZE];

            journal.Seek(0, SeekOrigin.Begin);

            while (journal.Position < journal.Length)
            {
                // read page bytes from journal file
                journal.Read(buffer, 0, BasePage.PAGE_SIZE);

                // read pageID (first 4 bytes)
                var pageID = BitConverter.ToUInt32(buffer, 0);

                _log.Write(Logger.RECOVERY, "recover page #{0:0000}", pageID);

                // if header, read all byte (to get original filesize)
                if (pageID == 0)
                {
                    var header = (HeaderPage)BasePage.ReadPage(buffer);

                    fileSize = BasePage.GetSizeOfPages(header.LastPageID + 1);
                }

                // write in stream
                this.WritePage(pageID, buffer);
            }

            _log.Write(Logger.RECOVERY, "resize datafile to {0} bytes", fileSize);

            // redim filesize if grow more than original before rollback
            _stream.SetLength(fileSize);
        }

#endregion Recovery datafile

#region Temporary

      protected abstract FileDiskServiceBase CreateFileDiskService(ConnectionString connectionString, Logger log);

        public IDiskService GetTempDisk()
        {
            // if exists, delete first
            this.DeleteTempDisk();

            // no journal, no logger
            return CreateFileDiskService(new ConnectionString("filename=" + _tempFilename + ";journal=false"), new Logger());
        }

      protected abstract void DeleteFile(string filepath);

        public void DeleteTempDisk()
        {
         DeleteFile(_tempFilename);
        }

#endregion Temporary

#region Utils

      protected abstract void WaitFor(int milliseconds);

        /// <summary>
        /// Try run an operation over datafile - keep tring if locked
        /// </summary>
        protected void TryExec(Action action)
        {
            var timer = DateTime.UtcNow.Add(_timeout);

            while (DateTime.UtcNow < timer)
            {
                try
                {
                    action();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                   WaitFor(250);
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(250);
                }
            }

            _log.Write(Logger.ERROR, "timeout disk access after {0}", _timeout);

            throw LiteException.LockTimeout(_timeout);
        }

      protected abstract void OpenExclusiveFile(string filename, Action<Stream> success);

#endregion Utils
   }
}