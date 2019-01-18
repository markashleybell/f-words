module fwords.Syndication

open System
open fwords.Types
open CommonMark

let toRfc1123String (dto: DateTimeOffset) = dto.ToString("r")

let createRssFeedItem (page: Page) =
    let url = page.Site_Url + "/" + page.Relative_Url

    sprintf """        <item>
            <title>%s</title>
            <link>%s</link>
            <description>%s</description>
            <guid isPermaLink="false">%s?d=%i</guid>
            <pubDate>%s</pubDate>
        </item>
"""
        page.Title
        url
        (((CommonMarkConverter.Convert page.Abstract).Trim()) |> System.Net.WebUtility.HtmlEncode)
        url
        ((DateTimeOffset(page.Updated.Value, TimeSpan.Zero)).ToUnixTimeSeconds())
        (toRfc1123String (DateTimeOffset(page.Updated.Value, TimeSpan.Zero)))

let createRssFeed (metaData: SiteMetadata) pages = 
    let items = pages |> Seq.map (createRssFeedItem) |> String.concat ""

    sprintf """<?xml version="1.0" encoding="utf-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
    <channel>
        <title>%s</title>
        <link>%s</link>
        <description>%s</description>
        <lastBuildDate>%s</lastBuildDate>
        <atom:link href="%s/rss.xml" rel="self" type="application/rss+xml" />
%s    </channel>
</rss>""" 
        metaData.Site_Name 
        metaData.Site_Url 
        metaData.Rss_Description 
        (toRfc1123String (DateTimeOffset(DateTime.Now)))
        metaData.Site_Url
        items