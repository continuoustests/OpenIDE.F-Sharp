module OpenIDE.FSharp.Scripts

open System
open System.IO
open System.Reflection
open System.Diagnostics

let pluginRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let queryProcess executable arguments =
    let startInfo = ProcessStartInfo(executable, arguments)
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true;
    let p = Process.Start(startInfo)
    let lines = p.StandardOutput.ReadToEnd().Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
    p.WaitForExit()
    lines

let getScriptRootFor profilType scriptType =
    queryProcess "oi" "profile list"
     |> Array.map (fun line -> line.Split([|"|"|], StringSplitOptions.RemoveEmptyEntries))
     |> Array.filter (fun line -> line.[0].Equals("active-"+profilType))
     |> Array.map (fun line -> line.[2])
     |> Seq.head
     |> fun root -> Path.Combine(root, scriptType)

let prepareScript scriptType profileType name =
    let scriptRoot = (getScriptRootFor profileType scriptType+"s")
    let scriptFiles = Path.Combine(scriptRoot, name+"-files")
    if Directory.Exists(scriptRoot) <> true then Directory.CreateDirectory(scriptRoot) |> ignore
    let commandExists = 
        (Directory.GetFiles(scriptRoot, (name+".*")) |> Array.length) > 0 || Directory.Exists(scriptFiles)
    if commandExists then printfn "error|A %s with the same command name already exists" scriptType
    else
        let templateRoot = Path.Combine(pluginRoot, "preserved-data", scriptType+"-template")
        if Directory.Exists(scriptFiles) <> true then Directory.CreateDirectory(scriptFiles) |> ignore
        [|("template.fsx", name+".fsx");("build.exe", "build.exe");|]
         |> Array.map (fun (target, destination) -> (Path.Combine(templateRoot, target), Path.Combine(scriptFiles, destination)))
         |> Array.map File.Copy
         |> ignore
        Path.Combine(templateRoot, "template.oilnk")
         |> File.ReadAllLines
         |> Array.map (fun line -> line.Replace("{{name}}", name))
         |> fun lines -> File.WriteAllLines(Path.Combine(scriptRoot, name+".oilnk"), lines)
        printfn "command|editor goto \"%s|1|1\"" (Path.Combine(scriptRoot, name+".oilnk"))
        printfn "command|editor goto \"%s|1|1\"" (Path.Combine(scriptFiles, name+".fsx"))

let parseProfile args =
    if args |> Array.exists (fun itm -> itm.Equals("-g")) then "global"
    else "local"

let newScript args =
    let profile = parseProfile args
    prepareScript "script" profile args.[0]

let newRScript args =
    let profile = parseProfile args
    prepareScript "rscript" profile args.[0]