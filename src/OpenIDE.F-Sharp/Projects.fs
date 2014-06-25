module OpenIDE.FSharp.Projects

open System
open System.IO
open System.Reflection

type Template =
    | Template of string
    | Invalid

let createTemplateRoot = 
    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "preserved-data", "create")

let getCreateTemplates () =
    if Directory.Exists(createTemplateRoot) then createTemplateRoot |> Directory.GetDirectories
    else [||]

let getCreateTemplate name =
    let templates = getCreateTemplates()
    let matches =
        templates
         |> Array.filter (fun path -> Path.GetFileName(path).Equals(name))
    if (matches |> Array.length).Equals(1) then matches.[0] |> Template
    else Invalid

let generateCreateDefinitions () =
    let definition =
        "   {name}|\"F# {name} project\" 
        PATH|\"Path to the project to create\" end 
    end 
"
    getCreateTemplates()
     |> Array.map (fun file -> Path.GetFileName(file))
     |> Array.map (fun name -> definition.Replace("{name}", name))
     |> Seq.fold (fun all cmd -> all+cmd) ""

let writeFile (file, content) =
    File.WriteAllLines(file, content)

let translateFileName (file, name) =
    if Path.GetFileName(file).Equals("template.project") then Path.Combine(Path.GetDirectoryName(file), name+".fsproj")
    else file

let translate (content: string[], projectName, projectGuid) =
    content
     |> Array.map (fun line -> 
        line.Replace("{{project-name}}", projectName).Replace("{{project-guid}}", projectGuid.ToString()))

let createFromTemplate name path =
    let projectName = Path.GetFileName(path)
    let projectGuid = Guid.NewGuid()
    let target =
        if Path.IsPathRooted(path) then path
        else Path.Combine(Environment.CurrentDirectory, path)
    if Directory.Exists(target) <> true then Directory.CreateDirectory(target) |> ignore
    match (name |> getCreateTemplate) with
     | Template(path) -> 
        Directory.GetFiles(path)
         |> Array.map (fun file -> (Path.Combine(target, Path.GetFileName(file)), File.ReadAllLines(file)))
         |> Array.map (fun (file, content) -> 
            writeFile(translateFileName(file, projectName), translate(content, projectName, projectGuid))
            file)
         |> Array.filter (fun file -> Path.GetFileName(file).Equals("Program.fs"))
         |> Array.iter (fun file -> printfn "command|editor goto \"%s|1|1\"" file)
     | Invalid -> printfn "error|%s is not a valid project template" name

let newProject args =
    let argCount = args |> Array.length
    match args with
     | (a) when argCount.Equals(2) -> createFromTemplate args.[0] args.[1] |> ignore
     | (a) -> printfn "error|Invalid arguments"
