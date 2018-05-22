namespace OAuthExample

open System.IO
open WebSharper
open WebSharper.Sitelets
open WebSharper.OAuth

module Auth =

    /// Perform a GET request to an OAuth2-protected JSON API.
    let JsonRequest<'JsonResponse> (url: string) (token: OAuth2.AuthenticationToken) = async {
        let req =
            System.Net.HttpWebRequest.CreateHttp(url,
                KeepAlive = false,
                UserAgent = "WebSharper OAuthExample"
            )
        token.AuthorizeRequest(req)
        let! response = req.AsyncGetResponse()
        use reader = new StreamReader(response.GetResponseStream())
        let! jsonData = reader.ReadToEndAsync() |> Async.AwaitTask
        return WebSharper.Json.Deserialize<'JsonResponse> jsonData
    }

    /// The content to serve based on a user's authentication response.
    let private redirectEndpoint getUserData (ctx: Context<EndPoint>) response = async {
        match response with
        | OAuth2.Success token ->
            // All good! The user is authenticated.
            let! (userData : Database.UserData) = getUserData token
            let userId = Database.AddOrUpdateUser userData
            do! ctx.UserSession.LoginUser(userId)
            return! Content.RedirectTemporary EndPoint.Private
        | _ ->
            // This is "normal" failure: the user simply rejected the authorization.
            do! ctx.UserSession.Logout()
            return! Content.RedirectTemporary EndPoint.Home
    }

    let private getAppSetting (key: string) =
        match System.Configuration.ConfigurationManager.AppSettings.[key] with
        | null -> ""
        | v -> v

    module GitHub =

        type private UserData = { login: string; name: string }

        let service = OAuth2.ServiceSettings.Github(getAppSetting "github-app-id", getAppSetting "github-app-secret")

        /// The OAuth2 authorization provider for GitHub.
        let Provider =
            OAuth2.Provider.Setup(
                service = service,
                redirectEndpointAction = EndPoint.OAuth OAuthProvider.GitHub,
                redirectEndpoint = redirectEndpoint (fun token -> async {
                    let! userData = JsonRequest<UserData> "https://api.github.com/user" token
                    return {
                        OAuthUserId = OAuthProvider.GitHub, userData.login
                        OAuthToken = token
                        DisplayName =
                            match userData.name with
                            | null -> userData.login
                            | name -> name
                    }
                })
            )

    module Facebook =

        type private UserData = { id: string; name: string }

        let service = OAuth2.ServiceSettings.Facebook(getAppSetting "facebook-app-id", getAppSetting "facebook-app-secret")

        /// The OAuth2 authorization provider for Facebook.
        let Provider =
            OAuth2.Provider.Setup(
                service = service,
                redirectEndpointAction = EndPoint.OAuth OAuthProvider.Facebook,
                redirectEndpoint = redirectEndpoint (fun token -> async {
                    let! userData = JsonRequest<UserData> "https://graph.facebook.com/me" token
                    return {
                        OAuthUserId = OAuthProvider.Facebook, userData.id
                        OAuthToken = token
                        DisplayName = userData.name
                    }
                })
            )

    /// The set of all redirect endpoints for our OAuth providers.
    let Sitelet =
        GitHub.Provider.RedirectEndpointSitelet
        <|>
        Facebook.Provider.RedirectEndpointSitelet

    /// Sanity check for the purpose of demonstration:
    /// if this is false, then the providers aren't configured properly,
    /// so we show instructions on the home page.
    let IsConfigured =
        GitHub.service.ClientId <> "" &&
        GitHub.service.ClientSecret <> "" &&
        Facebook.service.ClientId <> "" &&
        Facebook.service.ClientSecret <> ""
