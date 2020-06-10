using System;
using System.Buffers;
using System.Text;
using Lane.Memcached.Request;

namespace Lane.Memcached.Response
{
    public struct StringResponseDeserializer: IResponseDeserializer<string>
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        public string Deserialize(MemcachedResponse response)
        {
            if (response.Header.ResponseStatus == Enums.ResponseStatus.NoError)
            {
                var sequence = response.Data;
                if (sequence.IsSingleSegment)
                {
                    var span = sequence.FirstSpan;
                    return span.IsEmpty ? string.Empty : encoding.GetString(span);
                }
                else
                {
                    return GetString(in sequence);
                }
            }

            return default(MemcachedErrorHandler).HandleError<string>(response);
        }

        private static string GetString(in ReadOnlySequence<byte> buffer)
        {
            var decoder = encoding.GetDecoder();
            var charCount = GetCharCount(in buffer, encoding, in decoder);
            return string.Create(charCount, (buffer, decoder), (chars, tuple) =>
            {
                var (buffer, decoder) = tuple;
                if (chars.IsEmpty)
                {
                    return;
                }
                decoder.Reset();

                int totalBytes = 0;
                bool isComplete = true;
                foreach (var segment in buffer)
                {
                    var bytes = segment.Span;
                    if (bytes.IsEmpty) continue;
                    decoder.Convert(bytes, chars, false, out var bytesUsed, out var charsUsed, out isComplete);
                    totalBytes += bytesUsed;
                    chars = chars.Slice(charsUsed);
                    if (chars.IsEmpty) break;
                }
                if (!isComplete || totalBytes != buffer.Length)
                {
                    throw new InvalidOperationException("Incomplete decoding frame");
                }
            });
        }

        private static int GetCharCount(in ReadOnlySequence<byte> buffer, Encoding encoding, in Decoder decoder)
        {
            checked
            {
                if (encoding.IsSingleByte) return (int)buffer.Length;
                if (encoding is UnicodeEncoding) return (int)(buffer.Length >> 1);
                if (encoding is UTF32Encoding) return (int)(buffer.Length >> 2);
            }

            if (buffer.IsSingleSegment)
            {
                var span = buffer.First.Span;
                return span.IsEmpty ? 0 : encoding.GetCharCount(span);
            }

            int charCount = 0;
            decoder.Reset();
            foreach (var segment in buffer)
            {
                var span = segment.Span;
                if (span.IsEmpty) continue;
                charCount += decoder.GetCharCount(span, false);
            }
            return charCount;

        }
    }
}