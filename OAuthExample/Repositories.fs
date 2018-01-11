namespace OAuthExample

module Repositories =

    type Repository = { name: string; html_url: string }

    /// Retrieve the user's list of GitHub repositories from the API.
    let GetUserRepositories token =
        Auth.JsonRequest<Repository[]> "https://api.github.com/user/repos" token
