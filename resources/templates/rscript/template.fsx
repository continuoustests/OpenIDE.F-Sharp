open System

let printReactPatterns () =
    printfn "'hello-event'"

let handleEvent (event, globalProfile, localProfile) =
    printfn "handling event %s" event

[<EntryPoint>]
let main args = 
    match args with
     | (a) when a.Length.Equals(1) &&  a.[0].Equals("reactive-script-reacts-to") -> printReactPatterns()
     | (a) when a.Length.Equals(3) -> handleEvent(a.[0], a.[1], a.[2])
     | (a) -> ()
    0