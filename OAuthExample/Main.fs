namespace OAuthExample

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About
    | [<EndPoint "/oauth-github">] OAuthGitHub

module Authentication =
    open System.IO
    open WebSharper.OAuth

    type GithubUserData = { login: string }

    /// Get basic user data from GitHub.
    let getUserData (token: OAuth2.AuthenticationToken) =
        async {
            let req =
                System.Net.HttpWebRequest.CreateHttp("https://api.github.com/user",
                    KeepAlive = false,
                    UserAgent = "WebSharper OAuthExample"
                )
            token.AuthorizeRequest(req)
            let! response = req.AsyncGetResponse()
            use reader = new StreamReader(response.GetResponseStream())
            let! jsonData = reader.ReadToEndAsync() |> Async.AwaitTask
            return WebSharper.Json.Deserialize<GithubUserData> jsonData
        }

    /// The OAuth2 authorization provider for GitHub.
    let Provider =
        OAuth2.Provider.Setup(
            service = OAuth2.ServiceSettings.Github("APP_ID", "APP_SECRET"),
            redirectEndpointAction = EndPoint.OAuthGitHub,
            redirectEndpoint = (fun ctx response ->
                async {
                    match response with
                    | OAuth2.Success token ->
                        let! userData = getUserData token
                        // All good! The user is authenticated.
                        do! ctx.UserSession.LoginUser(userData.login)
                        return! Content.RedirectTemporary EndPoint.About
                    | _ ->
                        // This is "normal" failure: the user simply rejected the authorization.
                        do! ctx.UserSession.Logout()
                        return! Content.RedirectTemporary EndPoint.Home
                }
            )
        )

module Templating =
    open WebSharper.UI.Html

    type MainTemplate = Templating.Template<"Main.html">

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
             li [if endpoint = act then yield attr.``class`` "active"] [
                a [attr.href (ctx.Link act)] [text txt]
             ]
        [
            "Home" => EndPoint.Home
            "About" => EndPoint.About
        ]

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page(
            MainTemplate()
                .Title(title)
                .MenuBar(MenuBar ctx action)
                .Body(body)
                .Doc()
        )

module Site =
    open WebSharper.UI.Html

    let HomePage ctx =
        Templating.Main ctx EndPoint.Home "Home" [
            h1 [] [text "Say Hi to the server!"]
            div [] [client <@ Client.Main() @>]
        ]

    let AboutPage (ctx: Context<EndPoint>) = async {
        let! loggedIn = ctx.UserSession.GetLoggedInUser()
        match loggedIn with
        | Some username ->
            return! Templating.Main ctx EndPoint.About "About" [
                h1 [] [text ("Welcome " + username)]
                p [] [text "This is a template WebSharper client-server application."]
            ]
        | None ->
            let loginUrl = Authentication.Provider.GetAuthorizationRequestUrl(ctx)
            return! Templating.Main ctx EndPoint.About "About" [
                h1 [] [text "Not logged in!"]
                p [] [
                    text "Sorry, you need to be logged in to access this page. "
                    a [attr.href loginUrl] [text "Click here"]
                    text " to log in."
                ]
            ]
    }

    [<Website>]
    let Main =
        Authentication.Provider.RedirectEndpointSitelet
        <|>
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.About -> AboutPage ctx
            // This is already handled by RedirectEndpointSitelet above:
            | EndPoint.OAuthGitHub -> Content.ServerError
        )
