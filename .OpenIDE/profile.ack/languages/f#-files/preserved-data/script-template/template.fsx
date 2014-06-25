open System

let printDefinitions () =
    printfn "Script description"

let runScript (runLocation, globalProfile, localProfile, args) =
    printfn "Hello F# script"

[<EntryPoint>]
let main args = 
    match args with
     | (a) when a.Length.Equals(1) &&  a.[0].Equals("get-command-definitions") -> printDefinitions()
     | (a) when a.Length >= 3 -> runScript(a.[0], a.[1], a.[2], (if a.Length > 3 then a.[3..] else [||]))
     | (a) -> ()
    0