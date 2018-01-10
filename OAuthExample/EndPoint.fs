namespace OAuthExample

open WebSharper

type OAuthProvider = Facebook | GitHub

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/private">] Private
    | [<EndPoint "/oauth">] OAuth of OAuthProvider
    | [<EndPoint "/logout">] Logout
