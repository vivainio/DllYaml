// Learn more about F# at http://fsharp.org

open Mono.Cecil
open System
open System.Text.Json

let simplifyCollectionName name =
    match name with
    | "Nullable`1" -> "?"
    | "IEnumerable`1" -> "seq"
    | "ICollection`1" -> "coll"
    | "ObservableCollection`1" -> "ocoll"
    | "Task`1" -> "task"
    | "List`1" -> "list"
    | "Dictionary`2" -> "dict"
    | _ -> name
    
    
let rec simplifyGenericTypeArgs (args: TypeReference seq) =
    args
    |> Seq.map (fun a -> simplifyTypeName a)
    |> String.concat ", "

and simplifyTypeName (t: TypeReference) =

    match t with
    
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
         sprintf "%s(%s)" attr.AttributeType.Name
             (attr.ConstructorArguments
              |> Seq.map (fun a -> a.Value.ToString())
              |> String.concat ",")
        
let attributesToString (attrs: CustomAttribute seq) =
    String.concat " " (attrs |> Seq.map oneAttributeToString)

let parseAssembly (f: string) =
    
    let a = AssemblyDefinition.ReadAssembly (f)
    let types = a.MainModule.Types |> Seq.sortBy (fun t -> t.FullName)
    
        
    let nest n = (String.replicate n "  ") 
    printfn ".meta:"
    printfn "%sname: %s" (nest 1) a.MainModule.Assembly.FullName
    printfn "%s.attr:" (nest 1)
    for attr in a.MainModule.Assembly.CustomAttributes do
        printfn "%s- %s" (nest 2) (oneAttributeToString attr)
    
    for t in types do
        
        let kindText = kindInd t
        
        printfn "%s:" t.FullName
    
        if kindText <> "" then do
            printfn "%s.t: %s" (nest 1) kindText
        
        if t.HasInterfaces then do
            printfn "%s.implements:" (nest 1)
            for iface in t.Interfaces do
                printfn "%s- %s" (nest 2) (simplifyTypeName iface.InterfaceType)
        
        if t.BaseType <> null && t.BaseType.Name <> "Object" then do
            printfn "%s.base: %s" (nest 1) (simplifyTypeName t.BaseType)
            
        if t.HasCustomAttributes then do
            printfn "%s.attr: %s" (nest 1) (attributesToString t.CustomAttributes)
        
        if t.HasProperties then do
            printfn "%s.prop:" (nest 1)            
            for p in t.Properties do           
                printfn "%s%s: %s" (nest 2) p.Name (simplifyTypeName p.PropertyType)

        
        let fieldSpec (f: FieldDefinition) =
            match f with
            | _ when f.HasConstant -> ("const", f.Name, f.Constant.ToString())
            | _ when f.IsStatic -> ("static", f.Name, simplifyTypeName f.FieldType)
            | _ when f.IsPublic -> ("public", f.Name, simplifyTypeName f.FieldType)
            | _ when f.IsPrivate -> ("private", f.Name, simplifyTypeName f.FieldType)

            | _ -> ("other", f.Name, f.ToString())
                  
        let groupedFields = t.Fields |> Seq.map fieldSpec |> Seq.groupBy (fun (a,_,_) -> a)
            
        for (g, lst) in groupedFields do
            printfn "%s.%s:" (nest 1) g
            for (_, n,v) in lst do
                printfn "%s%s: %s" (nest 2) n v
        
        let reportMethod (mi: MethodDefinition) =
            if mi.IsGetter || mi.IsSetter || mi.IsConstructor then false else true
           
        let methodsToReport = t.Methods |> Seq.filter reportMethod |> Array.ofSeq
        if not (Array.isEmpty methodsToReport) then do
            printfn "%s.m:" (nest 1)
            for m in methodsToReport do
                printfn "%s - %s %s" (nest 2) m.Name (simplifyTypeName m.ReturnType) 

                       
                

[<EntryPoint>]
let main argv =
    
    for f in argv do
        printfn "# %s" f
    
        parseAssembly f
        printfn "\n...\n"
    0 // return an integer exit code
