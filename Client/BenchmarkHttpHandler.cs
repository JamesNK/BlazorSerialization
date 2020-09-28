using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class BenchmarkHttpHandler : DelegatingHandler
    {
        private readonly Stopwatch _stopwatch;
        private CaptureResponseLengthContent? _content;

        public TimeSpan? HeadersReceivedElapsed { get; private set; }
        public int? BytesRead => _content?.BytesRead;

        public BenchmarkHttpHandler(Stopwatch stopwatch, HttpMessageHandler inner) : base(inner)
        {
            _stopwatch = stopwatch;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HeadersReceivedElapsed = null;

            var response = await base.SendAsync(request, cancellationToken);

            _content = new CaptureResponseLengthContent(response.Content);
            response.Content = _content;

            HeadersReceivedElapsed = _stopwatch.Elapsed;

            return response;
        }

        private class CaptureResponseLengthContent : HttpContent
        {
            private readonly HttpContent _inner;
            private CaptureResponseLengthStream? _innerStream;

            public int? BytesRead => _innerStream?.BytesRead;

            public CaptureResponseLengthContent(HttpContent inner)
            {
                _inner = inner;

                foreach (var header in inner.Headers)
                {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                throw new NotImplementedException();
            }

            protected override async Task<Stream> CreateContentReadStreamAsync()
            {
                var stream = await _inner.ReadAsStreamAsync();
                _innerStream = new CaptureResponseLengthStream(stream);

                return _innerStream;
            }

            protected override bool TryComputeLength(out long length)
            {
                throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                    _innerStream?.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private class CaptureResponseLengthStream : Stream
        {
            private readonly Stream _inner;
            public int BytesRead { get; private set; }

            public CaptureResponseLengthStream(Stream inner)
            {
                _inner = inner;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position
            {
                get => _inner.Position;
                set => _inner.Position = value;
            }

            public override void Flush() => _inner.Flush();

            public override int Read(byte[] buffer, int offset, int count)
            {
                var readCount = _inner.Read(buffer, offset, count);
                BytesRead += readCount;
                return readCount;
            }

            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

            public override void SetLength(long value) => _inner.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var readCount = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
                BytesRead += readCount;
                return readCount;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                var readCount = await _inner.ReadAsync(buffer, cancellationToken);
                BytesRead += readCount;
                return readCount;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }
            }
        }
    }
}