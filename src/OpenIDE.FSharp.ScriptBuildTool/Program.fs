module OpenIDE.FSharp.ScriptBuildTool.Main

open System
open System.IO
open System.Reflection
open System.Diagnostics

let getRoot (path) =
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let hasUpdated (path, sources) =
    let file = Path.Combine(getRoot(), path)
    if File.Exists(file) then
        let updated (source, target) = 
            let infoSource = new FileInfo(source)
            let infoTarget = new FileInfo(target)
            infoTarget.LastWriteTimeUtc > infoSource.LastWriteTimeUtc
        let updatedCount =
            sources
             |> Array.map (fun itm -> Path.Combine(getRoot(), itm))
             |> Array.filter (fun itm -> File.Exists(itm))
             |> Array.map (fun itm -> if updated(file, itm) then 1 else 0)
             |> Array.sum
        updatedCount > 0
    else true

let build () =
    let i = 0
    let fsc =
        i.GetType().Assembly.Location
        |> Path.GetDirectoryName
        |> Path.GetDirectoryName
        |> fun(root) -> Path.Combine(root, "4.0", "fsc.exe")

    let nameParser(root: string) =
        root
            |> Path.GetFileName
            |> fun(name) -> name.Substring(0, name.Length-6)

    let (root, filename) = 
        let root = getRoot()
        root, nameParser(root).ToString()

    let script = Path.Combine(root, filename+".fsx")
    let output = Path.Combine(root, filename+".exe")

    let proc = new Process()
    let info = new ProcessStartInfo(fsc, "\"--out:"+output+"\" --target:exe \""+script+"\"")
    info.CreateNoWindow <- true
    info.WindowStyle <- ProcessWindowStyle.Hidden
    info.WorkingDirectory <- Environment.CurrentDirectory
    proc.StartInfo <- info
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true
    proc.Start() |> ignore
    proc.StandardOutput.ReadToEnd().Trim().Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.filter (fun(line) -> line.Contains("error") || line.Contains("fatal"))
        |> Array.map (fun(line) -> Console.WriteLine("error|"+line))
        |> ignore
    proc.StandardError.ReadToEnd().Trim().Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun(line) -> Console.WriteLine("error|"+line))
        |> ignore
    proc.WaitForExit()

let isUpdated (x: string[]) =
    match x with
     | (args) when args.Length > 1 -> hasUpdated(args.[0], args.[1..])
     | (args) -> true

let runBuild (args: string[]) =
    if isUpdated(args) then build()

[<EntryPoint>]
let main (args: string[]) = 
    runBuild(args)
    0