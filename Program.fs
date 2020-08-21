// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Text
open Mono.Cecil

let simplifyCollectionName name =
    match name with
    | "Nullable`1" -> "?"
    | "IEnumerable`1" -> "seq"
    | "ICollection`1" -> "coll"
    | "ObservableCollection`1" -> "coll"
    | "Task`1" -> "task"
    | "List`1" -> "list"
    | "IList`1" -> "list"    
    | "Dictionary`2" -> "dict"
    | "IDictionary`2" -> "dict"
    | "Tuple`2" -> "tuple"
    | "ValueTuple`2" -> "tuple"
    | name when name.StartsWith("Func`") -> "func"
    | name when name.StartsWith("Action`") -> "action"
    
    | "RepeatedField`1" -> "repeated"
    | name when name.Contains("`") -> name.Split('`').[0]
    | _ -> name
    
    
let rec simplifyGenericTypeArgs (args: TypeReference seq) =
    let mapped = args
                 |> Seq.map (fun a -> simplifyTypeName a)
    let concatted = String.concat "," mapped
    match Seq.length mapped with
    | 0 -> ""
    | 1 -> concatted
    | _ -> "(" + concatted + ")"
    

and simplifyTypeName (t: TypeReference) =

    match t.Name with
    | "Void" -> "()"
    | "Object" -> "obj"
    | "String" -> "str"
    | "Boolean" -> "bool"
    | "Int32" -> "int"
    | "Int64" -> "int64"
    | _ when t.IsGenericInstance -> 
        let git = t :?> GenericInstanceType
        (simplifyGenericTypeArgs git.GenericArguments) + " " + (simplifyCollectionName t.Name)
    | _ when t.IsArray ->
        let arr = t :?> ArrayType
        simplifyTypeName arr.ElementType + " array"
    | name when t.FullName.StartsWith("System.") -> name
    | _ -> t.FullName
     
let kindInd (typ: TypeDefinition) =
    match typ with
    | _ when typ.IsInterface -> "interface"
    | _ when typ.IsAbstract -> "abstract"
    | _ -> ""

let oneAttributeToString (attr: CustomAttribute) =    
    let ctorArgs =
        try
             attr.ConstructorArguments |> Array.ofSeq
         with
         | _ -> [||]
    
    if ctorArgs.Length = 0 then
        attr.AttributeType.Name
     else
         sprintf "%s(\"%s\")" attr.AttributeType.Name
             (attr.ConstructorArguments
              |> Seq.map (fun a -> a.Value.ToString().Replace(":", "_"))
              |> String.concat ",")
        
let attributesToString (attrs: CustomAttribute seq) =
    String.concat " " (attrs |> Seq.map oneAttributeToString)

let stringHasUnprintableChars (s: string) =
    let smallest = s.ToCharArray() |> Array.min
    (int) smallest < 0x20


let yamlKey (k: string) =
    if k.StartsWith("|") then ("?" + k) else k
    

let badChar c =
    c = '-' || c = ',' || c = '|' || c = ';' || c = ':' || c = '?' || (int c) > 127
    
    
let firstLast (st: string) =
    let s = st.Trim()
    match s.Length with
        | 0 -> '_', '_'
        | 1 -> s.[0], s.[0]
        | len -> s.[0], s.[len - 1]
    
let yamlString (s: string) =
    let firstChar, lastChar = firstLast s
    let needsQuoting = s.Contains("%") || s.Contains(": ") || s.Contains("{") || s.Contains("\"") || s.Contains("'")
                       || s.Contains("`") || s.Contains("@")
                       || badChar firstChar || badChar lastChar
    if needsQuoting then
        sprintf "'%s'"
            (s.Replace("'", "''") )
        else s 

let toAscii (s:string) =
    s |> Encoding.ASCII.GetBytes |> Encoding.ASCII.GetString

