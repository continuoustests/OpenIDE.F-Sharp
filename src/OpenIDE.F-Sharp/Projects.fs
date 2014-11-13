module OpenIDE.FSharp.Projects

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions

type Template =
    | Template of string
    | Invalid

type ProjectFile =
    | ProjectFile of string
    | NotFound

let appRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
let createTemplateRoot = Path.Combine(appRoot, "preserved-data", "create")
let newTemplateRoot = Path.Combine(appRoot, "preserved-data", "new")

let toAbsoulute path =
    if Path.IsPathRooted(path) then path
    else Path.Combine(Environment.CurrentDirectory, path)

let relativeTo source file =
    Uri(source)
     |> fun f -> f.MakeRelativeUri(Uri(file))
     |> fun f -> f.ToString()

let getProjectFiles path = 
    if Directory.Exists(path) then Directory.GetFiles(path, "*.fsproj")
    else [||]

let rec getClosestProject path =
    match path with
     | null -> NotFound
     | (p) when Directory.Exists(p) <> true -> getClosestProject(Path.GetDirectoryName(p))
     | (p) ->
        let projects = getProjectFiles path
        if (projects |> Array.length).Equals(0) then getClosestProject(Path.GetDirectoryName(p))
        else projects |> Seq.head |> ProjectFile

let getNamespaceFor project file =
    let valueExtractor (s: string) =
        let start = s.IndexOf(">")+1
        s.Substring(start, s.LastIndexOf("<") - start)
    let rootNamespace =
        project
         |> File.ReadAllText
         |> fun content -> Regex.Matches(content, "(?ms)\<RootNamespace\>(?<abc>.*?)\</RootNamespace\>")
         |> Seq.cast<Match>
         |> Seq.map (fun m -> m.Value |> valueExtractor)
         |> Seq.head
    let additionalNamespace = 
        file
         |> relativeTo project
         |> fun path -> path.Replace(Path.DirectorySeparatorChar, '.')
         |> Path.GetFileNameWithoutExtension
    rootNamespace+"."+additionalNamespace

let getTemplates root =
    let fetcher path =
        match path with
         | (p) when p.Equals(newTemplateRoot) -> Directory.GetFiles p
         | (p) -> Directory.GetDirectories p
    if Directory.Exists(root) || File.Exists(root) then root |> fetcher
    else [||]

let getTemplate root name =
    let matches =
        root
         |> getTemplates
         |> Array.filter (fun path -> Path.GetFileNameWithoutExtension(path).Equals(name))
    if (matches |> Array.length).Equals(1) then matches.[0] |> Template
    else Invalid

let generateCreateDefinitions () =
    let definition =
        "   {name}|\"F# {name} project\" 
        PATH|\"Path to the project to create\" end 
    end 
"
    createTemplateRoot
     |> getTemplates
     |> Array.map (fun file -> Path.GetFileName(file))
     |> Array.map (fun name -> definition.Replace("{name}", name))
     |> Seq.fold (fun all cmd -> all+cmd) ""

let generateNewDefinitions () =
    let definition =
        "   {name}|\"Creates new {name}\" 
        PATH|\"Path to the file to create (without file extension\" 
            [ADDAFTER]|\"Project referenced file to add the new file after\" end 
        end 
    end 
"
    newTemplateRoot
     |> getTemplates
     |> Array.map (fun file -> Path.GetFileNameWithoutExtension(file))
     |> Array.map (fun name -> definition.Replace("{name}", name))
     |> Seq.fold (fun all cmd -> all+cmd) ""

let createFromTemplate name path =
    let writeFile (file, content) = File.WriteAllLines(file, content)

    let translateFileName (file, name) =
        if Path.GetFileName(file).Equals("template.project") then Path.Combine(Path.GetDirectoryName(file), name+".fsproj")
        else file

    let translate (content: string[], projectName, projectGuid) =
        content
         |> Array.map (fun line -> 
            line.Replace("{{project-name}}", projectName).Replace("{{project-guid}}", projectGuid.ToString()))

    let projectName = Path.GetFileName(path)
    let projectGuid = Guid.NewGuid()
    let target = toAbsoulute path
    if Directory.Exists(target) <> true then Directory.CreateDirectory(target) |> ignore
    match (getTemplate createTemplateRoot name) with
     | Template(path) -> 
        Directory.GetFiles(path)
         |> Array.map (fun file -> (Path.Combine(target, Path.GetFileName(file)), File.ReadAllLines(file)))
         |> Array.map (fun (file, content) -> 
            writeFile(translateFileName(file, projectName), translate(content, projectName, projectGuid))
            file)
         |> Array.filter (fun file -> Path.GetFileName(file).Equals("Program.fs"))
         |> Array.iter (fun file -> printfn "command|editor goto \"%s|1|1\"" file)
     | Invalid -> printfn "error|%s is not a valid project template" name

let newFromTemplate name path addAfter =
    match (getTemplate newTemplateRoot name) with
     | Invalid -> printfn "error|%s is not a valid file template" name
     | Template(template) -> 
        let file = path+".fs" |> toAbsoulute
        match (getClosestProject file) with
         | NotFound -> printfn "error|Could not find project to add file to"
         | ProjectFile(project) ->
            // Write new file to disk
            template
             |> File.ReadAllLines
             |> Array.map (fun line -> line.Replace("{{namespace}}", getNamespaceFor project file))
             |> fun lines -> File.WriteAllLines(file, lines)
            printfn "command|editor goto \"%s|1|1\"" file

            // Update Project file with new file reference
            project
             |> File.ReadAllLines
             |> fun a ->
                let addAfterLine =
                    a
                     |> Seq.mapi (fun i line -> (i, line))
                     |> Seq.filter (fun (i, line) -> line.Trim().StartsWith("<Compile Include=\""+addAfter+"\""))
                     |> Seq.map (fun (i, line) -> i)
                     |> Seq.head
                match addAfterLine with
                 | 0 -> a
                 | line ->
                    let first =
                        Array.append a.[..line] [|"    <Compile Include=\""+(relativeTo project file)+"\" />"|]
                    Array.append first a.[(line+1)..]
             |> fun lines -> File.WriteAllLines(project, lines)

let deleteAndRemove path =
    let file = path |> toAbsoulute
    match (getClosestProject file) with
     | NotFound -> ()
     | ProjectFile(project) ->
        let relativeFile = relativeTo project file
        project
             |> File.ReadAllLines
             |> Seq.filter (fun line -> line.Trim().StartsWith("<Compile Include=\""+relativeFile+"\"") <> true)
             |> fun lines -> File.WriteAllLines(project, lines)
    if File.Exists(file) then File.Delete(file)
    else ()

let newProject args =
    let argCount = args |> Array.length
    match args with
     | (a) when argCount.Equals(2) -> createFromTemplate args.[0] args.[1] |> ignore
     | (a) -> printfn "error|Invalid arguments"

let newFile args =
    let argCount = args |> Array.length
    match args with
     | (a) when argCount.Equals(2) -> newFromTemplate args.[0] args.[1] "AssemblyInfo.fs" |> ignore
     | (a) when argCount.Equals(3) -> newFromTemplate args.[0] args.[1] args.[2] |> ignore
     | (a) -> printfn "error|Invalid arguments"

let deleteFile file =
    deleteAndRemove file