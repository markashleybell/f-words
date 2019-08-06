<Query Kind="FSharpProgram">
  <Reference Relative="..\f-words\bin\Debug\net472\fw.exe">C:\Src\f-words\f-words\bin\Debug\net472\fw.exe</Reference>
  <NuGetReference>CommonMark.NET</NuGetReference>
  <NuGetReference>TaskBuilder.fs</NuGetReference>
  <Namespace>CommonMark</Namespace>
  <Namespace>CommonMark.Formatters</Namespace>
  <Namespace>CommonMark.Syntax</Namespace>
  <Namespace>fwords.Templating</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
</Query>

let content = 
    File.ReadAllText @"C:\Src\markb.uk\content\portable-git-windows-setting-home-environment-variable.md"

//let getBodyHtml' = getBodyHtml (transformRelativeLinks' "TEST_CDN")
//    
//content 
//|> getBodyHtml' 
//|> Dump 
//|> ignore

let markdownSettings = CommonMarkSettings.Default.Clone()

markdownSettings.OutputFormat <- OutputFormat.SyntaxTree

let printDoc doc = 
    use writer = new System.IO.StringWriter (CultureInfo.CurrentCulture)
    CommonMarkConverter.ProcessStage3 (doc, writer, markdownSettings)
    writer.ToString ()

content 
|> stripMetadataHeaders 
|> parse 
|> printDoc
// |> toHtml 
|> Dump
|> ignore

