<Query Kind="FSharpProgram">
  <Reference Relative="..\f-words\bin\Debug\net472\fw.exe">C:\Src\f-words\f-words\bin\Debug\net472\fw.exe</Reference>
  <NuGetReference>CommonMark.NET</NuGetReference>
  <NuGetReference>TaskBuilder.fs</NuGetReference>
  <Namespace>CommonMark</Namespace>
  <Namespace>CommonMark.Formatters</Namespace>
  <Namespace>CommonMark.Syntax</Namespace>
  <Namespace>fwords.Templating</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
</Query>

let content = 
    File.ReadAllText @"C:\Src\markb.uk\content\election-2015-manifesto-mining.md"

let nodeHasTargetUrl (node: EnumeratorEntry) =
    // node.ToString().Dump()
    node.IsOpening && node.Inline <> null && node.Inline.TargetUrl <> null

let transformRelativeLinks' cdnUrl (doc: Block) =
    doc.AsEnumerable () 
    |> Seq.filter nodeHasTargetUrl 
    |> Seq.iter (fun n -> 
        if n.Inline.TargetUrl.StartsWith "~"
        then n.Inline.TargetUrl <- cdnUrl + (n.Inline.TargetUrl.Substring 1))
    doc
    
let getBodyHtml' = getBodyHtml (transformRelativeLinks' "TEST_CDN")
    
content 
|> getBodyHtml' 
|> Dump 
|> ignore
