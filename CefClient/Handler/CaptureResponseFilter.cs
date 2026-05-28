

using CefSharp;
using System.IO;
using System.Threading;

namespace CefClient.Handler
{
    public sealed class CaptureResponseFilter : IResponseFilter
    {
        private readonly MemoryStream _captureStream;

        private readonly long _maxBytes;

        private bool _overflow;

        private long _capturedLength;

        public ulong RequestId { get; private set; }

        public string Url { get; private set; }

        public long Length
        {
            get
            {
                return Interlocked.Read(ref _capturedLength);
            }
        }

        public CaptureResponseFilter(
            ulong requestId,
            string url,
            long maxBytes)
        {
            RequestId = requestId;
            Url = url;

            _maxBytes =
                maxBytes <= 0
                    ? 10 * 1024 * 1024
                    : maxBytes;

            _captureStream = new MemoryStream();
        }

        public bool InitFilter()
        {
            return true;
        }

        public FilterStatus Filter(
            Stream dataIn,
            out long dataInRead,
            Stream dataOut,
            out long dataOutWritten)
        {
            dataInRead = 0;
            dataOutWritten = 0;

            if (dataIn == null)
            {
                return FilterStatus.Done;
            }

            var buffer = new byte[32 * 1024];

            int read;

            while ((read = dataIn.Read(buffer, 0, buffer.Length)) > 0)
            {
                dataOut.Write(buffer, 0, read);

                dataInRead += read;
                dataOutWritten += read;

                Interlocked.Add(ref _capturedLength, read);

                if (!_overflow)
                {
                    if (_captureStream.Length + read <= _maxBytes)
                    {
                        _captureStream.Write(buffer, 0, read);
                    }
                    else
                    {
                        _overflow = true;
                    }
                }
            }

            return FilterStatus.Done;
        }

        public byte[] ToArray()
        {
            if (_overflow)
                return null;

            try
            {
                return _captureStream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_captureStream != null)
                {
                    _captureStream.Dispose();
                }
            }
            catch
            {
            }
        }
    }
}
