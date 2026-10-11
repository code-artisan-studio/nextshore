namespace NextShore.Infrastructure.Seed;

/// <summary>A seed file is malformed. The message names the file and the offending path.</summary>
public sealed class SeedException(string message, Exception? inner = null) : Exception(message, inner);
