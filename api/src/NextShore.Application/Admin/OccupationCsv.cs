using System.Globalization;
using System.Text;
using NextShore.Domain;

namespace NextShore.Application.Admin;

/// <summary>
/// Occupations import: a header row with <c>code,title,is_eligible</c> and optional <c>source_url,verified_at</c>
/// (empty source_url = the pathway's). RFC 4180 quoting, so titles may contain commas.
/// </summary>
public static class OccupationCsv
{
    public const int MaxRows = 2000;
    private static readonly string[] Required = ["code", "title", "is_eligible"];

    public static Result<IReadOnlyList<OccupationRow>> Parse(string csv)
    {
        var lines = ReadRecords(csv).Where(r => r.Fields.Any(f => f.Length > 0)).ToList();
        if (lines.Count == 0)
        {
            return Fail("csv", "Empty file: expected a header row code,title,is_eligible[,source_url,verified_at].");
        }

        var header = lines[0].Fields.Select(h => h.Trim().ToLowerInvariant()).ToList();
        var missing = Required.Where(r => !header.Contains(r)).ToList();
        if (missing.Count > 0)
        {
            return Fail("csv", $"Header is missing: {string.Join(", ", missing)}.");
        }

        if (lines.Count - 1 > MaxRows)
        {
            return Fail("csv", $"At most {MaxRows} rows per import.");
        }

        int Col(string name) => header.IndexOf(name);
        var errors = new Dictionary<string, string[]>();
        var rows = new List<OccupationRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (line, fields) in lines.Skip(1))
        {
            string Get(string name) => Col(name) is var i and >= 0 && i < fields.Count ? fields[i].Trim() : "";
            var problems = new List<string>();

            var code = Get("code");
            var title = Get("title");
            if (code.Length == 0) problems.Add("code is empty");
            else if (!seen.Add(code)) problems.Add($"code {code} appears twice");
            if (title.Length == 0) problems.Add("title is empty");

            bool? eligible = Get("is_eligible").ToLowerInvariant() switch
            {
                "true" or "yes" or "1" => true,
                "false" or "no" or "0" => false,
                _ => null,
            };
            if (eligible is null) problems.Add("is_eligible must be true or false");

            var source = Get("source_url");
            if (source.Length > 0 && !AdminRules.IsHttpUrl(source)) problems.Add("source_url must be an absolute http(s) URL");

            DateOnly? verified = null;
            var verifiedText = Get("verified_at");
            if (verifiedText.Length > 0)
            {
                if (DateOnly.TryParseExact(verifiedText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) verified = d;
                else problems.Add("verified_at must be yyyy-MM-dd");
            }

            if (problems.Count > 0)
            {
                errors[$"line {line}"] = problems.ToArray();
                continue;
            }

            rows.Add(new OccupationRow(line, code, title, eligible!.Value, source.Length > 0 ? source : null, verified));
        }

        return errors.Count > 0 ? Error.Validation(errors) : rows;
    }

    private static Result<IReadOnlyList<OccupationRow>> Fail(string key, string message) =>
        Error.Validation(new Dictionary<string, string[]> { [key] = [message] });

    /// <summary>Splits CSV text into records (line number of the record start + fields).</summary>
    private static IEnumerable<(int Line, List<string> Fields)> ReadRecords(string csv)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var line = 1;
        var recordLine = 1;
        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else { if (c == '\n') line++; field.Append(c); }
                continue;
            }

            switch (c)
            {
                case '"' when field.Length == 0:
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    yield return (recordLine, fields);
                    fields = [];
                    field.Clear();
                    recordLine = ++line;
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            yield return (recordLine, fields);
        }
    }
}
