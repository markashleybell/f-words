﻿module fwords.Templating

open DotLiquid.FileSystems
open DotLiquid
open Types
open Utils
open System.Text.RegularExpressions
open CommonMark
open System
open System.Collections.Generic
open CommonMark.Syntax
open System.Globalization
open CommonMark.Formatters
open System.IO
open MAB.SyntaxHighlighter
open MAB.SyntaxHighlighter.Languages

let getSiteMetadata (cfg: Map<string, string>): SiteMetadata =
    {
        Site_Name = cfg.["site_name"]
        Site_Url = cfg.["site_url"]
        Rss_Description = cfg.["rss_description"]
        Cdn1 = cfg.["cdn1"]
        Cdn2 = cfg.["cdn2"]
        Analytics_Id = cfg.["analytics_id"]
        Disqus_Id = cfg.["disqus_id"]
    }

let metadataRegexOptions = 
    RegexOptions.IgnoreCase ||| RegexOptions.Multiline ||| RegexOptions.Compiled

let metadataMatcher name =
    let pattern = sprintf "%s: (.*?)$" name
    (name, new Regex (pattern, metadataRegexOptions))

let titleHeader = metadataMatcher "Title"
let publishedHeader = metadataMatcher "Published"
let updatedHeader = metadataMatcher "Updated"
let abstractHeader = metadataMatcher "Abstract"
let pageTypeHeader = metadataMatcher "PageType"
let thumbnailHeader = metadataMatcher "Thumbnail"
let templateHeader = metadataMatcher "Template"
let documentClassHeader = metadataMatcher "DocumentClass"

let formatLiquidExceptionList errors =
    errors 
    |> Seq.map (fun (e: exn) -> Regex.Replace(e.Message, "(Liquid )?Error - ", "")) 
    |> Seq.map (fun msg -> sprintf "  %s" (formatOutput msg))
    |> String.concat Environment.NewLine

let getOptionalHeaderValue (matcher: (string * Regex)) content =
    let (_, rx) = matcher
    rx.Match content 
    |> (fun m -> match m.Success with
                 | true -> Some (m.Groups.[1].Value.Trim())
                 | false -> None)

let getRequiredHeaderValue (matcher: (string * Regex)) content =
    let (headerName, rx) = matcher
    rx.Match content 
    |> (fun m -> match m.Success with
                 | true -> Ok (m.Groups.[1].Value.Trim())
                 | false -> Error (formatOutput (sprintf "Missing %s header" headerName)))

let getOptionalHeaderValueOrDefault header default' content =
    let optionalValue = content |> getOptionalHeaderValue header 
    match optionalValue with
    | Some f -> f
    | None -> default'

let getPageType content =
    match (content |> getOptionalHeaderValue pageTypeHeader) with
    | Some typ ->  
        match typ with 
        | "static" -> Ok Static
        | _ -> Error (formatOutput "Invalid PageType header value (\"static\" is currently the only valid value)")
    | None -> Ok Article

let getDateHeaderValue (matcher: (string * Regex)) content =
    let (headerName, _) = matcher
    let dateParseError = Error (formatOutput (sprintf "Invalid date format in %s header" headerName))
    let parseDate = parseiso8601date dateParseError
    result {
        let! dateString = content |> getRequiredHeaderValue matcher
        return! dateString |> parseDate
    }

let getOutputFileName getFileNameWithoutExtension filePath = 
    filePath 
    |> getFileNameWithoutExtension
    |> sprintf "%s.html"

let getRelativeUrl getFileNameWithoutExtension filePath =
    let fileName = filePath |> getFileNameWithoutExtension
    match fileName with
    | "index" -> ""
    | _ -> fileName + ".html"

let stripMetadataHeaders (content: string) = 
    let matchers = [
        titleHeader
        publishedHeader
        updatedHeader
        abstractHeader
        pageTypeHeader
        thumbnailHeader
        templateHeader
        documentClassHeader
    ]

    matchers 
    |> Seq.map (fun (_, rx) -> rx)
    |> Seq.fold (fun ct rx -> rx.Replace (ct, "")) content

let langDeclarationMatcher = new Regex ("^:::([^\s]+)", RegexOptions.Multiline)

let getAndStripLanguageDeclaration input =
    replaceAndReturn langDeclarationMatcher input

let languageMap = Map.ofList [
    ("csharp", (csharp, "EventHandler Form DateTime Timer TimeSpan"))
    ("fsharp", (fsharp, ""))
    ("javascript", (javascript, ""))
    ("json", (javascript, ""))
    ("python", (python, ""))
    ("html", (html, ""))
]

let highlightCode = 
    SyntaxHighlighter.formatCode languageMap

type CustomHtmlFormatter(target: System.IO.TextWriter, settings: CommonMarkSettings) = 
    inherit HtmlFormatter(target, settings)

    override __.WriteBlock(block, isOpening, isClosing, ignoreChildNodes) =
        let isCodeBlock = 
            (block.Tag = BlockTag.FencedCode || block.Tag = BlockTag.IndentedCode) 
                && (not (base.RenderPlainTextInlines.Peek()))
    
        if isCodeBlock
        then
            ignoreChildNodes <- false
            if isOpening
            then 
                // TODO: Get types to be highlighted from code block header
                let (language, content) = block.StringContent.ToString() |> getAndStripLanguageDeclaration

                let highlighted = 
                    match language with
                    | Some lang -> 
                        let (processed, regex, code) = content |> highlightCode lang
                        code
                    | None -> System.Net.WebUtility.HtmlEncode content
            
                __.Write("<pre class=\"code\"><code>")
                __.Write(highlighted)
                __.Write("</code></pre>")
        else
            base.WriteBlock(block, isOpening, isClosing, &ignoreChildNodes)

