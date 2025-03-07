﻿namespace System.IO.Abstractions.TestingHelpers
{
    /// <inheritdoc />
    [Serializable]
    public class MockFileStream : MemoryStream
    {
        private readonly IMockFileDataAccessor mockFileDataAccessor;
        private readonly string path;
        private readonly FileAccess access = FileAccess.ReadWrite;
        private readonly FileOptions options;
        private readonly MockFileData fileData;
        private bool disposed;

        /// <inheritdoc />
        public MockFileStream(
                  IMockFileDataAccessor mockFileDataAccessor,
                  string path,
                  FileMode mode,
                  FileAccess access = FileAccess.ReadWrite,
                  FileOptions options = FileOptions.None)

        {
            this.mockFileDataAccessor = mockFileDataAccessor ?? throw new ArgumentNullException(nameof(mockFileDataAccessor));
            this.path = path;
            this.options = options;

            if (mockFileDataAccessor.FileExists(path))
            {
                if (mode.Equals(FileMode.CreateNew))
                {
                    throw CommonExceptions.FileAlreadyExists(path);
                }

                fileData = mockFileDataAccessor.GetFile(path);
                fileData.CheckFileAccess(path, access);

                var timeAdjustments = GetTimeAdjustmentsForFileStreamWhenFileExists(mode, access);
                mockFileDataAccessor.AdjustTimes(fileData, timeAdjustments);
                var existingContents = fileData.Contents;
                var keepExistingContents =
                    existingContents?.Length > 0 &&
                    mode != FileMode.Truncate && mode != FileMode.Create;
                if (keepExistingContents)
                {
                    base.Write(existingContents, 0, existingContents.Length);
                    base.Seek(0, mode == FileMode.Append
                        ? SeekOrigin.End
                        : SeekOrigin.Begin);
                }
            }
            else
            {
                var directoryPath = mockFileDataAccessor.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directoryPath) && !mockFileDataAccessor.Directory.Exists(directoryPath))
                {
                    throw CommonExceptions.CouldNotFindPartOfPath(path);
                }

                if (mode.Equals(FileMode.Open) || mode.Equals(FileMode.Truncate))
                {
                    throw CommonExceptions.FileNotFound(path);
                }

                fileData = new MockFileData(new byte[] { });
                mockFileDataAccessor.AdjustTimes(fileData,
                    TimeAdjustments.CreationTime | TimeAdjustments.LastAccessTime);
                mockFileDataAccessor.AddFile(path, fileData);
            }

            this.access = access;
        }

        /// <inheritdoc />
        public override bool CanRead => access.HasFlag(FileAccess.Read);

        /// <inheritdoc />
        public override bool CanWrite => access.HasFlag(FileAccess.Write);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            mockFileDataAccessor.AdjustTimes(fileData,
                TimeAdjustments.LastAccessTime);
            return base.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            mockFileDataAccessor.AdjustTimes(fileData,
                TimeAdjustments.LastAccessTime | TimeAdjustments.LastWriteTime);
            base.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            InternalFlush();
            base.Dispose(disposing);
            OnClose();
            disposed = true;
        }

        /// <inheritdoc />
        public override void Flush()
        {
            InternalFlush();
        }

        private void InternalFlush()
        {
            if (mockFileDataAccessor.FileExists(path))
            {
                var mockFileData = mockFileDataAccessor.GetFile(path);
                /* reset back to the beginning .. */
                var position = Position;
                Seek(0, SeekOrigin.Begin);
                /* .. read everything out */
                var data = new byte[Length];
                Read(data, 0, (int)Length);
                /* restore to original position */
                Seek(position, SeekOrigin.Begin);
                /* .. put it in the mock system */
                mockFileData.Contents = data;
            }
        }

        private void OnClose()
        {
            if (options.HasFlag(FileOptions.DeleteOnClose) && mockFileDataAccessor.FileExists(path))
            {
                mockFileDataAccessor.RemoveFile(path);
            }

            if (options.HasFlag(FileOptions.Encrypted) && mockFileDataAccessor.FileExists(path))
            {
                mockFileDataAccessor.FileInfo.FromFileName(path).Encrypt();
            }
        }

        private TimeAdjustments GetTimeAdjustmentsForFileStreamWhenFileExists(FileMode mode, FileAccess access)
        {
            switch (mode)
            {
                case FileMode.Append:
                case FileMode.CreateNew:
                    if (access.HasFlag(FileAccess.Read))
                    {
                        return TimeAdjustments.LastAccessTime;
                    }
                    return TimeAdjustments.None;
                case FileMode.Create:
                case FileMode.Truncate:
                    if (access.HasFlag(FileAccess.Write))
                    {
                        return TimeAdjustments.LastAccessTime | TimeAdjustments.LastWriteTime;
                    }
                    return TimeAdjustments.LastAccessTime;
                case FileMode.Open:
                case FileMode.OpenOrCreate:
                default:
                    return TimeAdjustments.None;
            }
        }
    }
}