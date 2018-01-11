namespace OAuthExample

module Repositories =

    type Repository = { name: string; html_url: string }

    let GetUserRepositories token =
        Auth.JsonRequest<Repository[]> "https://api.github.com/user/repos" token
