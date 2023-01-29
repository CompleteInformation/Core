namespace CompleteInformation.Core.Backend.Web

[<AutoOpen>]
module Api =
    open CompleteInformation.Base.Backend.Web
    open CompleteInformation.Core
    open CompleteInformation.Server.Api
    open System

    let getPluginApi devMode plugins =
        let getPlugins () = async {
            return
                plugins
                |> Seq.map (fun (plugin: WebserverPlugin) -> plugin.getMetaData ())
                |> List.ofSeq
        }

        let api: PluginApi = { get = getPlugins }

        Api.build devMode api

    module User =
        type UserMap = Map<UserId, User>

        let moduleName = "user"
        let fileName = "users.jsonl"

        let append (user: User) =
            Persistence.JsonL.append moduleName fileName user

        let load () : Async<UserMap> = async {
            let! list = Persistence.JsonL.load<User> moduleName fileName

            return
                match list with
                | Persistence.ReadResult.Success list -> list
                | Persistence.ReadResult.FileNotFound -> []
                |> Seq.map (fun (user: User) -> user.id, user)
                |> Map.ofSeq
        }

    let getUserApi devMode =
        let mutable users = User.load () |> Async.RunSynchronously

        let createUser name = async {
            let id = Guid.NewGuid() |> UserId
            let user = User.create id name

            users <- Map.add id user users
            do! User.append user

            return id
        }

        let getUser id : Async<User option> = async { return users |> Map.tryFind id }

        let getUserList () : Async<User list> = async { return users |> Map.values |> List.ofSeq }

        let api: UserApi = {
            create = createUser
            get = getUser
            getList = getUserList
        }

        Api.build devMode api
