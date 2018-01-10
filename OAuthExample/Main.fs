namespace OAuthExample

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server
open WebSharper.UI.Html

module Templating =

    type MainTemplate = Templating.Template<"Main.html">

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
             li [if endpoint = act then yield attr.``class`` "active"] [
                a [attr.href (ctx.Link act)] [text txt]
             ]
        [
            "Home" => EndPoint.Home
            "Private section" => EndPoint.Private
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

    let HomePage ctx =
        Templating.Main ctx EndPoint.Home "Home" [
            h1 [] [text "Welcome to the WebSharper OAuth sample app."]
            p [] [text "Try to access the Private section to log in."]
        ]

    let PrivatePage (ctx: Context<EndPoint>) = async {
        let! loggedIn = ctx.UserSession.GetLoggedInUser()
        let body =
            match loggedIn |> Option.bind Database.TryGetUser with
            | Some user ->
                Templating.MainTemplate.PrivateNotLoggedInContent()
                    .Username(user.DisplayName)
                    .Doc()
            | None ->
                Templating.MainTemplate.PrivateLoggedInContent()
                    .GitHubLoginUrl(Auth.GitHub.Provider.GetAuthorizationRequestUrl(ctx))
                    .FacebookLoginUrl(Auth.Facebook.Provider.GetAuthorizationRequestUrl(ctx))
                    .Doc()
        return! Templating.Main ctx EndPoint.Private "Private section" [body]
    }

    let LogoutPage (ctx: Context<EndPoint>) = async {
        do! ctx.UserSession.Logout()
        return! Content.RedirectTemporary EndPoint.Home
    }

    [<Website>]
    let Main =
        Auth.Sitelet
        <|>
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.Private -> PrivatePage ctx
            | EndPoint.Logout -> LogoutPage ctx
            // This is already handled by Auth.Sitelet above:
            | EndPoint.OAuth _ -> Content.ServerError
        )
