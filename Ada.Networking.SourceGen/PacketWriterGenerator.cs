using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ada.Networking.SourceGen;

[Generator(LanguageNames.CSharp)]
public sealed class PacketWriterGenerator : IIncrementalGenerator
{
    private const string _abstractWriterMetadataName = "Ada.API.Interfaces.Networking.AbstractPacketWriter";
    private const string _packetDataMetadataName = "Ada.Core.Shared.Attributes.PacketDataAttribute";
    private const string _writerType = "global::Ada.API.INetworkPacketWriter";
    private const string _actionType = "global::System.Action<object, global::Ada.API.INetworkPacketWriter>";

    private static readonly HashSet<string> _enumerableNames = new()
    {
        "List", "IList", "ICollection", "IReadOnlyList", "IReadOnlyCollection", "HashSet"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) is not INamedTypeSymbol symbol)
                    {
                        return default;
                    }

                    var abstractWriter = ctx.SemanticModel.Compilation
                        .GetTypeByMetadataName(_abstractWriterMetadataName);

                    if (abstractWriter is null || !IsCandidateWriter(symbol, abstractWriter))
                    {
                        return default;
                    }

                    var packetData = ctx.SemanticModel.Compilation
                        .GetTypeByMetadataName(_packetDataMetadataName);

                    return new WriterCandidate(symbol, packetData);
                })
            .Where(static c => c.Writer is not null);

        context.RegisterSourceOutput(candidates.Collect(), static (spc, writers) => Execute(spc, writers));
    }

    private readonly struct WriterCandidate
    {
        public readonly INamedTypeSymbol? Writer;
        public readonly INamedTypeSymbol? PacketData;

        public WriterCandidate(INamedTypeSymbol? writer, INamedTypeSymbol? packetData)
        {
            Writer = writer;
            PacketData = packetData;
        }
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<WriterCandidate> rawCandidates)
    {
        if (rawCandidates.IsDefaultOrEmpty)
        {
            return;
        }

        var packetData = rawCandidates[0].PacketData;
        var candidates = rawCandidates.Select(static c => c.Writer!).ToImmutableArray();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var entries = new List<(string TypeName, string Method)>();
        var methods = new StringBuilder();
        var index = 0;

        foreach (var writer in candidates.OrderBy(static c => c.ToDisplayString(), System.StringComparer.Ordinal))
        {
            if (!seen.Add(writer))
            {
                continue;
            }

            var emitter = new Emitter(packetData);
            var body = emitter.TryEmitWriter(writer);

            if (body is null)
            {
                continue;
            }

            var typeName = writer.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var method = "Write_" + index++;
            entries.Add((typeName, method));

            methods.Append("        private static void ").Append(method)
                .Append('(').Append(typeName).Append(" p, ").Append(_writerType).Append(" w)\n        {\n")
                .Append(body)
                .Append("        }\n\n");
        }

        if (entries.Count == 0)
        {
            return;
        }

        spc.AddSource("GeneratedPacketWriters.g.cs", BuildSource(entries, methods.ToString()));
    }

    private static bool IsCandidateWriter(INamedTypeSymbol writer, INamedTypeSymbol abstractWriter)
    {
        if (writer.TypeKind != TypeKind.Class || writer.IsAbstract || writer.IsStatic)
        {
            return false;
        }

        if (!writer.TypeParameters.IsEmpty || writer.ContainingType is not null)
        {
            return false;
        }

        if (writer.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(writer.BaseType, abstractWriter))
        {
            return false;
        }

        if (DeclaresMethod(writer, "OnSerialize"))
        {
            return false;
        }

        return true;
    }

    private static bool DeclaresMethod(INamedTypeSymbol type, string name)
        => type.GetMembers(name).OfType<IMethodSymbol>().Any();

    private static string BuildSource(List<(string TypeName, string Method)> entries, string methods)
    {
        var sb = new StringBuilder();

        sb.Append("// <auto-generated/>\n#nullable enable\n");
        sb.Append("namespace Ada.Networking.Writers.Generated\n{\n");
        sb.Append("    public static class GeneratedPacketWriters\n    {\n");

        sb.Append("        private static readonly global::System.Collections.Frozen.FrozenDictionary<global::System.Type, ")
            .Append(_actionType).Append("> Writers = Create();\n\n");

        sb.Append("        [global::System.Runtime.CompilerServices.ModuleInitializer]\n");
        sb.Append("        internal static void Initialize()\n");
        sb.Append("            => global::Ada.API.PacketWriterFastPath.Handler = TrySerialize;\n\n");

        sb.Append("        public static bool TrySerialize(object packet, ").Append(_writerType).Append(" writer)\n        {\n");
        sb.Append("            if (Writers.TryGetValue(packet.GetType(), out var write))\n            {\n");
        sb.Append("                write(packet, writer);\n                return true;\n            }\n\n");
        sb.Append("            return false;\n        }\n\n");

        sb.Append("        public static bool Handles(global::System.Type type) => Writers.ContainsKey(type);\n\n");

        sb.Append("        private static global::System.Collections.Frozen.FrozenDictionary<global::System.Type, ")
            .Append(_actionType).Append("> Create()\n        {\n");
        sb.Append("            var map = new global::System.Collections.Generic.Dictionary<global::System.Type, ")
            .Append(_actionType).Append(">(").Append(entries.Count).Append(")\n            {\n");

        foreach (var (typeName, method) in entries)
        {
            sb.Append("                [typeof(").Append(typeName).Append(")] = static (o, w) => ")
                .Append(method).Append("((").Append(typeName).Append(")o, w),\n");
        }

        sb.Append("            };\n\n");
        sb.Append("            return global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(map);\n        }\n\n");

        sb.Append(methods);
        sb.Append("    }\n}\n");

        return sb.ToString();
    }

    private sealed class Emitter
    {
        private readonly INamedTypeSymbol? _packetData;
        private int _counter;

        public Emitter(INamedTypeSymbol? packetData) => _packetData = packetData;

        public string? TryEmitWriter(INamedTypeSymbol writer)
        {
            var sb = new StringBuilder();
            var visited = ImmutableHashSet<INamedTypeSymbol>.Empty.WithComparer(SymbolEqualityComparer.Default);

            return EmitObject(sb, "p", writer, "            ", visited, attributedOnly: false) ? sb.ToString() : null;
        }

        private bool EmitObject(StringBuilder sb, string target, INamedTypeSymbol type, string indent, ImmutableHashSet<INamedTypeSymbol> visited, bool attributedOnly)
        {
            foreach (var property in SerializableProperties(type, attributedOnly))
            {
                if (!EmitValue(sb, target + "." + property.Name, property.Type, indent, visited))
                {
                    return false;
                }
            }

            return true;
        }

        private bool EmitValue(StringBuilder sb, string expr, ITypeSymbol type, string indent, ImmutableHashSet<INamedTypeSymbol> visited)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                    sb.Append(indent).Append("w.WriteString(").Append(expr).Append(" ?? string.Empty);\n");
                    return true;
                case SpecialType.System_Int32:
                    sb.Append(indent).Append("w.WriteInteger(").Append(expr).Append(");\n");
                    return true;
                case SpecialType.System_Int16:
                    sb.Append(indent).Append("w.WriteShort(").Append(expr).Append(");\n");
                    return true;
                case SpecialType.System_Int64:
                    sb.Append(indent).Append("w.WriteLong(").Append(expr).Append(");\n");
                    return true;
                case SpecialType.System_Boolean:
                    sb.Append(indent).Append("w.WriteBool(").Append(expr).Append(");\n");
                    return true;
            }

            if (type is IArrayTypeSymbol array)
            {
                return EmitCollection(sb, expr, array.ElementType, ".Length", indent, visited);
            }

            if (type is INamedTypeSymbol named)
            {
                if (IsGeneric(named, "List", 1) && named.TypeArguments[0].SpecialType == SpecialType.System_String)
                {
                    return EmitStringList(sb, expr, indent);
                }

                if (IsGeneric(named, "Dictionary", 2))
                {
                    return EmitDictionary(sb, expr, named, indent);
                }

                if (IsEnumerable(named, out var element))
                {
                    return EmitCollection(sb, expr, element!, ".Count", indent, visited);
                }

                return EmitSingleObject(sb, expr, named, indent, visited);
            }

            return false;
        }

        private bool EmitStringList(StringBuilder sb, string expr, string indent)
        {
            var id = _counter++;
            var inner = indent + "    ";

            sb.Append(indent).Append("{\n");
            sb.Append(inner).Append("var __l").Append(id).Append(" = ").Append(expr).Append(";\n");
            sb.Append(inner).Append("if (__l").Append(id).Append(" is null) { w.WriteInteger(0); }\n");
            sb.Append(inner).Append("else { w.WriteInteger(__l").Append(id).Append(".Count); foreach (var __s").Append(id)
                .Append(" in __l").Append(id).Append(") { w.WriteString(__s").Append(id).Append(" ?? string.Empty); } }\n");
            sb.Append(indent).Append("}\n");

            return true;
        }

        private bool EmitDictionary(StringBuilder sb, string expr, INamedTypeSymbol dict, string indent)
        {
            var key = dict.TypeArguments[0];
            var value = dict.TypeArguments[1];

            string? keyWrite = key.SpecialType switch
            {
                SpecialType.System_Int32 => "w.WriteInteger(__kv{0}.Key);",
                SpecialType.System_Int64 => "w.WriteLong(__kv{0}.Key);",
                SpecialType.System_String => "w.WriteString(__kv{0}.Key);",
                _ => null
            };

            if (keyWrite is null)
            {
                return false;
            }

            string? valueWrite;

            if (value is INamedTypeSymbol valueNamed && IsGeneric(valueNamed, "List", 1) && valueNamed.TypeArguments[0].SpecialType == SpecialType.System_String)
            {
                valueWrite = "foreach (var __s{0} in __kv{0}.Value) { w.WriteString(__s{0}); }";
            }
            else
            {
                valueWrite = value.SpecialType switch
                {
                    SpecialType.System_Int32 => "w.WriteInteger(__kv{0}.Value);",
                    SpecialType.System_Int64 => "w.WriteLong(__kv{0}.Value);",
                    SpecialType.System_String => "w.WriteString(__kv{0}.Value ?? string.Empty);",
                    _ => null
                };
            }

            if (valueWrite is null)
            {
                return false;
            }

            var id = _counter++;
            var inner = indent + "    ";

            sb.Append(indent).Append("{\n");
            sb.Append(inner).Append("var __d").Append(id).Append(" = ").Append(expr).Append(";\n");
            sb.Append(inner).Append("if (__d").Append(id).Append(" is null) { w.WriteInteger(0); }\n");
            sb.Append(inner).Append("else\n").Append(inner).Append("{\n");
            sb.Append(inner).Append("    w.WriteInteger(__d").Append(id).Append(".Count);\n");
            sb.Append(inner).Append("    foreach (var __kv").Append(id).Append(" in __d").Append(id).Append(")\n");
            sb.Append(inner).Append("    {\n");
            var idText = id.ToString();
            sb.Append(inner).Append("        ").Append(keyWrite.Replace("{0}", idText)).Append('\n');
            sb.Append(inner).Append("        ").Append(valueWrite.Replace("{0}", idText)).Append('\n');
            sb.Append(inner).Append("    }\n");
            sb.Append(inner).Append("}\n");
            sb.Append(indent).Append("}\n");

            return true;
        }

        private bool EmitCollection(StringBuilder sb, string expr, ITypeSymbol element, string countMember, string indent, ImmutableHashSet<INamedTypeSymbol> visited)
        {
            var id = _counter++;
            var inner = indent + "    ";

            if (element.SpecialType == SpecialType.System_String)
            {
                sb.Append(indent).Append("{\n");
                sb.Append(inner).Append("var __c").Append(id).Append(" = ").Append(expr).Append(";\n");
                sb.Append(inner).Append("if (__c").Append(id).Append(" is not null) { w.WriteInteger(__c").Append(id).Append(countMember)
                    .Append("); foreach (var __e").Append(id).Append(" in __c").Append(id).Append(") { w.WriteString(__e").Append(id).Append(" ?? string.Empty); } }\n");
                sb.Append(indent).Append("}\n");
                return true;
            }

            var primitive = PrimitiveWriteMethod(element);

            if (primitive is not null)
            {
                sb.Append(indent).Append("{\n");
                sb.Append(inner).Append("var __c").Append(id).Append(" = ").Append(expr).Append(";\n");
                sb.Append(inner).Append("if (__c").Append(id).Append(" is not null) { w.WriteInteger(__c").Append(id).Append(countMember)
                    .Append("); foreach (var __e").Append(id).Append(" in __c").Append(id).Append(") { ").Append(primitive).Append("(__e").Append(id).Append("); } }\n");
                sb.Append(indent).Append("}\n");
                return true;
            }

            if (element is not INamedTypeSymbol dto || !IsSerializableObject(dto) || visited.Contains(dto))
            {
                return false;
            }

            var itemSb = new StringBuilder();

            if (!EmitObject(itemSb, "__e" + id, dto, inner + "    ", visited.Add(dto), attributedOnly: false))
            {
                return false;
            }

            sb.Append(indent).Append("{\n");
            sb.Append(inner).Append("var __c").Append(id).Append(" = ").Append(expr).Append(";\n");
            sb.Append(inner).Append("if (__c").Append(id).Append(" is not null)\n").Append(inner).Append("{\n");
            sb.Append(inner).Append("    w.WriteInteger(__c").Append(id).Append(countMember).Append(");\n");
            sb.Append(inner).Append("    foreach (var __e").Append(id).Append(" in __c").Append(id).Append(")\n");
            sb.Append(inner).Append("    {\n");
            sb.Append(itemSb);
            sb.Append(inner).Append("    }\n");
            sb.Append(inner).Append("}\n");
            sb.Append(indent).Append("}\n");

            return true;
        }

        private bool EmitSingleObject(StringBuilder sb, string expr, INamedTypeSymbol type, string indent, ImmutableHashSet<INamedTypeSymbol> visited)
        {
            if (_packetData is null || !IsSerializableObject(type) || visited.Contains(type))
            {
                return false;
            }

            var id = _counter++;
            var inner = indent + "    ";
            var objSb = new StringBuilder();
            var isReference = type.IsReferenceType;
            var bodyIndent = isReference ? inner + "    " : inner;

            if (!EmitObject(objSb, "__o" + id, type, bodyIndent, visited.Add(type), attributedOnly: true))
            {
                return false;
            }

            sb.Append(indent).Append("{\n");
            sb.Append(inner).Append("var __o").Append(id).Append(" = ").Append(expr).Append(";\n");

            if (isReference)
            {
                sb.Append(inner).Append("if (__o").Append(id).Append(" is not null)\n").Append(inner).Append("{\n");
                sb.Append(objSb);
                sb.Append(inner).Append("}\n");
            }
            else
            {
                sb.Append(objSb);
            }

            sb.Append(indent).Append("}\n");

            return true;
        }

        private bool IsSerializableObject(INamedTypeSymbol type)
        {
            if (type.SpecialType != SpecialType.None || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return false;
            }

            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract || !type.TypeParameters.IsEmpty)
            {
                return false;
            }

            var baseType = type.BaseType;

            return baseType is null || baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType;
        }

        private IEnumerable<IPropertySymbol> SerializableProperties(INamedTypeSymbol type, bool attributedOnly)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                if (property.IsStatic || property.IsIndexer || property.GetMethod is null)
                {
                    continue;
                }

                if (property.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (attributedOnly && !HasPacketData(property))
                {
                    continue;
                }

                yield return property;
            }
        }

        private bool HasPacketData(IPropertySymbol property)
        {
            if (_packetData is null)
            {
                return false;
            }

            return property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _packetData));
        }

        private static string? PrimitiveWriteMethod(ITypeSymbol type) => type.SpecialType switch
        {
            SpecialType.System_Int32 => "w.WriteInteger",
            SpecialType.System_Int64 => "w.WriteLong",
            SpecialType.System_Int16 => "w.WriteShort",
            SpecialType.System_Boolean => "w.WriteBool",
            _ => null
        };

        private static bool IsGeneric(INamedTypeSymbol type, string name, int arity)
            => type.Name == name
               && type.Arity == arity
               && type.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic";

        private static bool IsEnumerable(INamedTypeSymbol type, out ITypeSymbol? element)
        {
            element = null;

            if (type.Arity != 1 || type.ContainingNamespace?.ToDisplayString() != "System.Collections.Generic" || !_enumerableNames.Contains(type.Name))
            {
                return false;
            }

            element = type.TypeArguments[0];
            return true;
        }
    }
}
