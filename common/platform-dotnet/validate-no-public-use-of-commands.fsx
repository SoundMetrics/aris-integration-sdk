open System
open System.IO
open System.Reflection

(*

    This script ensures that the protobuf message types defined in SoundMetrics.Aris.Messages
    are not used in public APIs in this solution.

    The message types themselves are not dangerous in any way, but using them in the public APIs
    will leave consumers of the APIs vulnerable to changes in the protocols.

*)

module Seq =

    let dump name (ts : 'T seq) : 'T seq =

        let ts' = ts |> Seq.cache
        ts' |> Seq.iter (fun t -> printfn "  %s: %A" name t)
        ts'

module Details =
    open System.Text.RegularExpressions

    type PackageInfo = {
        Name : string
        AltLocation : string option // Because there's a misnamed nuget package.
    }
    with
        static member Of name = { Name = name; AltLocation = None }
        static member Of(name, altLocation) = { Name = name; AltLocation = Some altLocation }

    // We preload some assemblies so they can be resolved when others are loaded
    let private preloadPackages = [
        PackageInfo.Of("SoundMetrics.Aris.Headers")
        PackageInfo.Of("System.Threading.Tasks.Dataflow")
        PackageInfo.Of("fracas.standard", "fracas")
        PackageInfo.Of("System.Reactive")
        PackageInfo.Of("Google.Protobuf")
    ]

    let preloadAssemblies () =

        let findPackageFolders name =
            let re = Regex(name + "\.\d+\.\d+\.\d+.*") // Don't let "a.b.1.2.3" match "a.b.c.1.1.1"
            let isMatchingFolder = re.IsMatch

            Directory.EnumerateDirectories(@".\packages", name + "*")
            |> Seq.filter isMatchingFolder
            |> Seq.dump "package folders"

        let findAssemblyInTree name folder =
            let rec iter name folders =
                match folders with
                | folder :: tail ->
                    let path = Path.Combine(folder, name + ".dll")
                    if File.Exists(path) then
                        Some path
                    else
                        let more = Directory.EnumerateDirectories(folder) |> Seq.toList
                        iter name (more @ folders)
                | [] -> None
            iter name [folder]

        let preload name folder =
            match findAssemblyInTree name folder with
            | Some path ->
                let assm = Assembly.LoadFrom(path)
                printfn "    preloaded %s" path
                Some assm
            | None -> failwithf "Couldn't find an assembly for %s" name

        printfn "Preloading assemblies..."

        preloadPackages |> Seq.map (fun info ->
                            let name =  match info.AltLocation with
                                        | Some loc -> loc
                                        | None -> info.Name
                            info, findPackageFolders name)
                        |> Seq.collect (fun (info, folders) ->
                            folders |> Seq.map (fun folder -> preload info.Name folder)
                        )
                        |> Seq.choose id
                        |> Seq.toList // reify to cause iteration

    // Class type provides overloading for a IsPublic method
    [<Sealed>]
    type Public =
        static member Test (i : ConstructorInfo) = i.IsPublic
        static member Test (i : EventInfo) = i.AddMethod.IsPublic // If not, public can't use this property
        static member Test (i : FieldInfo) = i.IsPublic
        static member Test (i : MethodInfo) = i.IsPublic
        static member Test (i : PropertyInfo) =
            let getter = i.GetGetMethod()
            let setter = i.GetSetMethod()

            (isNull getter || getter.IsPublic) && (isNull setter || setter.IsPublic)

    // Class type provides overloading for a 'find the bad stuff' method
    [<Sealed>]
    type Check (prohibitedTypes : Type seq) =

        let returnBadActors (i : #MemberInfo) (bads : Type seq) =
            if bads |> Seq.length > 0
                then seq { yield (i :> MemberInfo, bads) }
                else Seq.empty

        member __.BadActors (i : ConstructorInfo) : (MemberInfo * Type seq) seq =

            let isBad (pi : ParameterInfo) = prohibitedTypes |> Seq.contains pi.ParameterType
            let badParams = i.GetParameters() |> Seq.filter isBad
                                              |> Seq.map (fun pi -> pi.ParameterType)
                                              |> Seq.cache
            returnBadActors i badParams

        member __.BadActors (i : EventInfo) : (MemberInfo * Type seq) seq =

            let isBad (pi : ParameterInfo) = prohibitedTypes |> Seq.contains pi.ParameterType
            let badParams = i.EventHandlerType.GetMethod("Invoke").GetParameters()
                                |> Seq.filter isBad
                                |> Seq.map (fun pi -> pi.ParameterType)
                                |> Seq.cache
            returnBadActors i badParams

        member __.BadActors (i : FieldInfo) : (MemberInfo * Type seq) seq =

            let isBad (t : Type) = prohibitedTypes |> Seq.contains t
            let badTypes = i.FieldType |> Seq.singleton
                                       |> Seq.filter isBad
                                       |> Seq.cache
            returnBadActors i badTypes

        member __.BadActors (i : MethodInfo) : (MemberInfo * Type seq) seq =

            let isBad (pi : ParameterInfo) = prohibitedTypes |> Seq.contains pi.ParameterType
            let badParams = i.GetParameters()
                                |> Seq.filter isBad
                                |> Seq.map (fun pi -> pi.ParameterType)
                                |> Seq.cache
            let badReturn = if prohibitedTypes |> Seq.contains i.ReturnType
                                then i.ReturnType |> Seq.singleton
                                else Seq.empty
            returnBadActors i (Seq.concat [badParams; badReturn])

        member __.BadActors (i : PropertyInfo) : (MemberInfo * Type seq) seq =

            let isBad (pi : PropertyInfo) = prohibitedTypes |> Seq.contains pi.PropertyType
            let badTypes = i |> Seq.singleton
                             |> Seq.filter isBad
                             |> Seq.map (fun pi -> pi.PropertyType)
                             |> Seq.cache
            returnBadActors i badTypes


let assembliesOfInterest = [
    "SoundMetrics.Aris.Comms"
    "SoundMetrics.Aris.Config"
    "SoundMetrics.Aris.FrameHeaderInjection"
    //"SoundMetrics.Aris.Messages" We don't check this one, it's the subject of this script.
    "SoundMetrics.Aris.PaletteShader"
    "SoundMetrics.Aris.ReorderCS"
    "SoundMetrics.NativeMemory"
    "SoundMetrics.Scripting"

    //"SoundMetrics.Scripting.Desktop" 
    // System.IO.FileLoadException: Cannot resolve dependency to assembly 'FSharp.Core...'
    // It only exposes SoundMetrics.Scripting with dispatch, so skipping this should be okay.
]

let getPublicTypes assemblyPath =

    let isPublicType (typeInfo : TypeInfo) =
        typeInfo.IsPublic // TODO public enclosing type?

    let publicTypes =
        try
            let assm = Assembly.LoadFrom(assemblyPath)
            assm.DefinedTypes |> Seq.filter isPublicType
        with
            | :? ReflectionTypeLoadException as ex ->
                ex.Types |> Seq.filter (fun t -> not (isNull t))
                         |> Seq.map (fun t -> t.GetTypeInfo())
            | :? FileLoadException as ex ->
                eprintfn "Couldn't load file '%s'" ex.FileName
                reraise()
        |> Seq.filter (fun ti -> ti.Name <> "<>c")
        |> Seq.cache

    //printfn "Public types for %s" (Path.GetFileName(assemblyPath))
    //publicTypes |> Seq.iter (fun ti ->
    //                printfn "  %s %s %A"
    //                    (ti.Assembly.GetName().Name) ti.Name ti)
    publicTypes

// Assembly build locations can vary (.NET vs dotnet Standard)
let findAssemblyPath assemblyName : string option =
    let startFolder = Path.Combine(assemblyName, "bin", "Release")
    let targetFile = assemblyName + ".dll"

    let rec checkForFile folder =

        let dllPath = Path.Combine(folder, targetFile)
        if File.Exists(dllPath) then
            Some dllPath
        else
            let subFolders = Directory.EnumerateDirectories(folder)
            subFolders |> Seq.map checkForFile |> Seq.tryPick id

    checkForFile startFolder

let membersToIgnore =
    [
        "ToString"
        "Equals"
        "GetHashCode"
        "GetType"
        "Invoke"
    ] |> List.toSeq |> Set

let findProhibitedTypes (prohibitedTypes : TypeInfo seq) (subjectType : TypeInfo) 
        : (TypeInfo * MemberInfo * Type seq) seq =

    let filterMembers (m : MemberInfo) = not (membersToIgnore |> Set.contains m.Name)

    let prohibitedTypes' = prohibitedTypes |> Seq.map (fun ti -> ti.AsType()) |> Seq.cache
    let check = Details.Check(prohibitedTypes')

    let addSubjectType (mi, bads) = (subjectType, mi, bads)

    seq {
        yield! subjectType.GetConstructors() |> Seq.filter Details.Public.Test
                                             |> Seq.filter filterMembers
                                             |> Seq.collect check.BadActors
                                             |> Seq.map addSubjectType
        yield! subjectType.GetEvents() |> Seq.filter Details.Public.Test
                                       |> Seq.filter filterMembers
                                       |> Seq.collect check.BadActors
                                       |> Seq.map addSubjectType
        yield! subjectType.GetFields() |> Seq.filter Details.Public.Test
                                       |> Seq.filter filterMembers
                                       |> Seq.collect check.BadActors
                                       |> Seq.map addSubjectType
        yield! subjectType.GetMethods() |> Seq.filter Details.Public.Test
                                        |> Seq.filter filterMembers
                                        |> Seq.collect check.BadActors
                                        |> Seq.map addSubjectType
        yield! subjectType.GetProperties() |> Seq.filter Details.Public.Test
                                           |> Seq.filter filterMembers
                                           |> Seq.collect check.BadActors
                                           |> Seq.map addSubjectType
    }

// TODO also check inherited types & interfaces

let performTest () =

    let assemblyNameToPath assemblyName =
        match findAssemblyPath assemblyName with
        | Some path ->
            printfn "assemblyNameToPath: %s -> %s" assemblyName path
            path
        | None -> failwithf "Couldn't find DLL for %s" assemblyName

    Details.preloadAssemblies() |> ignore

    let prohibitedTypes = assemblyNameToPath "SoundMetrics.Aris.Messages"
                            |> getPublicTypes
                            |> Seq.cache

    let results =
        seq {
            for assm in assembliesOfInterest do
                printfn "Checking assembly %s" assm
                yield!
                    assm |> assemblyNameToPath
                         |> getPublicTypes
                         |> Seq.collect (findProhibitedTypes prohibitedTypes)
        }
        |> Seq.toList

    printfn "%s" (String('-', 80))
    printfn "%d errors found" results.Length

    if results.Length > 0 then
        for (containingTypeInfo, mi, badTypes) in results do
            for bt in badTypes do
                printfn "%s %s uses %s" containingTypeInfo.Name mi.Name bt.Name

    results.Length

// Get only the args after the first "--"
let getProgramArgs args =

    let rec findDelim argList =
        match argList with
        | "--" :: rem ->    rem |> List.toArray
        | _ :: rem ->       findDelim rem
        | [] ->             Array.empty

    findDelim (args |> Array.toList)

// Get the root folder and output file from arguments.
let parseArgs () =
    // Get only the args that apply to the program.
    let args = getProgramArgs fsi.CommandLineArgs

    match args with
    | [| folder |] -> Ok folder
    | [||] ->         Ok ""
    | _ ->            Error "Too many arguments"

let run () =

    match parseArgs() with
    | Ok "" ->
        let defaultFolder = @"S:\git\aris-integration-sdk\common\platform-dotnet\"
        printfn "Using default folder: %s" defaultFolder
        Environment.CurrentDirectory <- defaultFolder

    | Ok folder ->
        printfn "Using provided folder: %s" folder
        Environment.CurrentDirectory <- folder
    | Error msg ->  eprintfn "ERROR: %s" msg

    printfn "cwd: %s" (Environment.CurrentDirectory)

    let errorCount =
        try
            performTest()
        with
            ex -> eprintfn "%A" ex
                  -1

    if errorCount <> 0 then
        let exitCode = 1
        printfn "*** FAILED ***"
        Environment.Exit(exitCode)
        //Environment.ExitCode <- exitCode

run()
