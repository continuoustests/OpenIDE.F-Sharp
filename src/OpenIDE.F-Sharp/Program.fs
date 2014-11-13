module OpenIDE.FSharp.Main

open System

type Command =
    | GetCommandDefinitions
    | GetCrawlFileTypes
    | CrawlSource of string
    | NewScript of string[]
    | NewRScript of string[]
    | Create of string[]
    | New of string[]
    | Delete of string
    | None

let commandStringToArguments (cmd: string) =
    // Checks whether a quote is lead by an escape char
    let isUnescapedQuote (i, c) =
        c.Equals('"') && (i.Equals(0) || (cmd |> Seq.toArray).[i-1] <> '\\')
    // Get the start and end position of quoted strings
    let quotedStrings =
        cmd
         |> Seq.toArray
         |> Array.mapi (fun i c -> (i, c))
         |> Array.filter (fun point -> isUnescapedQuote point)
         |> Array.map (fun (i, c) -> i)
         |> Seq.pairwise
         |> Seq.toArray
         |> Array.map (fun (s, e) -> (s+1, e-1))
    // Checks if current position is inside a quoted string
    let outsideQuotedString i =
        let count =
            quotedStrings
             |> Array.filter (fun (sstart, send) -> i >= sstart && i <= send)
             |> Array.length
        count.Equals(0)
    // Locate all unquoted words
    let words =
        cmd
         |> Seq.toArray
         |> Array.mapi (fun i c -> (i+1, c))
         |> Array.filter (fun (i, c) -> c.Equals(' '))
         |> Array.map (fun (i, c) -> i)
         |> Array.filter (fun i -> outsideQuotedString i)
         |> Array.append [|0|]
    // Get start positions for all commands in command string
    let startPositions =
        words
         |> Array.append (quotedStrings |> Array.map (fun (start, _) -> start))
         |> Array.sort
         |> Seq.distinct
         |> Seq.toArray
    // Parse out a list of commands from start and end position
    startPositions
     |> Array.mapi (fun i p ->
        if i.Equals((startPositions |> Array.length)-1) then (p, cmd.Length)
        else (p, startPositions.[i+1]-1))
     |> Array.map (fun (s, e) -> cmd.Substring(s, e-s))

let commandDefinitions =
    let definitions = "
[[script]]|\"\" 
    [[new]]|\"\" 
        fsharp|\"Create a new f# script\" 
            NAME|\"Command name\" end 
        end 
    end 
end 
[[rscript]]|\"\" 
    [[new]]|\"\" 
        fsharp|\"Create a new f# reactive script\" 
            NAME|\"Command name\" end 
        end 
    end 
end
create|\"Creates a new F# project\"
{create-commands}
end 
new|\"New templated file\"
{new-commands}
end
deletefile|\"Delete file from disk\" 
    PATH|\"Path to file to delete\" end 
end "
    definitions
        .Replace("{create-commands}", OpenIDE.FSharp.Projects.generateCreateDefinitions())
        .Replace("{new-commands}", OpenIDE.FSharp.Projects.generateNewDefinitions())

let hasMore (a: string[], length, items: string[]) =
    let itemLength = items |> Array.length
    if (a |> Array.length) >= itemLength then
        let count =
            a.[..(itemLength-1)]
             |> Array.mapi (fun index itm -> itm.Equals(items.[index]))
             |> Array.filter (fun matched -> matched)
             |> Array.length
        a.Length > length && count.Equals(itemLength)
    else false

let has (a: string[], length, items: string[]) =
    if hasMore (a, length-1, items) then a.Length.Equals(length)
    else false

let getCommand arguments =
    match arguments with
     | (a) when has(a, 1, [|"get-command-definitions"|]) -> GetCommandDefinitions
     | (a) when has(a, 1, [|"crawl-file-types"|]) -> GetCrawlFileTypes
     | (a) when has(a, 2, [|"crawl-source"|]) -> a.[1] |> CrawlSource
     | (a) when hasMore(a, 3, [|"script"; "new"; "fsharp";|]) -> a.[3..] |> NewScript
     | (a) when hasMore(a, 3, [|"rscript"; "new"; "fsharp";|]) -> a.[3..] |> NewRScript
     | (a) when hasMore(a, 2, [|"create"|]) -> a.[1..] |> Create
     | (a) when hasMore(a, 2, [|"new"|]) -> a.[1..] |> New
     | (a) when hasMore(a, 1, [|"deletefile"|]) -> a.[1] |> Delete
     | _ -> None

let runCommand cmd =
    match cmd with
     | GetCommandDefinitions -> printfn "%s" commandDefinitions
     | GetCrawlFileTypes -> printfn ".fs|.fsproj"
     | CrawlSource(path) -> printfn "Crawling %s" path
     | NewScript(args) -> OpenIDE.FSharp.Scripts.newScript args
     | NewRScript(args) -> OpenIDE.FSharp.Scripts.newRScript args
     | Create(args) -> OpenIDE.FSharp.Projects.newProject args
     | New(args) -> OpenIDE.FSharp.Projects.newFile args
     | Delete(file) -> OpenIDE.FSharp.Projects.deleteFile file
     | None -> ()

let initializedHandler path =
    let mutable keepAlive = true
    printfn "initialized"
    while keepAlive do
        let args = Console.ReadLine() |> commandStringToArguments
        if has(args, 1, [|"shutdown"|]) then keepAlive <- false
        else getCommand args |> runCommand
        printfn "end-of-conversation"

[<EntryPoint>]
let main args = 
    if has(args, 2, [|"initialize"|]) then args.[1] |> initializedHandler
    else getCommand args |> runCommand
    0