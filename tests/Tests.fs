namespace Tests

open NUnit.Framework
open FsUnit

module TemplatingTests =
    open fwords.Templating
    open MAB.SyntaxHighlighter

    [<Test>]
    let ``Parse language declaration`` () =
        let text = """:::csharp
        var myObject1 = new ComplicatedThing(
            loadChildren: true,
            maxDepth: 3,
            loadImages: true,
            allowUpdates: false,
            lockDeletion: true
        );"""

        let lang = text |> parseLanguageDeclaration

        let (langName, extraTypes) = lang.Value

        langName |> should equal "csharp"
        extraTypes |> should equal None

    [<Test>]
    let ``Parse language declaration with extra types`` () =
        let text = """:::csharp{ComplicatedThing|Action}
        var myObject1 = new ComplicatedThing(
            loadChildren: true,
            maxDepth: 3,
            loadImages: true,
            allowUpdates: false,
            lockDeletion: true
        );"""

        let lang = text |> parseLanguageDeclaration

        let (langName, extraTypes) = lang.Value

        langName |> should equal "csharp"
        extraTypes.Value |> should equal [|"ComplicatedThing"; "Action"|]

    [<Test>]
    let ``Get language map`` () =
        let map = getLanguageMap "csharp" None
        let (_, types) = map.["csharp"]
        types |> should equal "EventHandler Form DateTime Timer TimeSpan"

    [<Test>]
    let ``Get language map with extra types`` () =
        let map = getLanguageMap "csharp" (Some [|"TEST1"; "TEST2"|])
        let (_, types) = map.["csharp"]
        types |> should equal "EventHandler Form DateTime Timer TimeSpan TEST1 TEST2"
