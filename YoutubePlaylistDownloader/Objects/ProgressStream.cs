using System;
using System.IO;

namespace YoutubePlaylistDownloader.Objects
{
    /// <summary>
    /// Wraps another stream and provides reporting for when bytes are read or written to the stream.
    /// </summary>
    public class ProgressStream : Stream
    {
        #region Private Data Members
        private Stream innerStream;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new ProgressStream supplying the stream for it to report on.
        /// </summary>
        /// <param name="streamToReportOn">The underlying stream that will be reported on when bytes are read or written.</param>
        public ProgressStream(Stream streamToReportOn)
        {
            if (streamToReportOn != null)
            {
                this.innerStream = streamToReportOn;
            }
            else
            {
                throw new ArgumentNullException(nameof(streamToReportOn));
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when bytes are read from the stream.
        /// </summary>
        public event ProgressStreamReportEventHandler BytesRead;

        /// <summary>
        /// Raised when bytes are written to the stream.
        /// </summary>
        public event ProgressStreamReportEventHandler BytesWritten;

        /// <summary>
        /// Raised when bytes are either read or written to the stream.
        /// </summary>
        public event ProgressStreamReportEventHandler BytesMoved;

        protected virtual void OnBytesRead(int bytesMoved)
        {
            if (BytesRead != null)
            {
                var args = new ProgressStreamReportEventArgs(bytesMoved, innerStream.Length, innerStream.Position, true);
                BytesRead(this, args);
            }
        }

        protected virtual void OnBytesWritten(int bytesMoved) => BytesWritten?.Invoke(this, new ProgressStreamReportEventArgs(bytesMoved, innerStream.Length, innerStream.Position, false));


        protected virtual void OnBytesMoved(int bytesMoved, bool isRead) => BytesMoved?.Invoke(this, new ProgressStreamReportEventArgs(bytesMoved, innerStream.Length, innerStream.Position, isRead));
        
        #endregion

        #region Stream Members

        public override bool CanRead
        {
            get { return innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return innerStream.CanWrite; }
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override long Length
        {
            get { return innerStream.Length; }
        }

        public override long Position
        {
            get
            {
                return innerStream.Position;
            }
            set
            {
                innerStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = innerStream.Read(buffer, offset, count);

            OnBytesRead(bytesRead);
            OnBytesMoved(bytesRead, true);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);

            OnBytesWritten(count);
            OnBytesMoved(count, false);
        }

        public override void Close()
        {
            innerStream.Close();
            base.Close();
        }
        #endregion
    }

    /// <summary>
    /// Contains the pertinent data for a ProgressStream Report event.
    /// </summary>
    public class ProgressStreamReportEventArgs : EventArgs
    {
        /// <summary>
        /// The number of bytes that were read/written to/from the stream.
        /// </summary>
        public int BytesMoved { get; private set; }

        /// <summary>
        /// The total length of the stream in bytes.
        /// </summary>
        public long StreamLength { get; private set; }

        /// <summary>
        /// The current position in the stream.
        /// </summary>
        public long StreamPosition { get; private set; }

        /// <summary>
        /// True if the bytes were read from the stream, false if they were written.
        /// </summary>
        public bool WasRead { get; private set; }

        /// <summary>
        /// Default constructor for ProgressStreamReportEventArgs.
        /// </summary>
        public ProgressStreamReportEventArgs()
            : base() { }

        /// <summary>
        /// Creates a new ProgressStreamReportEventArgs initializing its members.
        /// </summary>
        /// <param name="bytesMoved">The number of bytes that were read/written to/from the stream.</param>
        /// <param name="streamLength">The total length of the stream in bytes.</param>
        /// <param name="streamPosition">The current position in the stream.</param>
        /// <param name="wasRead">True if the bytes were read from the stream, false if they were written.</param>
        public ProgressStreamReportEventArgs(int bytesMoved, long streamLength, long streamPosition, bool wasRead)
            : this()
        {
            this.BytesMoved = bytesMoved;
            this.StreamLength = streamLength;
            this.StreamPosition = streamPosition;
            this.WasRead = wasRead;
        }
    }

    /// <summary>
    /// The delegate for handling a ProgressStream Report event.
    /// </summary>
    /// <param name="sender">The object that raised the event, should be a ProgressStream.</param>
    /// <param name="args">The arguments raised with the event.</param>
    public delegate void ProgressStreamReportEventHandler(object sender, ProgressStreamReportEventArgs args);
}