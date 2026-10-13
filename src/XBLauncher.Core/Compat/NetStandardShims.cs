// Shims that let modern C# language features compile against netstandard2.0.
// These are no-ops at runtime; they only satisfy the compiler.

// Enables init-only setters (used by object initializers with `required`/`init`).
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

// Enables `required` members on types under netstandard2.0.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;
        public string FeatureName { get; }
        public bool IsOptional { get; set; }
        public const string RefStructs = nameof(RefStructs);
        public const string RequiredMembers = nameof(RequiredMembers);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
    }
}

// ---- netstandard2.0 API shims ------------------------------------------------
// The Core code uses a handful of APIs that only exist on .NET 5+/netstandard2.1.
// These extensions/aliases bring them to netstandard2.0 so the same sources build
// for both the net10 WPF app and the UWP (.NET Standard) app.

namespace System.Net.Http
{
    // netstandard2.0 has no SocketsHttpHandler; the default HttpClientHandler is fine.
    internal sealed class SocketsHttpHandler : HttpClientHandler
    {
        public SocketsHttpHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        public TimeSpan PooledConnectionLifetime { get; set; }
    }

    internal static class HttpContentExtensions
    {
        public static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken ct) =>
            content.ReadAsStreamAsync();

        public static Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken ct) =>
            content.ReadAsStringAsync();
    }

    internal static class StreamExtensions
    {
        public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken ct) =>
            stream.WriteAsync(buffer, 0, buffer.Length, ct);
    }
}

namespace System.IO.Compression
{
    internal static class ZipFileExtensions
    {
        // netstandard2.0 ZipFile lacks the overwriteFiles overload.
        public static void ExtractToDirectoryOverwrite(string zipPath, string dest)
        {
            Directory.CreateDirectory(dest);
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var outPath = Path.Combine(dest, entry.FullName);
                    var outDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outDir))
                        Directory.CreateDirectory(outDir);
                    if (string.IsNullOrEmpty(entry.Name))
                        continue; // directory entry
                    entry.ExtractToFile(outPath, overwrite: true);
                }
            }
        }
    }
}

namespace System.Diagnostics
{
    // netstandard2.0's ProcessStartInfo has no ArgumentList. CommandBuilder
    // accumulates quoted arguments into the Arguments string, matching the
    // ArgumentList.Add(...) call sites in the Core sources.
    internal sealed class CommandArguments
    {
        private readonly ProcessStartInfo _psi;
        public CommandArguments(ProcessStartInfo psi) => _psi = psi;

        public void Add(string arg)
        {
            if (_psi.Arguments.Length > 0)
                _psi.Arguments += " ";
            _psi.Arguments += Quote(arg);
        }

        private static string Quote(string arg)
        {
            if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                return arg;
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        }
    }

    internal static class ProcessExtensions
    {
        public static async Task WaitForExitAsync(this Process process, CancellationToken ct)
        {
            while (!process.HasExited)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }
    }

    internal static class StreamReaderExtensions
    {
        public static Task<string> ReadToEndAsync(this System.IO.StreamReader reader, CancellationToken ct) =>
            reader.ReadToEndAsync();
    }
}

namespace System.Collections.Generic
{
    internal static class KeyValuePairDeconstruction
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair,
            out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}

namespace System.IO
{
    internal static class StreamCopyExtensions
    {
        public static Task CopyToAsync(this Stream source, Stream destination, CancellationToken ct) =>
            source.CopyToAsync(destination, 81920, ct);
    }
}