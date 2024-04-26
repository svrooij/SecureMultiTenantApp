using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureMultiTenantApp.Extensions
{
    internal static class TokenModifier
    {
        /// <summary>
        /// Take a JWT and remove the last part (the signature)
        /// </summary>
        /// <param name="token">JWT, three parts devided by a .</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static string RemoveSignature(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Token must have 3 parts");
            }

            return $"{parts[0]}.{parts[1]}._you_re_kidding_right_";
        }
    }
}
