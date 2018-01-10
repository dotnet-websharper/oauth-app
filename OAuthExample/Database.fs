namespace OAuthExample

open System
open System.Collections.Generic
open System.Collections.Concurrent

/// This is a simple mock database that just stores data in a dictionary.
module Database =

    /// The unique identifier of a user of this app.
    type UserId = string

    /// The unique identifier of a GitHub or Facebook user.
    type OAuthUserId = OAuthProvider * string

    /// Data associated with a user of this app.
    type UserData =
        {
            OAuthUserId: OAuthUserId
            OAuthToken: WebSharper.OAuth.OAuth2.AuthenticationToken
            DisplayName: string
        }

    /// The data associated with app users.
    let private Users = ConcurrentDictionary<UserId, UserData>()

    /// The association from external users to app users.
    let private OAuthUsers = ConcurrentDictionary<OAuthUserId, UserId>()

    /// Get user data for this app user id, if it exists.
    let TryGetUser userId =
        match Users.TryGetValue(userId) with
        | true, u -> Some u
        | false, _ -> None

    /// Create a user from data parsed from OAuth, or get the existing associated user id.
    let AddOrUpdateUser data =
        let userId =
            match OAuthUsers.TryGetValue(data.OAuthUserId) with
            | true, userId -> userId
            | false, _ -> Guid.NewGuid().ToString("N")
        Users.AddOrUpdate(userId, data, fun _ _ -> data) |> ignore
        userId
