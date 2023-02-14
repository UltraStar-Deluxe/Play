using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public class TypeScriptDeclarationGenerator
{
    public Dictionary<string, string> CustomTypeMappings { get; set; } = new()
    {
        { "Voice", "any" },
        { "Encoding", "any" },
    };

    public List<string> IgnoredMemberNames { get; set; } = new()
    {
        "Equals",
        "GetHashCode",
        "GetType",
        "ToString",
    };
    
    public bool LowercaseFirstLetterOfMemberNames { get; set; } = true;
    
    private readonly List<TypeScriptInterfaceDeclarationInfo> interfaceDeclarations = new();
    
    public void AddInterfaceDeclaration(Type type)
    {
        interfaceDeclarations.Add(GetTypeScriptDeclarationInfo(type));
    }

    public void GenerateDeclarationsFile(string targetPath)
    {
        string declarationCode = interfaceDeclarations
            .Select(info =>
            {
                string memberDeclarations = info.memberDeclarationInfos
                    .Select(memberDeclInfo =>
                    {
                        string methodParameterList = memberDeclInfo.isMethod
                            ? "("
                              + memberDeclInfo.methodParameterNameToTypeName
                                  .Select(entry => $"{entry.Key}: {entry.Value}")
                                  .JoinWith(", ")
                              + ")"
                            : "";
                        string result = $"    {memberDeclInfo.memberName}{methodParameterList}: {memberDeclInfo.returnTypeName};";
                        return result;
                    })
                    .JoinWith("\n");
                string infoCode = $"declare interface {info.className} {{\n{memberDeclarations}\n}}";
                return infoCode;
            })
            .JoinWith("\n\n");
        File.WriteAllText(targetPath, declarationCode);
    }

    private TypeScriptInterfaceDeclarationInfo GetTypeScriptDeclarationInfo(Type type)
    {
        if (type == null)
        {
            return null;
        }

        TypeScriptInterfaceDeclarationInfo result = new()
        {
            className = type.Name,
        };

        MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public
                                                   | BindingFlags.Instance);
        foreach (MemberInfo memberInfo in memberInfos)
        {
            if ((memberInfo.MemberType
                is not MemberTypes.Method
                and not MemberTypes.Property)
                || IgnoredMemberNames.Contains(memberInfo.Name)
                || memberInfo.Name.StartsWith("get_")
                || memberInfo.Name.StartsWith("set_"))
            {
                continue;
            }
            
            string memberName = memberInfo.Name;
            if (LowercaseFirstLetterOfMemberNames)
            {
                memberName = memberName.Substring(0, 1).ToLower() + memberName.Substring(1);
            }
            
            string csReturnTypeName = GetMemberReturnTypeName(memberInfo);
            string memberTypeName = GetTypeScriptTypeName(csReturnTypeName);
            
            TypeScriptMemberDeclarationInfo memberDeclInfo = new()
            {
                memberName = memberName,
                returnTypeName = memberTypeName,
                isMethod = memberInfo is MethodInfo,
            };
            if (memberDeclInfo.isMethod)
            {
                MethodInfo methodInfo = (MethodInfo) memberInfo;
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                parameterInfos.ForEach(parameterInfo =>
                {
                    string parameterTypeName = parameterInfo.ParameterType.Name;
                    memberDeclInfo.methodParameterNameToTypeName[parameterInfo.Name] = GetTypeScriptTypeName(parameterTypeName);
                });
            }
            result.memberDeclarationInfos.Add(memberDeclInfo);
        }

        return result;
    }

    private string GetTypeScriptTypeName(string csTypeName)
    {
        if (CustomTypeMappings.ContainsKey(csTypeName))
        {
            return CustomTypeMappings[csTypeName];
        }

        return csTypeName switch
        {
            "Double" => "number",
            "Float" => "number",
            "Single" => "number",
            "Int32" => "number",
            "Int64" => "number",
            "UInt32" => "number",
            "UInt64" => "number",
            "String" => "string",
            "Boolean" => "boolean",
            "Void" => "void",
            "List`1" => "ArrayLike<any>",
            "IReadOnlyList`1" => "ArrayLike<any>",
            "Dictionary`2" => "Map<any, any>",
            "IReadOnlyDictionary`2" => "Map<any, any>",
            _ => csTypeName
                .Replace("`", "_")
        };
    }

    private string GetMemberReturnTypeName(MemberInfo memberInfo)
    {
        if (memberInfo is MethodInfo methodInfo)
        {
            return methodInfo.ReturnType.Name;
        }
        else if (memberInfo is PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType.Name;
        }
        return null;
    }
    
    public class TypeScriptMemberDeclarationInfo
    {
        public string memberName;
        public string returnTypeName;
        public bool isMethod;
        public readonly Dictionary<string, string> methodParameterNameToTypeName = new();
    }
    
    public class TypeScriptInterfaceDeclarationInfo
    {
        public string className;
        public readonly List<TypeScriptMemberDeclarationInfo> memberDeclarationInfos = new();
    }
}
