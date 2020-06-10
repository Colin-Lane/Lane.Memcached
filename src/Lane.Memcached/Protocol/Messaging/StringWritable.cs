using System;
using System.Text;

namespace Lane.Memcached.Protocol.Messaging
{
    public class StringWritable: IMemoryWritable
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        private readonly string _key;
        public int Length { get; }

        public StringWritable(string key)
        {
            _key = key;
            Length = Encoding.GetByteCount(_key);
        }

        public void CopyTo(Span<byte> destination)
        {
            Encoding.GetBytes(_key, destination);
        }
    }
}