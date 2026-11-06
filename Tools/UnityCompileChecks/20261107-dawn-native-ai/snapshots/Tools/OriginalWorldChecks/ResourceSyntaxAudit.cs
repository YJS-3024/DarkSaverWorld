using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
internal static class ResourceSyntaxAudit
{
    static void Main(string[] args)
    {
        var calls=new List<object>();var classes=new List<object>();
        foreach(string path in Directory.GetFiles(Path.Combine(args[0],"Assets/_Original/Runtime"),"*.cs",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal))
        {
            var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path));var root=tree.GetRoot();string relative=Path.GetRelativePath(args[0],path).Replace('\\','/');
            foreach(var declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())classes.Add(new{source=relative,name=declaration.Identifier.ValueText,bases=declaration.BaseList?.Types.Select(t=>t.Type.ToString()).ToArray()??Array.Empty<string>()});
            foreach(var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if(!(invocation.Expression is MemberAccessExpressionSyntax member)||!(member.Expression.ToString()=="Resources"||member.Expression.ToString()=="UnityEngine.Resources")||!new[]{"Load","LoadAll","LoadAsync"}.Contains(member.Name.Identifier.ValueText))continue;
                var argument=invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;string key=argument is LiteralExpressionSyntax literal&&literal.IsKind(SyntaxKind.StringLiteralExpression)?literal.Token.ValueText:null;
                string type=member.Name is GenericNameSyntax generic?generic.TypeArgumentList.Arguments.Single().ToString():invocation.ArgumentList.Arguments.Skip(1).FirstOrDefault()?.Expression is TypeOfExpressionSyntax kind?kind.Type.ToString():"Object";
                calls.Add(new{source=relative,line=invocation.GetLocation().GetLineSpan().StartLinePosition.Line+1,method=member.Name.Identifier.ValueText,key,type});
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new{calls,classes}));
    }
}
