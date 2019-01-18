module fwords.Types

open System
open DotLiquid

[<LiquidType("Title", "Abstract", "Relative_Url", "Updated")>]
type ArticleSummary = {
    Title: string
    Abstract: string
    Relative_Url: string
    Updated: Nullable<DateTime>
}

type Page = {
    Source_FileName: string
    Output_FileName: string

    Title: string
    Abstract: string
    Body: string
    Relative_Url: string
    Meta_Title: string
    Og_Title: string
    Og_Abstract: string
    Og_Image: string
    Og_Url: string
    Document_Class: string
    Template: string

    // I really don't like this... but DotLiquid doesn't support 
    // F# option types, so we don't have a lot of choice
    Published: Nullable<DateTime>
    Updated: Nullable<DateTime>

    Site_Name: string
    Site_Url: string
    Rss_Description: string
    Cdn1: string
    Cdn2: string
    Analytics_Id: string
    Disqus_Id: string

    Output_Path: string

    Article_List: ArticleSummary list
}

type SiteMetadata = {
    Site_Name: string
    Site_Url: string
    Rss_Description: string
    Cdn1: string
    Cdn2: string
    Analytics_Id: string
    Disqus_Id: string
}

type SitePaths = {
    Base_Path: string
    Template_Path: string
    Content_Path: string
    Output_Path: string
}

type PageType = Static | Article
