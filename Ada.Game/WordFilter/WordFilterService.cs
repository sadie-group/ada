using System.Text;
using System.Text.RegularExpressions;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Db;
using Ada.Db.Models.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Game.WordFilter;

public class WordFilterService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ILogger<WordFilterService> logger) : IWordFilterService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Dictionary<char, char> LeetMap = new()
    {
        ['0'] = 'o',
        ['1'] = 'i',
        ['3'] = 'e',
        ['4'] = 'a',
        ['5'] = 's',
        ['6'] = 'g',
        ['7'] = 't',
        ['8'] = 'b',
        ['9'] = 'g',
        ['@'] = 'a',
        ['$'] = 's',
        ['!'] = 'i',
        ['+'] = 't',
        ['|'] = 'i'
    };

    private readonly Lock _loadLock = new();
    private volatile List<CompiledEntry>? _entries;

    private record CompiledEntry(WordFilterEntry Entry, Regex Regex);

    public WordFilterResultDto Filter(string text, WordFilterContext context)
    {
        var entries = _entries ?? LoadEntries();

        WordFilterAction? action = null;
        var matchedPatterns = new List<string>();
        var spans = new List<(int Start, int Length, string Replacement)>();

        var (strippedText, strippedMap) = Normalize(text, stripSeparators: true);
        var (keptText, _) = Normalize(text, stripSeparators: false);

        foreach (var compiled in entries)
        {
            if ((compiled.Entry.Contexts & context) == 0)
            {
                continue;
            }
            
            var useStripped = compiled.Entry.NormalizeText &&
                              compiled.Entry.MatchTypeId != WordFilterMatchType.WholeWord;

            var haystack = compiled.Entry.NormalizeText
                ? useStripped ? strippedText : keptText
                : text;

            MatchCollection matches;

            try
            {
                matches = compiled.Regex.Matches(haystack);
            }
            catch (RegexMatchTimeoutException)
            {
                logger.LogWarning(
                    "Word filter pattern '{Pattern}' timed out, skipping",
                    compiled.Entry.Pattern);
                continue;
            }

            if (matches.Count == 0)
            {
                continue;
            }

            matchedPatterns.Add(compiled.Entry.Pattern);

            if (action == null || compiled.Entry.ActionId > action)
            {
                action = compiled.Entry.ActionId;
            }

            foreach (Match match in matches)
            {
                if (match.Length == 0)
                {
                    continue;
                }

                var replacement = compiled.Entry.Replacement ?? "bobba";

                if (useStripped)
                {
                    var start = strippedMap[match.Index];
                    var end = strippedMap[match.Index + match.Length - 1];
                    spans.Add((start, end - start + 1, replacement));
                }
                else
                {
                    spans.Add((match.Index, match.Length, replacement));
                }
            }
        }

        return new WordFilterResultDto
        {
            OriginalText = text,
            FilteredText = spans.Count == 0 ? text : ApplyReplacements(text, spans),
            Action = action,
            MatchedPatterns = matchedPatterns
        };
    }

    public async Task ReloadAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entries = await dbContext.WordFilterEntries
            .AsNoTracking()
            .Where(x => x.Enabled)
            .ToListAsync();

        lock (_loadLock)
        {
            _entries = Compile(entries);
        }
    }

    private List<CompiledEntry> LoadEntries()
    {
        lock (_loadLock)
        {
            if (_entries != null)
            {
                return _entries;
            }

            using var dbContext = dbContextFactory.CreateDbContext();

            _entries = Compile(dbContext.WordFilterEntries
                .AsNoTracking()
                .Where(x => x.Enabled)
                .ToList());

            return _entries;
        }
    }

    private List<CompiledEntry> Compile(List<WordFilterEntry> entries)
    {
        var compiled = new List<CompiledEntry>();

        foreach (var entry in entries)
        {
            var pattern = entry.MatchTypeId switch
            {
                WordFilterMatchType.Regex => entry.Pattern,
                WordFilterMatchType.WholeWord => $@"\b{Regex.Escape(NormalizePattern(entry))}\b",
                _ => Regex.Escape(NormalizePattern(entry))
            };

            try
            {
                compiled.Add(new CompiledEntry(
                    entry,
                    new Regex(
                        pattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                        RegexTimeout)));
            }
            catch (ArgumentException exception)
            {
                logger.LogWarning(
                    exception,
                    "Invalid word filter pattern '{Pattern}', skipping",
                    entry.Pattern);
            }
        }

        return compiled;
    }

    private static string NormalizePattern(WordFilterEntry entry)
    {
        return entry.NormalizeText
            ? Normalize(entry.Pattern, entry.MatchTypeId != WordFilterMatchType.WholeWord).Text
            : entry.Pattern;
    }
    
    private static (string Text, int[] Map) Normalize(string text, bool stripSeparators)
    {
        var builder = new StringBuilder(text.Length);
        var map = new List<int>(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var character = char.ToLowerInvariant(text[i]);

            if (LeetMap.TryGetValue(character, out var mapped))
            {
                character = mapped;
            }

            if (stripSeparators && !char.IsLetterOrDigit(character))
            {
                continue;
            }

            builder.Append(character);
            map.Add(i);
        }

        return (builder.ToString(), map.ToArray());
    }

    private static string ApplyReplacements(
        string text,
        List<(int Start, int Length, string Replacement)> spans)
    {
        var merged = new List<(int Start, int Length, string Replacement)>();

        foreach (var span in spans.OrderBy(x => x.Start).ThenByDescending(x => x.Length))
        {
            if (merged.Count > 0 &&
                span.Start < merged[^1].Start + merged[^1].Length)
            {
                var last = merged[^1];
                var end = Math.Max(last.Start + last.Length, span.Start + span.Length);
                merged[^1] = (last.Start, end - last.Start, last.Replacement);
                continue;
            }

            merged.Add(span);
        }

        var builder = new StringBuilder(text.Length);
        var position = 0;

        foreach (var span in merged)
        {
            builder.Append(text, position, span.Start - position);
            builder.Append(span.Replacement);
            position = span.Start + span.Length;
        }

        builder.Append(text, position, text.Length - position);
        return builder.ToString();
    }
}
