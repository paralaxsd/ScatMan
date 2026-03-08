using System.ComponentModel;
using System.Text.Json;
using ScatMan.Cli;
using ScatMan.Core;
using MemberDescriptor = ScatMan.Core.MemberDescriptor;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ScatMan.Cli.Commands;

sealed class MembersCommand : AsyncCommand<MembersCommand.Settings>
{
    public sealed class Settings : PackageSettings
    {
        [CommandArgument(2, "<typeName>")]
        [Description("Full or simple type name")]
        public string TypeName { get; init; } = "";

        [CommandOption("--no-default-values")]
        [Description("Do not include optional parameter default values in method signatures")]
        public bool NoDefaultValues { get; init; }

        [CommandOption("--include-attributes")]
        [Description("Include member and parameter attributes in signatures")]
        public bool IncludeAttributes { get; init; }

        [CommandOption("--kind")]
        [Description("Filter by member kind: constructor, method, property, field, event")]
        public string? Kind { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var (assemblies, resolvedVersion) = await settings.FetchAssembliesAsync(ct);

        try
        {
            var members = new TypeInspector().GetMembers(
                assemblies,
                settings.TypeName,
                includeDefaultValues: !settings.NoDefaultValues,
                includeAttributes: settings.IncludeAttributes,
                kind: settings.Kind);

            if (settings.Json) PrintJson(members, settings, resolvedVersion);
            else PrintFormatted(members, settings);
        }
        catch (TypeNotFoundException ex)
        {
            PrintError(ex.Message, settings.Json);
            return 1;
        }

        return 0;
    }

    static void PrintJson(
        IReadOnlyList<MemberDescriptor> members,
        Settings settings,
        string resolvedVersion)
    {
        var result = new MembersResult(
            settings.Package,
            resolvedVersion,
            settings.TypeName,
            [.. members.Select(m => new MemberResult(m.Name, m.Kind, m.Signature, m.Summary))]);

        Console.WriteLine(JsonSerializer.Serialize(result, BaseSettings.JsonOptions));
    }

    static void PrintFormatted(IReadOnlyList<MemberDescriptor> members, Settings settings)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(settings.TypeName)}[/] — {members.Count} public member(s)\n");

        foreach (var group in members.GroupBy(m => m.Kind).OrderBy(g => g.Key))
        {
            var label = group.Key == "property" ? "properties" : $"{group.Key}s";
            AnsiConsole.MarkupLine($"[grey]{label}[/]");

            foreach (var m in group)
            {
                PrintSignature(m.Signature);

                if (m.Summary is { Length: > 0 } summary)
                    AnsiConsole.MarkupLine($"    [grey]{Markup.Escape(summary)}[/]");
            }

            AnsiConsole.WriteLine();
        }

        if (members.Count == 0)
            AnsiConsole.MarkupLine("  [yellow](no public members)[/]");
    }

    static void PrintSignature(string signature)
    {
        var width = AnsiConsole.Profile.Width;
        var parenIdx = signature.IndexOf('(');

        if (2 + signature.Length <= width || parenIdx < 0)
        {
            AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(signature)}[/]");
            return;
        }

        var prefix = signature[..parenIdx];
        var inner  = signature[(parenIdx + 1)..signature.LastIndexOf(')')];

        if (string.IsNullOrEmpty(inner))
        {
            AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(signature)}[/]");
            return;
        }

        var @params = SplitParams(inner);
        AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(prefix)}([/]");
        foreach (var (p, last) in @params.Select((p, i) => (p, i == @params.Length - 1)))
            AnsiConsole.MarkupLine($"    [cyan]{Markup.Escape(p)}{(last ? ")" : ",")}[/]");
    }

    static string[] SplitParams(string s)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < s.Length; i++)
        {
            if      (s[i] is '<' or '(') depth++;
            else if (s[i] is '>' or ')') depth--;
            else if (s[i] == ',' && depth == 0)
            {
                parts.Add(s[start..i].Trim());
                start = i + 1;
            }
        }

        parts.Add(s[start..].Trim());
        return [.. parts];
    }

    static void PrintError(string message, bool json)
    {
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(new { error = message }, BaseSettings.JsonOptions));
        else
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }
}
