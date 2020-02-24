# DllYaml
Dump dotnet assembly contents in parseable and readable YAML format

Intended for easy parsing. Can be used to implement tools that produce
content by reflecting on the binaries of existing code (e.g. protobuf and DTO generators).

## Installation 

```
$ dotnet tool install DllYaml -g
$ dllyaml myapp.dll mylib.dll > output.yaml
```

Example ouput from running against mono.cecil:

```yaml
.meta:
  name: Mono.Cecil.Rocks, Version=0.11.2.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e
  .attr:
    - ExtensionAttribute
    - CompilationRelaxationsAttribute("8")
    - RuntimeCompatibilityAttribute
    - DebuggableAttribute("2")
    - AssemblyProductAttribute("Mono.Cecil")
    - AssemblyCopyrightAttribute("Copyright � 2008 - 2018 Jb Evain")
    - ComVisibleAttribute("False")
    - AssemblyFileVersionAttribute("0.11.2.0")
    - AssemblyInformationalVersionAttribute("0.11.2.0")
    - AssemblyTitleAttribute("Mono.Cecil.Rocks")
    - CLSCompliantAttribute("False")
    - TargetFrameworkAttribute(".NETStandard,Version=v2.0")
<Module>:
  .ns: 
Mono.Cecil.Rocks.DocCommentId:
  .ns: Mono.Cecil.Rocks
  .methods:
    - WriteField -> ()
    - WriteEvent -> ()
    - WriteType -> ()
    - WriteMethod -> ()
    - IsConversionOperator -> Boolean
    - WriteReturnType -> ()
    - WriteProperty -> ()
    - WriteParameters -> ()
    - WriteTypeSignature -> ()
    - WriteGenericInstanceTypeSignature -> ()
    - WriteList -> ()
    - WriteModiferTypeSignature -> ()
    - WriteFunctionPointerTypeSignature -> ()
    - WriteArrayTypeSignature -> ()
    - WriteDefinition -> ()
    - WriteTypeFullName -> ()
    - WriteItemName -> ()
    - ToString -> String
    - GetDocCommentId -> String
    - <WriteParameters>b__9_0 -> ()
    - <WriteArrayTypeSignature>b__15_0 -> ()
Mono.Cecil.Rocks.Functional:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - Y -> A, R Func`2
    - Prepend -> TSource seq
    - PrependIterator -> TSource seq
Mono.Cecil.Rocks.IILVisitor:
  .ns: Mono.Cecil.Rocks
  .t: interface
  .methods:
    - OnInlineNone -> ()
    - OnInlineSByte -> ()
    - OnInlineByte -> ()
    - OnInlineInt32 -> ()
    - OnInlineInt64 -> ()
    - OnInlineSingle -> ()
    - OnInlineDouble -> ()
    - OnInlineString -> ()
    - OnInlineBranch -> ()
    - OnInlineSwitch -> ()
    - OnInlineVariable -> ()
    - OnInlineArgument -> ()
    - OnInlineSignature -> ()
    - OnInlineType -> ()
    - OnInlineField -> ()
    - OnInlineMethod -> ()
Mono.Cecil.Rocks.ILParser:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .methods:
    - Parse -> ()
    - ParseMethod -> ()
    - CreateContext -> NS.ILParser/ParseContext
    - ParseFatMethod -> ()
    - ParseCode -> ()
    - GetVariable -> Mono.Cecil.Cil.VariableDefinition
Mono.Cecil.Rocks.MethodBodyRocks:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - SimplifyMacros -> ()
    - ExpandMacro -> ()
    - MakeMacro -> ()
    - Optimize -> ()
    - OptimizeLongs -> ()
    - OptimizeMacros -> ()
    - OptimizeBranches -> ()
    - OptimizeBranch -> Boolean
    - ComputeOffsets -> ()
Mono.Cecil.Rocks.MethodDefinitionRocks:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - GetBaseMethod -> Mono.Cecil.MethodDefinition
    - GetOriginalBaseMethod -> Mono.Cecil.MethodDefinition
    - ResolveBaseType -> Mono.Cecil.TypeDefinition
    - GetMatchingMethod -> Mono.Cecil.MethodDefinition
Mono.Cecil.Rocks.ModuleDefinitionRocks:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - GetAllTypes -> Mono.Cecil.TypeDefinition seq
Mono.Cecil.Rocks.ParameterReferenceRocks:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - GetSequence -> Int32
Mono.Cecil.Rocks.TypeDefinitionRocks:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - GetConstructors -> Mono.Cecil.MethodDefinition seq
    - GetStaticConstructor -> Mono.Cecil.MethodDefinition
    - GetMethods -> Mono.Cecil.MethodDefinition seq
    - GetEnumUnderlyingType -> Mono.Cecil.TypeReference
Mono.Cecil.Rocks.TypeReferenceRocks:
  .ns: Mono.Cecil.Rocks
  .t: abstract
  .attr: ExtensionAttribute
  .methods:
    - MakeArrayType -> Mono.Cecil.ArrayType
    - MakeArrayType -> Mono.Cecil.ArrayType
    - MakePointerType -> Mono.Cecil.PointerType
    - MakeByReferenceType -> Mono.Cecil.ByReferenceType
    - MakeOptionalModifierType -> Mono.Cecil.OptionalModifierType
    - MakeRequiredModifierType -> Mono.Cecil.RequiredModifierType
    - MakeGenericInstanceType -> Mono.Cecil.GenericInstanceType
    - MakePinnedType -> Mono.Cecil.PinnedType
    - MakeSentinelType -> Mono.Cecil.SentinelType

---

```
