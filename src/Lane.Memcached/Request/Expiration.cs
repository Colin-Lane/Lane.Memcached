using System;

namespace Lane.Memcached.Request
{
    public readonly struct Expiration
    {
        public readonly uint Value { get; }

        private Expiration(uint value)
        {
            Value = value;
        }

        public static implicit operator Expiration(TimeSpan? expireIn) => SetExpiration(expireIn);

        private static Expiration SetExpiration(TimeSpan? expireIn)
        {
            uint value = 0;
            if (expireIn != null)
            {
                if (expireIn < TimeSpan.FromDays(30))
                {
                    value = (uint)expireIn.Value.TotalSeconds;
                }
                else
                {
                    value = (uint)new DateTimeOffset(DateTime.UtcNow.Add(expireIn.Value)).ToUnixTimeSeconds();
                }                    
            }
            return new Expiration(value);
        }
    }
}
