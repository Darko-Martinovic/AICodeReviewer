using System.Globalization;
using System.Text;

namespace Content.Urls;

public static class SlugGenerator
{
    public const int MaxLength = 80;

    /// <summary>
    /// Lower-case ASCII letters and digits joined by single hyphens, at most MaxLength characters
    /// </summary>
    public static string ToSlug(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var normalized = title.Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(Math.Min(normalized.Length, MaxLength));
        var pendingHyphen = false;

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingHyphen && slug.Length > 0)
                {
                    slug.Append('-');
                }
                pendingHyphen = false;
                slug.Append(char.ToLowerInvariant(c));
            }
            else
            {
                pendingHyphen = true;
            }

            if (slug.Length >= MaxLength)
            {
                break;
            }
        }

        return slug.ToString(0, Math.Min(slug.Length, MaxLength)).TrimEnd('-');
    }
}
