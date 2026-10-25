using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Reads the game's C# sources and writes a JSON description of every type: members,
// bases, and which other types each one creates or uses. Input for the MagicDraw generator.
var root = args[0];
var output = args[1];

var files = new List<string> { "Program.cs", "IsaacGame.cs", "Menu.cs" }.Select(f => Path.Combine(root, f)).ToList();
foreach (var dir in new[] { "Core", "Game", "Net", "Rendering", "Demos" })
    files.AddRange(Directory.GetFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories));

var parse = new CSharpParseOptions(LanguageVersion.Preview);
var types = new List<Dictionary<string, object>>();
var bodies = new List<(Dictionary<string, object> Type, SyntaxNode Node)>();

foreach (var file in files.OrderBy(f => f))
{
    var cu = CSharpSyntaxTree.ParseText(File.ReadAllText(file), parse, file).GetCompilationUnitRoot();

    if (cu.Members.OfType<GlobalStatementSyntax>().Any())
    {
        var program = NewType("", "Program", "class", "public", false, true, false, new List<string>(), "Entry point (top-level statements).");
        ((List<object>)program["operations"]).Add(Op("Main", "public", true, false, false, null, [P("args", "string[]")]));
        types.Add(program);
        bodies.Add((program, cu));
    }

    foreach (var decl in cu.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    {
        if (decl.Parent is TypeDeclarationSyntax)
            continue; // nested types (e.g. private helper classes) are implementation details

        var ns = decl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
        var mods = decl.Modifiers.Select(m => m.Text).ToHashSet();
        string kind = decl switch
        {
            InterfaceDeclarationSyntax => "interface",
            EnumDeclarationSyntax => "enum",
            StructDeclarationSyntax => "struct",
            RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "struct",
            _ => "class",
        };
        var name = decl.Identifier.Text;
        if (decl is TypeDeclarationSyntax { TypeParameterList: { } tps })
            name += "<" + string.Join(", ", tps.Parameters.Select(p => p.Identifier.Text)) + ">";

        var bases = decl.BaseList?.Types.Select(t => BaseName(t.Type)).ToList() ?? new List<string>();
        var t = NewType(ns, name, kind, Visibility(mods, "package"), mods.Contains("abstract"), mods.Contains("static"),
            decl is RecordDeclarationSyntax, bases, Summary(decl));
        var attrs = (List<object>)t["attributes"];
        var ops = (List<object>)t["operations"];

        if (decl is EnumDeclarationSyntax e)
        {
            ((List<object>)t["literals"]).AddRange(e.Members.Select(m => (object)m.Identifier.Text));
            types.Add(t);
            continue;
        }

        var td = (TypeDeclarationSyntax)decl;
        bool inInterface = kind == "interface";
        string defaultVis = inInterface ? "public" : "private";

        // Primary constructor: `class Room(int gx, int gy)` or a positional record.
        if (td.ParameterList is { } primary)
        {
            var ps = primary.Parameters.Select(p => P(p.Identifier.Text, p.Type?.ToString() ?? "")).ToList();
            ops.Add(Op(td.Identifier.Text, "public", false, false, true, null, ps));
            if (decl is RecordDeclarationSyntax)
                foreach (var p in primary.Parameters)
                    attrs.Add(Attr(p.Identifier.Text, p.Type?.ToString() ?? "", "public", false, true, null));
        }

        foreach (var member in td.Members)
        {
            var mm = member.Modifiers.Select(m => m.Text).ToHashSet();
            var vis = Visibility(mm, defaultVis);
            bool isStatic = mm.Contains("static") || mm.Contains("const");
            switch (member)
            {
                case FieldDeclarationSyntax f:
                    foreach (var v in f.Declaration.Variables)
                    {
                        string init = mm.Contains("const") ? v.Initializer?.Value.ToString() : null;
                        attrs.Add(Attr(v.Identifier.Text, f.Declaration.Type.ToString(), vis, isStatic,
                            mm.Contains("readonly") || mm.Contains("const"), init));
                    }
                    break;
                case PropertyDeclarationSyntax p:
                    bool readOnly = p.ExpressionBody != null ||
                                    (p.AccessorList != null && !p.AccessorList.Accessors.Any(a => a.Keyword.IsKind(SyntaxKind.SetKeyword) && !a.Modifiers.Any()));
                    attrs.Add(Attr(p.Identifier.Text, p.Type.ToString(), vis, isStatic, readOnly, null));
                    break;
                case MethodDeclarationSyntax m:
                    var mName = m.Identifier.Text + (m.TypeParameterList is { } mtp
                        ? "<" + string.Join(", ", mtp.Parameters.Select(x => x.Identifier.Text)) + ">" : "");
                    var ret = m.ReturnType.ToString();
                    bool isAbstract = mm.Contains("abstract") || (inInterface && m.Body == null && m.ExpressionBody == null);
                    ops.Add(Op(mName, vis, isStatic, isAbstract, false, ret == "void" ? null : ret, Params(m.ParameterList)));
                    break;
                case ConstructorDeclarationSyntax c when !mm.Contains("static"):
                    ops.Add(Op(c.Identifier.Text, vis, false, false, true, null, Params(c.ParameterList)));
                    break;
            }
        }

        types.Add(t);
        bodies.Add((t, decl));
    }
}

// Which model types each type creates (`new X(...)`) or calls statically (`X.Member`).
var simple = types.ToDictionary(t => BaseName((string)t["name"]), t => t);
foreach (var (t, node) in bodies)
{
    var self = BaseName((string)t["name"]);
    // `Stats.MaxHealth` inside Player reads the Stats *property*, not the Stats class.
    var ownMembers = ((List<object>)t["attributes"]).Select(a => (string)a.GetType().GetProperty("name")!.GetValue(a)).ToHashSet();
    var creates = new SortedSet<string>();
    var uses = new SortedSet<string>();
    var scope = node is CompilationUnitSyntax cu
        ? cu.Members.OfType<GlobalStatementSyntax>().Cast<SyntaxNode>()
        : new[] { node };
    foreach (var n in scope.SelectMany(s => s.DescendantNodes()))
    {
        if (n is ObjectCreationExpressionSyntax oc)
        {
            var target = BaseName(oc.Type);
            if (target != self && simple.ContainsKey(target)) creates.Add(target);
        }
        else if (n is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax id })
        {
            var target = id.Identifier.Text;
            if (target != self && simple.ContainsKey(target) && !ownMembers.Contains(target)) uses.Add(target);
        }
    }
    uses.ExceptWith(creates);
    t["creates"] = creates.ToList();
    t["uses"] = uses.ToList();
}

