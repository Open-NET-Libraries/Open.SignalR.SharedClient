namespace Open.SignalR.SharedClient;

/// <summary>
/// A validated URI object or URL string.
/// </summary>
internal readonly struct UriOrUrlString : IEquatable<UriOrUrlString>
{
	/// <summary>
	/// Creates a <see cref="UriOrUrlString"/> from the provided <paramref name="uri"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException">If <paramref name="uri"/> is null.</exception>
	public UriOrUrlString(Uri uri)
	{
		Uri = uri ?? throw new ArgumentNullException(nameof(uri));
		UrlString = uri.ToString();
	}

	/// <summary>
	/// Creates a <see cref="UriOrUrlString"/> from the provided <paramref name="url"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException">If <paramref name="url"/> is null.</exception>
	public UriOrUrlString([StringSyntax(StringSyntaxAttribute.Uri)] string url)
	{
		UrlString = url ?? throw new ArgumentNullException(nameof(url));
		ArgumentException.ThrowIfNullOrWhiteSpace(url);
		Uri = new(url);
	}

	/// <summary>
	/// The validated <see cref="Uri"/>.
	/// </summary>
	public Uri Uri { get; }

	/// <summary>
	/// The uri as a string.
	/// </summary>
	public string UrlString { get; }

	/// <summary>
	/// Implicitly converts to a <see cref="string"/>.
	/// </summary>
	public static implicit operator string(UriOrUrlString source) => source.UrlString;

	/// <summary>
	/// Implicitly converts to a <see cref="System.Uri"/>.
	/// </summary>
	public static implicit operator Uri(UriOrUrlString source) => source.Uri;

	/// <inheritdoc />
	public override string ToString() => UrlString;

	/// <inheritdoc />
	public override bool Equals(
		[NotNullWhen(true)] object? obj)
		=> obj is UriOrUrlString u && Equals(u);

	/// <inheritdoc />
	public bool Equals(UriOrUrlString other) => UrlString == other.UrlString;

	/// <inheritdoc />
	public override int GetHashCode()
		=> UrlString.GetHashCode();

	/// <inheritdoc />
	public static bool operator ==(UriOrUrlString left, UriOrUrlString right)
		=> left.Equals(right);

	/// <inheritdoc />
	public static bool operator !=(UriOrUrlString left, UriOrUrlString right)
		=> !(left == right);
}
