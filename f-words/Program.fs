open System
open fwords.Types
open fwords.Utils
open fwords.Templating
open System.IO
open fwords.Syndication
open System.Text

let encoding = UTF8Encoding false

let fullPath (path: string) =
    Path.GetFullPath (path.TrimEnd([|'\\'|]))

let fileExists path =
    File.Exists path

let readAll path =
    File.ReadAllText path

let readLines path =
    File.ReadAllLines path

let getFileNameWithoutExtension (path: string) =
    Path.GetFileNameWithoutExtension path

let getFilesInDirectory pattern path =
    Directory.GetFiles (path, pattern)

let directoryExists path =
    Directory.Exists path

let writeToFile outputFolder content =
    File.WriteAllText (outputFolder, content, encoding)

let writePageHtml outputFolder metaData html =
    writeToFile (sprintf "%s\%s" outputFolder metaData.Output_FileName) html

let deleteExisting filter path =
    try
        path |> getFilesInDirectory filter |> Array.iter File.Delete
        Ok ()
    with
    | ex -> [ex.Message] |> asErrors

let writeInColour (colour: ConsoleColor) (msg: string) =
    Console.ForegroundColor <- colour
    Console.WriteLine msg
    Console.ResetColor ()

let writeSuccess msg =
    msg |> writeInColour ConsoleColor.DarkGreen

let writeInfo msg =
    msg |> writeInColour ConsoleColor.Cyan

let writeError msg =
    msg |> writeInColour ConsoleColor.DarkRed

[<EntryPoint>]
let main argv =
    if argv |> isEmpty then
        writeError "Please specify a path"
        1
    else
        let basePath = fullPath argv.[0]

        writeInfo "Validating configuration and paths"

        let configPath = sprintf @"%s\config.cfg" basePath

        let loadConfig' = loadConfig fileExists readLines
        let createPaths' = createPaths basePath
        let validatePaths' = validatePaths directoryExists

        let deleteExisting' path = result {
            do! deleteExisting "*.html" path
            do! deleteExisting "*.xml" path
        }

        let createContent = result {
            let! configurationData = configPath |> loadConfig'
            let! configuration = configurationData |> validateConfig

            let pathData = configuration |> createPaths'
            let! paths = pathData |> validatePaths'

            let fs = getFileSystem paths.Template_Path

            let createTemplate' = createTemplate readAll fs
            let getOutputFileName' = getOutputFileName getFileNameWithoutExtension
            let getRelativeUrl' = getRelativeUrl getFileNameWithoutExtension

            let siteMetadata = configuration |> getSiteMetadata

            let getPageMetadata (path, content) =
                parsePageMetadata getOutputFileName' getRelativeUrl' siteMetadata paths path content

            let! pageMetaData =
                paths.Content_Path
                |> getFilesInDirectory "*.md"
                |> Seq.map (fun filePath -> (filePath, (readAll filePath)))
                |> Seq.map getPageMetadata
                |> checkAllOk

            let articleList = pageMetaData |> getArticleList

            let templates = dict [
                (createTemplate' "article")
                (createTemplate' "basic")
                (createTemplate' "home")
                (createTemplate' "articleindex")
            ]

            do! paths.Output_Path |> deleteExisting'

            writeInfo (sprintf "Generating HTML from %s" paths.Content_Path)

            pageMetaData
            |> Seq.filter (fun p -> p.Template = "article")
            |> Seq.sortByDescending (fun p -> p.Updated.Value)
            |> Seq.truncate 10
            |> createRssFeed siteMetadata
            |> writeToFile (sprintf @"%s\rss.xml" paths.Output_Path)

            return! pageMetaData
                    |> Seq.map (fun md -> { md with Article_List = articleList })
                    |> Seq.map (tryRender templates)
                    |> checkAllOk
        }

        match createContent with
        | Error errors ->
            errors |> Seq.iter writeError

            writeInfo "Done"
            1

        | Ok data ->
            data |> Seq.iter (fun (md, html) ->
                writePageHtml md.Output_Path md html
                writeSuccess (sprintf "  [OK] %s" md.Output_FileName))

            writeInfo "Done"
            0
