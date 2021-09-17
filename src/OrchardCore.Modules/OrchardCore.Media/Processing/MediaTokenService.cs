using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Processors;

namespace OrchardCore.Media.Processing
{
    public class MediaTokenService : IMediaTokenService
    {
        private const string TokenCacheKeyPrefix = "MediaToken:";
        private readonly IMemoryCache _memoryCache;

        private readonly HashSet<string> _knownCommands = new HashSet<string>();
        private readonly byte[] _hashKey;

        public MediaTokenService(
            IMemoryCache memoryCache,
            IOptions<MediaTokenOptions> options,
            IEnumerable<IImageWebProcessor> processors)
        {
            _memoryCache = memoryCache;
            foreach (var processor in processors)
            {
                foreach (var command in processor.Commands)
                {
                    _knownCommands.Add(command);
                }
            }

            _hashKey = options.Value.HashKey;
        }

        public string AddTokenToPath(string path)
        {
            var pathIndex = path.IndexOf('?');

            var parsed = pathIndex != -1
                ? QueryHelpers.ParseQuery(path.Substring(pathIndex + 1))
                : null;

            // If no commands or only a version command don't bother tokenizing.
            if (parsed is null || parsed.Count == 0 || parsed.Count == 1 && parsed.ContainsKey(ImageVersionProcessor.VersionCommand))
            {
                return path;
            }

            var processingCommands = new Dictionary<string, string>(parsed.Count);
            Dictionary<string, string> otherCommands = null;

            foreach (var command in parsed)
            {
                if (_knownCommands.Contains(command.Key))
                {
                    processingCommands[command.Key] = command.Value.ToString();
                }
                else
                {
                    otherCommands ??= new Dictionary<string, string>();
                    otherCommands[command.Key] = command.Value.ToString();
                }
            }

            // Using the command values as a key retrieve from cache
            var queryStringTokenKey = CreateQueryStringTokenKey(processingCommands.Values);
            var queryStringToken = GetHash(queryStringTokenKey);

            processingCommands["token"] = queryStringToken;

            // If any non-resizing parameters have been added to the path include these on the query string output.
            if (otherCommands != null)
            {
                foreach (var command in otherCommands)
                {
                    processingCommands.Add(command.Key, command.Value);
                }
            }

            return QueryHelpers.AddQueryString(path.Substring(0, pathIndex), processingCommands);
        }

        public bool TryValidateToken(IDictionary<string, string> commands, string token)
        {
            var queryStringTokenKey = CreateQueryStringTokenKey(commands.Values);

            // Store a hash of the valid query string commands.
            var queryStringToken = GetHash(queryStringTokenKey);

            if (String.Equals(queryStringToken, token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static string CreateQueryStringTokenKey(IEnumerable<string> values)
        {
            using var builder = ZString.CreateStringBuilder();
            builder.Append(TokenCacheKeyPrefix);
            foreach (var item in values)
            {
                builder.Append(item);
            }
            return builder.ToString();
        }

        private string GetHash(string queryStringTokenKey)
            => _memoryCache.GetOrCreate(queryStringTokenKey, entry =>
               {
                   entry.SlidingExpiration = TimeSpan.FromHours(5);

                   using var hmac = new HMACSHA256(_hashKey);

                   // queryStringTokenKey also contains prefix
                   var bytes = Encoding.UTF8.GetBytes(queryStringTokenKey, TokenCacheKeyPrefix.Length, queryStringTokenKey.Length - TokenCacheKeyPrefix.Length);

                   var hash = hmac.ComputeHash(bytes);

                   return Convert.ToBase64String(hash);
               });
    }
}
