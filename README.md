# f-words

`f-words` is an extremely basic static site generator written in F#. It was built specifically to publish [markb.uk](https://markb.uk/) ([source here](https://github.com/markashleybell/markb.uk)).

It uses [DotLiquid](https://github.com/dotliquid/dotliquid) templates for page layout, and [Markdown](https://github.com/Knagis/CommonMark.NET) for content generation.

## Build

You'll need the [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1) installed. Once you have it, you can build/publish the app using VS2019, Visual Studio Code or the `dotnet` CLI.

Releases are also automatically published [here](https://github.com/markashleybell/f-words/releases) when a new version is tagged.

## Configuration

`f-words` currently relies on an `fwords.cfg` file being present in the root of the working folder. The file must contain the following key/value pairs (example values shown):

```
site_name: My Blog
site_url: https://myblog.com
rss_description: The latest articles from my blog.
template_path: templates
content_path: content
output_path: public
```

`template_path` is where the DotLiquid templates reside, `content_path` points to a folder full of Markdown content files, and `output_path` is the subfolder where the published HTML output will end up.

## How to use

I haven't had time to write this up properly, but for now, you can hopefully work out how to structure a site project by using the [markb.uk source repository](https://github.com/markashleybell/markb.uk) as a reference.

To publish, just run `fwords.exe <YOUR SITE PROJECT ROOT>`.