let markdownSettings = CommonMarkSettings.Default.Clone()

let output (doc: Block) (output: TextWriter) (settings: CommonMarkSettings) =
    let fmt = new CustomHtmlFormatter(output, settings)
    fmt.WriteDocument(doc)

markdownSettings.OutputDelegate <- Action<Block,TextWriter,CommonMarkSettings>(output)

let parse (source: string) =
    CommonMarkConverter.Parse (source, markdownSettings)

let nodeHasTargetUrl (node: EnumeratorEntry) =
    node.IsOpening && node.Inline <> null && node.Inline.TargetUrl <> null

let transformRelativeLinks cdnUrl (doc: Block) =
    doc.AsEnumerable () 
    |> Seq.filter nodeHasTargetUrl 
    |> Seq.iter (fun n -> 
        if n.Inline.TargetUrl.StartsWith "~"
        then n.Inline.TargetUrl <- cdnUrl + (n.Inline.TargetUrl.Substring 1))
    doc

let toHtml doc =
    use writer = new System.IO.StringWriter (CultureInfo.CurrentCulture)
    CommonMarkConverter.ProcessStage3 (doc, writer, markdownSettings)
    writer.ToString ()

let getBodyHtml (transform: Block -> Block) (content: string) =
    content |> stripMetadataHeaders |> parse |> transform |> toHtml

let parseTemplate readAll (fs: LocalFileSystem) (name: string) =
    Template.Parse (readAll (fs.FullPath name))

let createTemplate readAll fs typ =
    (typ, (parseTemplate readAll fs typ))

let render o (template: Template) = 
    let result = template.Render (Hash.FromAnonymousObject o)
    match template.Errors.Count with
    | 0 -> Ok result
    | _ -> Error template.Errors

let getFileSystem templatePath = 
    let fs = LocalFileSystem templatePath
    Template.FileSystem <- fs
    // TODO: Why does the DotLiquid Ruby convention seemingly not work at all?
    // I'd like to not have underscores in my type property names, but at the moment
    // this isn't possible because what the DotLiquid docs say happens... doesn't happen
    // Template.NamingConvention <- NamingConventions.RubyNamingConvention()
    fs

let parsePageMetadata getOutputFileName getRelativeUrl (siteMetadata: SiteMetadata) (sitePaths: SitePaths) (filePath: string) content =
    let r = result {
        let! pageType = content |> getPageType
        
        let! title = content |> getRequiredHeaderValue titleHeader
        let! abstract' = content |> getRequiredHeaderValue abstractHeader

        let getBodyHtml' = getBodyHtml (transformRelativeLinks siteMetadata.Cdn2)

        let body = content |> getBodyHtml'
        
        let outputFileName = getOutputFileName filePath
        let relativeUrl = getRelativeUrl filePath
        let metatitle = sprintf "%s - %s" title siteMetadata.Site_Name
        
        let thumbnail = content |> getOptionalHeaderValueOrDefault thumbnailHeader "site.png"
        let template = content |> getOptionalHeaderValueOrDefault templateHeader "article"
        let documentClass = content |> getOptionalHeaderValueOrDefault documentClassHeader "html-base"

        let page = {
            Source_FileName = filePath
            Output_FileName = outputFileName
                
            Title = title
            Body = body
            Abstract = abstract'
            Relative_Url = relativeUrl
            Meta_Title = metatitle
            Og_Title = title
            Og_Abstract = abstract'
            Og_Image = thumbnail
            Og_Url = relativeUrl
            Document_Class = documentClass
            Template = template
                
            Published = Nullable()
            Updated = Nullable()
                
            Site_Name = siteMetadata.Site_Name
            Site_Url = siteMetadata.Site_Url
            Rss_Description = siteMetadata.Rss_Description
            Cdn1 = siteMetadata.Cdn1
            Cdn2 = siteMetadata.Cdn2
            Analytics_Id = siteMetadata.Analytics_Id
            Disqus_Id = siteMetadata.Disqus_Id

            Output_Path = sitePaths.Output_Path
                
            Article_List = []
        }

        match pageType with
        | Static ->
            return page
        | Article ->
            // Date fields are mandatory for articles, so even though 
            // the type fields are optional we call getDateHeaderValue, 
            // which fails with an error if the header isn't present
            let! published = content |> getDateHeaderValue publishedHeader
            let! updated = content |> getDateHeaderValue updatedHeader

            return { page with Published = Nullable published; Updated = Nullable updated }
    }

    match r with
    | Ok md -> Ok md
    | Error e -> Error (formatPageError filePath e)

let getArticleList pageMetaData = 
    pageMetaData
    |> Seq.filter (fun p -> p.Template = "article")
    |> Seq.map (fun p -> 
        { ArticleSummary.Title = p.Title
          Abstract = p.Abstract
          Relative_Url = p.Relative_Url
          Updated = p.Updated })
    |> Seq.sortByDescending (fun p -> p.Updated.Value)
    |> List.ofSeq

let tryRender (templates: IDictionary<string, Template>) metaData =
    let r = templates.[metaData.Template] |> render metaData
    match r with
    | Error e -> 
        Error (formatPageError metaData.Source_FileName (e |> formatLiquidExceptionList))
    | Ok html -> 
        Ok (metaData, html)