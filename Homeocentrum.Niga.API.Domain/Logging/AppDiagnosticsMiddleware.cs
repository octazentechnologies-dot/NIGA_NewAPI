using System.Collections.Generic;
using System.Diagnostics;
using Homeocentrum.Niga.API.Domain.Configuration;
using Microsoft.AspNetCore.Http;

namespace Homeocentrum.Niga.API.Domain.Logging
{
    /// <summary>Logs unhandled exceptions and API responses with status &gt;= 400 into Logs/.</summary>
    public sealed class AppDiagnosticsMiddleware
    {
        private readonly RequestDelegate _next;

        public AppDiagnosticsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            ApplyCorrelation(context);
            AppFileLog.SetRequestSnapshot(RequestDetails(context, null, 0));
            var flags = FeatureFlags.Current;
            var requestPath = context.Request.Path.Value ?? "";
            var isApi = requestPath.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
            var requestBody = flags.EnableRequestLogging && isApi ? await ReadRequestBodyAsync(context.Request) : null;
            BodyCaptureStream? capture = null;
            var originalBody = context.Response.Body;
            if (flags.EnableResponseLogging && isApi)
            {
                capture = new BodyCaptureStream(originalBody, BodyLogLimit);
                context.Response.Body = capture;
            }
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();
                var status = context.Response?.StatusCode ?? 0;
                var path = requestPath;
                if (IsSkipped(path))
                    return;
                if (requestBody != null)
                {
                    AppFileLog.Write("api", "INFO", "HttpRequest",
                        $"Request {context.Request.Method} {path} body={requestBody} trace={context.TraceIdentifier}");
                }
                if (status >= 400)
                {
                    var level = status >= 500 && status != 503 ? "ERROR" : "WARN";
                    var kind = level == "ERROR" ? "errors" : "api";
                    AppFileLog.Write(kind, level, "Http",
                        $"{status} {context.Request.Method} {path}{context.Request.QueryString} user={context.User?.Identity?.Name} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                        details: RequestDetails(context, status, sw.ElapsedMilliseconds),
                        sendAlert: level == "ERROR");
                }
                else if (isApi)
                {
                    var slow = AppFileLog.SlowRequestMilliseconds;
                    var isSlow = slow > 0 && sw.ElapsedMilliseconds >= slow;
                    if (isSlow || flags.EnableRequestLogging)
                    {
                        AppFileLog.Write(isSlow ? "perf" : "api", isSlow ? "WARN" : "INFO", isSlow ? "Performance" : "Http",
                            $"{status} {context.Request.Method} {path} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                            details: RequestDetails(context, status, sw.ElapsedMilliseconds),
                            sendAlert: isSlow);
                    }
                }
                if (capture != null && IsTextual(context.Response?.ContentType))
                {
                    AppFileLog.Write("api", "INFO", "HttpResponse",
                        $"Response {status} {context.Request.Method} {path} body={capture.Captured()} trace={context.TraceIdentifier}");
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var path = context.Request.Path.Value ?? "";
                var errorId = Errors.SafeError.NewErrorId();
                var details = RequestDetails(context, 500, sw.ElapsedMilliseconds);
                details["ErrorId"] = errorId;
                AppFileLog.Write("errors", "ERROR", "Http",
                    $"UNHANDLED errorId={errorId} {context.Request.Method} {path}{context.Request.QueryString} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                    ex,
                    details);
                await WriteSafeErrorAsync(context, errorId, ex);
            }
            finally
            {
                if (capture != null)
                    context.Response.Body = originalBody;
                AppFileLog.ClearRequestSnapshot();
            }
        }

        private const int BodyLogLimit = 4000;

        private static bool IsTextual(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return false;
            return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>First 4,000 characters of a JSON/text body for POST/PUT/PATCH/DELETE; null when there is nothing to log.</summary>
        private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
                return null;
            if (!IsTextual(request.ContentType) || request.ContentLength == 0)
                return null;
            request.EnableBuffering();
            var buffer = new char[BodyLogLimit];
            int read;
            using (var reader = new System.IO.StreamReader(request.Body, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true))
                read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;
            if (read == 0) return null;
            var text = new string(buffer, 0, read);
            return read == BodyLogLimit ? text + "…(truncated)" : text;
        }

        /// <summary>Passes every write through to the real response and keeps a copy of the first bytes for the log.</summary>
        private sealed class BodyCaptureStream : System.IO.Stream
        {
            private readonly System.IO.Stream _inner;
            private readonly System.IO.MemoryStream _copy = new();
            private readonly int _limit;
            private long _total;

            public BodyCaptureStream(System.IO.Stream inner, int limit)
            {
                _inner = inner;
                _limit = limit;
            }

            public string Captured()
            {
                var text = System.Text.Encoding.UTF8.GetString(_copy.GetBuffer(), 0, (int)_copy.Length);
                return _total > _copy.Length ? text + "…(truncated)" : text;
            }

            private void Keep(ReadOnlySpan<byte> data)
            {
                _total += data.Length;
                var room = _limit - (int)_copy.Length;
                if (room > 0)
                    _copy.Write(data[..Math.Min(room, data.Length)]);
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                Keep(buffer.AsSpan(offset, count));
                _inner.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                Keep(buffer.AsSpan(offset, count));
                return _inner.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Keep(buffer.Span);
                return _inner.WriteAsync(buffer, cancellationToken);
            }
        }

        private static async Task WriteSafeErrorAsync(HttpContext context, string errorId, Exception ex)
        {
            if (context.Response.HasStarted)
                return;
            context.Response.Clear();
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var body = new Errors.SafeErrorBody
            {
                ErrorId = errorId, TraceId = context.TraceIdentifier, Exception = Errors.SafeError.Expose(ex, "Http"),
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
        }

        private static void ApplyCorrelation(HttpContext context)
        {
            var incoming = context.Request.Headers["X-Correlation-Id"].ToString();
            if (IsSafeTrace(incoming))
                context.TraceIdentifier = incoming.Trim();
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier ?? "";
                return Task.CompletedTask;
            });
        }

        private static bool IsSafeTrace(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 80) return false;
            foreach (var ch in value.Trim())
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')) return false;
            }
            return true;
        }

        private static bool IsSkipped(string path)
        {
            return path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/json/version", StringComparison.OrdinalIgnoreCase)
                || path.IndexOf("/Diagnostics/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static Dictionary<string, string> RequestDetails(HttpContext context, int? status, long elapsedMs)
        {
            var details = AppFileLog.BaseDetails();
            details["TraceId"] = context.TraceIdentifier ?? "";
            details["Method"] = context.Request.Method ?? "";
            details["Path"] = (context.Request.Path.Value ?? "") + context.Request.QueryString;
            details["Status"] = status?.ToString() ?? "";
            details["ElapsedMs"] = elapsedMs.ToString();
            details["OccurredAtLocal"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            details["OccurredAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Z";
            details["ContentType"] = context.Request.ContentType ?? "";
            ClientEnvironment.ApplyUser(details, context.User);
            ClientEnvironment.ApplyRequestPlace(details, context);
            ClientEnvironment.ApplyUserAgent(details, context.Request.Headers["User-Agent"].ToString());
            return details;
        }
    }
}
