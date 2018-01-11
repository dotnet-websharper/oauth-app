namespace OAuthExample

open WebSharper

type OAuthProvider = Facebook | GitHub

type EndPoint =
    | [<EndPoint "GET /">] Home
    | [<EndPoint "GET /private">] Private
    | [<EndPoint "GET /repos">] Repos
    | [<EndPoint "GET /oauth">] OAuth of provider: OAuthProvider
    | [<EndPoint "GET /logout">] Logout