File.WriteAllText(output, JsonSerializer.Serialize(new { types }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{types.Count} types written to {output}");
foreach (var g in types.GroupBy(t => (string)t["namespace"]).OrderBy(g => g.Key))
    Console.WriteLine($"  {(g.Key == "" ? "(global)" : g.Key),-24} {g.Count(),3}  {string.Join(", ", g.Select(t => t["name"]))}");

static Dictionary<string, object> NewType(string ns, string name, string kind, string vis, bool isAbstract, bool isStatic,
    bool isRecord, List<string> bases, string doc) => new()
{
    ["namespace"] = ns, ["name"] = name, ["kind"] = kind, ["visibility"] = vis, ["isAbstract"] = isAbstract,
    ["isStatic"] = isStatic, ["isRecord"] = isRecord, ["bases"] = bases, ["doc"] = doc,
    ["attributes"] = new List<object>(), ["operations"] = new List<object>(), ["literals"] = new List<object>(),
};

static object Attr(string name, string type, string vis, bool isStatic, bool readOnly, string init) =>
    new { name, type, visibility = vis, isStatic, readOnly, init };

static object Op(string name, string vis, bool isStatic, bool isAbstract, bool isCtor, string returnType, List<object> parameters) =>
    new { name, visibility = vis, isStatic, isAbstract, isCtor, returnType, parameters };

static object P(string name, string type) => new { name, type };

static List<object> Params(ParameterListSyntax list) =>
    list.Parameters.Select(p => P(p.Identifier.Text, p.Type?.ToString() ?? "")).ToList();

static string Visibility(HashSet<string> mods, string fallback) =>
    mods.Contains("public") ? "public"
    : mods.Contains("protected") ? "protected"
    : mods.Contains("internal") ? "package"
    : mods.Contains("private") ? "private"
    : fallback;

static string BaseName(object type)
{
    var s = type is TypeSyntax ts ? ts.ToString() : (string)type;
    int lt = s.IndexOf('<');
    return (lt >= 0 ? s[..lt] : s).Trim();
}

static string Summary(SyntaxNode decl)
{
    var xml = decl.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
    var summary = xml?.Content.OfType<XmlElementSyntax>().FirstOrDefault(x => x.StartTag.Name.ToString() == "summary");
    if (summary == null) return null;
    var text = string.Concat(summary.Content.Select(c => c switch
    {
        XmlTextSyntax tx => string.Concat(tx.TextTokens.Select(k => k.Text)),
        XmlEmptyElementSyntax em => em.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault()?.Cref.ToString() ?? "",
        XmlElementSyntax el => string.Concat(el.Content.Select(x => x.ToString())),
        _ => "",
    }));
    return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
}
