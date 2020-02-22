// Learn more about F# at http://fsharp.org

open System.IO
open Mono.Cecil

let simplifyCollectionName name =
    match name with
    | "Nullable`1" -> "?"
    | "IEnumerable`1" -> "seq"
    | "ICollection`1" -> "coll"
    | "ObservableCollection`1" -> "ocoll"
    | "Task`1" -> "task"
    | "List`1" -> "list"
    | "Dictionary`2" -> "dict"
    | "RepeatedField`1" -> "repeated"
    | _ -> name
    
    
let rec simplifyGenericTypeArgs (args: TypeReference seq) =
    args
    |> Seq.map (fun a -> simplifyTypeName a)
    |> String.concat ", "

and simplifyTypeName (t: TypeReference) =

    match t with
    | _ when t.Name = "Void" -> "()"
    | _ when t.IsGenericInstance -> 
        let git = t :?> GenericInstanceType
        (simplifyGenericTypeArgs git.GenericArguments) + " " + (simplifyCollectionName t.Name)
    | _ when t.IsArray ->
        let arr = t :?> ArrayType
        simplifyTypeName arr.ElementType + " array"
    | _ -> t.FullName.Replace("System.", "")
     
let kindInd (typ: TypeDefinition) =
    match typ with
    | _ when typ.IsInterface -> "interface"
    | _ when typ.IsAbstract -> "abstract"
    | _ -> ""
    


let oneAttributeToString (attr: CustomAttribute) =
    if attr.ConstructorArguments.Count = 0 then
        attr.AttributeType.Name
     else
         sprintf "%s(\"%s\")" attr.AttributeType.Name
             (attr.ConstructorArguments
              |> Seq.map (fun a -> a.Value.ToString().Replace(":", "_"))
              |> String.concat ",")
        
let attributesToString (attrs: CustomAttribute seq) =
    String.concat " " (attrs |> Seq.map oneAttributeToString)

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

        
    let emitOneType t =         
        let kindText = kindInd t

        let emitRaw lvl (s: string) =
            printfn "%s%s" (nest lvl) s

        let emit lvl (s: string) =
            s.Replace(t.Namespace + ".", "NS.") |> emitRaw lvl
            
        let emitSection key =
            sprintf ".%s:" key |> emit 1
                            
        sprintf "%s:" t.FullName |> emitRaw 0
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
                sprintf "%s: %s" p.Name (simplifyTypeName p.PropertyType) |> emit 2

        
        let fieldSpec (f: FieldDefinition) =
            match f with
            | _ when f.IsPrivate -> ("private", f.Name, simplifyTypeName f.FieldType)
            | _ when f.HasConstant -> ("const", f.Name,
                                       match f.Constant with
                                       | :? string as s -> sprintf "\"%s\"" (s.Replace("\\", "\\\\"))
                                       | :? char as c -> sprintf "'%c'" c
                                       | c -> c.ToString())
            | _ when f.IsStatic -> ("static", f.Name, simplifyTypeName f.FieldType)
            | _ when f.IsPublic -> ("public", f.Name, simplifyTypeName f.FieldType)
            | _ ->
                ("other", f.Name, f.ToString())
                  
        let groupedFields =
            t.Fields
            |> Seq.map fieldSpec
            |> Seq.groupBy (fun (a,_,_) -> a)
            |> Seq.filter (fun (g, _ ) -> g <> "private")
            
        for (g, lst) in groupedFields do
            emitSection g
            for (_, n,v) in lst do
                sprintf "%s: %s" n v |> emit 2
        
        let reportMethod (mi: MethodDefinition) =
            if mi.IsGetter || mi.IsSetter || mi.IsConstructor then false else true
           
        let methodsToReport = t.Methods |> Seq.filter reportMethod |> Array.ofSeq
        if not (Array.isEmpty methodsToReport) then do
            emitSection "methods" 
            for m in methodsToReport do
                sprintf "- %s -> %s" m.Name (simplifyTypeName m.ReturnType) |> emit 2 

    emitAssemblyMetadata a
    for t in types do
        emitOneType t

let emitAssembliesFromFiles fileNames =
    for f in fileNames do
        try
            parseAssembly f
        with
            | :? System.BadImageFormatException -> printfn "# Bad assembly: %s" f
             
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