let parseAssembly (f: string) =    
    let a = AssemblyDefinition.ReadAssembly (f)
    let types = a.MainModule.Types |> Seq.sortBy (fun t -> t.FullName)
    
        
    let nest n = (String.replicate n "  ")
    
    
    let emitAssemblyMetadata (a: AssemblyDefinition) =
        printfn ".meta:"
        printfn "%sname: %s" (nest 1) a.MainModule.Assembly.FullName
        printfn "%s.attr:" (nest 1)
        for attr in a.MainModule.Assembly.CustomAttributes do
            printfn "%s- %s" (nest 2) (oneAttributeToString attr)
        
        printfn "%s.ref" (nest 1)    
        for dep in a.MainModule.AssemblyReferences do
            printfn "%s- %s %s" (nest 2) dep.Name (dep.Version.ToString())
    
    let emitOneType t =         
        let kindText = kindInd t

        let emitRaw lvl (s: string) =
            if stringHasUnprintableChars s then
                printfn "%s%s" (nest lvl) (toAscii s)
                // raise (System.BadImageFormatException("Obfuscated names found: "+s))
            else   
                printfn "%s%s" (nest lvl) s

        let nsToAbreviate = if t.Namespace.Length > 10 then t.Namespace + "." else ""
        
        let emit lvl (s: string) =
            if nsToAbreviate <> "" then s.Replace(nsToAbreviate, "NS.") else s
            |> emitRaw lvl
            
        let emitSection key =
            sprintf ".%s:" key |> emit 1
                            
        sprintf "%s:" (yamlKey t.FullName) |> emitRaw 0
        sprintf ".ns: %s" t.Namespace |> emitRaw 1
        if kindText <> "" then do
            sprintf ".t: %s" kindText |> emit 1
        if t.HasInterfaces then do
            emitSection "implements"
            for iface in t.Interfaces do
                sprintf "- %s" (simplifyTypeName iface.InterfaceType) |> emit 2 
        
        if t.BaseType <> null && t.BaseType.Name <> "Object" then do
            sprintf ".base: %s" (simplifyTypeName t.BaseType) |> emit 1
            
        if t.HasCustomAttributes then do
            sprintf ".attr: %s" (attributesToString t.CustomAttributes) |> emit 1 
        
        if t.HasProperties then do
            emitSection "prop"
            for p in t.Properties do           
                sprintf "%s: %s" (yamlKey p.Name) (simplifyTypeName p.PropertyType) |> emit 2

        
        let fieldSpec (f: FieldDefinition) =
            match f with
            | _ when f.IsPrivate -> ("private", f.Name, simplifyTypeName f.FieldType)
            | _ when f.HasConstant -> ("const", f.Name, f.Constant.ToString() |> toAscii |> yamlString)                                  
            | _ when f.IsStatic -> ("static", f.Name, simplifyTypeName f.FieldType)
            | _ when f.IsPublic -> ("public", f.Name, simplifyTypeName f.FieldType)
            | _ ->
                ("other", f.Name, f.ToString() |> yamlString)
                  
        let groupedFields =
            t.Fields
            |> Seq.map fieldSpec
            |> Seq.groupBy (fun (a,_,_) -> a)
            |> Seq.filter (fun (g, _ ) -> g <> "private")
            
        for (g, lst) in groupedFields do
            emitSection g
            for (_, n,v) in lst do
                sprintf "%s: %s" (yamlKey n) v |> emit 2
        
        let reportMethod (mi: MethodDefinition) =
            if mi.IsGetter || mi.IsSetter || mi.IsConstructor then false else true
           
        let methodsToReport = t.Methods |> Seq.filter reportMethod |> Array.ofSeq
        let emitListItem lvl li =
            "- " + (yamlString li) |> emit lvl
        if not (Array.isEmpty methodsToReport) then do
            emitSection "methods" 
            for m in methodsToReport do
                sprintf "%s -> %s" m.Name (simplifyTypeName m.ReturnType) |> emitListItem 2 

    emitAssemblyMetadata a
    for t in types do
        emitOneType t

let emitAssembliesFromFiles fileNames =
    for f in fileNames do
        try
            parseAssembly f
        with
            | :? System.BadImageFormatException as ex -> printfn "# Bad assembly: %s %A" f ex
             
        printfn "\n---\n"

[<EntryPoint>]
let main argv =    
    let files = argv
    let allFiles = 
        files
        |> Seq.collect
               (fun pat -> if Directory.Exists(pat) then Directory.GetFiles(pat, "*.dll") else [|pat|])
        |> Seq.filter (fun path -> File.Exists(path))
    
    emitAssembliesFromFiles allFiles
    0 // return an integer exit code
