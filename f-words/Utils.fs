module fwords.Utils

open System.Text.RegularExpressions
open System
open System.Globalization
open fwords.Types
open System.Collections.Generic

let inline (=>) a b = a, box b

let isError = function | Ok _ -> None | Error e -> Some e

let isNotError = function | Ok r -> Some r | Error _ -> None

let isEmpty<'a> (arr: IList<'a>) = arr.Count = 0

let formatPageError pageName msg =
    sprintf "  %s:\n%s" pageName msg

let formatOutput msg =
    sprintf "  - %s" msg

let split (sep: char) (s:string) =
    s.Split ([|sep|], StringSplitOptions.RemoveEmptyEntries)

let checkAllOk results =
    let errors = results |> Seq.choose isError
    let errorCount = Seq.length errors
    match errorCount with
    | 0 -> Ok (results |> Seq.choose isNotError)
    | _ -> Error errors

let tryMatch (matcher: Regex) (input: string) =
    let m = matcher.Match input
    match m.Success with
    | true -> (Some m)
    | false -> (None)

let replace (p: Regex) (r: string) (s: string) =
    p.Replace (s, r)

let parseiso8601date err s =
    let (parse, dt) = DateTime.TryParseExact (s, "yyyy-MM-dd HH:mm", null, DateTimeStyles.None)
    match parse with
    | false -> err
    | true -> Ok dt

let asConfigValue (s: string) =
    let idx = s.IndexOf(':')
    let key = s.Substring (0, idx)
    let value = s.Substring (idx + 1)
    (key.Trim(), value.Trim())

let asErrors (l: string list) =
    Error (l |> Seq.ofList)

let loadConfig fileExists readLines fileName =
    try
        let exists = fileName |> fileExists
        match exists with
        | false ->
            let msg = sprintf "Couldn't find a configuration at %s" fileName
            [formatOutput msg] |> asErrors
        | true ->
            let configValues =
                fileName
                |> readLines
                |> Seq.map asConfigValue
                |> Map.ofSeq
            Ok configValues
    with
        | ex -> [ex.Message] |> asErrors

let requiredConfigKeys = [
    "site_name"
    "site_url"
    "rss_description"
    "template_path"
    "content_path"
    "output_path"
]

let validateConfig (cfg: Map<string, string>) =
    requiredConfigKeys
    |> Seq.filter (fun k -> not (cfg.ContainsKey k))
    |> Seq.fold (fun msg k -> k::msg) []
    |> (fun s -> match s.Length with
                 | 0 -> Ok cfg
                 | _ -> Seq.rev s
                        |> Seq.map (sprintf "Missing configuration field: %s")
                        |> Seq.map formatOutput
                        |> Seq.toList
                        |> asErrors)

let createPaths basePath (cfg: Map<string, string>) =
    let combine = sprintf "%s\%s"

    let isRelative (path: string) =
        not (path.Contains @":\")

    let templatePath = cfg.["template_path"]
    let contentPath = cfg.["content_path"]
    let outputPath = cfg.["output_path"]

    {
        Base_Path = basePath
        Template_Path = if templatePath |> isRelative then combine basePath templatePath else templatePath
        Content_Path = if contentPath |> isRelative then combine basePath contentPath else contentPath
        Output_Path = if outputPath |> isRelative then combine basePath outputPath else outputPath
    }

let validatePaths directoryExists (paths: SitePaths) =
    let error pathType =
        sprintf "%s path %s not found" pathType

    let pathChecks = [
        (paths.Template_Path, (error "Template"))
        (paths.Content_Path, (error "Content"))
        (paths.Output_Path, (error "Output"))
    ]

    let results =
        pathChecks
        |> Seq.map (fun (pth, err) ->
            match (pth |> directoryExists) with
            | false -> Error (formatOutput (err pth))
            | true -> Ok pth)

    match (results |> checkAllOk) with
    | Ok _ -> Ok paths
    | Error errors -> Error errors


type ResultBuilder() =
    member __.Return(x) = Ok x
    member __.ReturnFrom(m: Result<_, _>) = m
    member __.Bind(m, f) = Result.bind f m

let result = ResultBuilder()
