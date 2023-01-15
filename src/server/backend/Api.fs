namespace CompleteInformation.Core.Backend.Web

[<AutoOpen>]
module Api =
    open CompleteInformation.Base.Backend.Web
    open CompleteInformation.Core
    open CompleteInformation.Server.Api

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
        let fileName = "users.json"

        let save (map: UserMap) =
            Map.values map |> List.ofSeq |> Persistence.saveJson moduleName fileName

        let load () : Async<UserMap> = async {
            let! list = Persistence.loadJson<User list> moduleName fileName

            return
                match list with
                | Persistence.ReadResult.Success list -> list
                | Persistence.ReadResult.FileNotFound -> []
                |> List.map (fun (user: User) -> user.id, user)
                |> Map.ofList
        }

    let getUserApi devMode =
        let mutable users = User.load () |> Async.RunSynchronously

        let createUser name = async {
            let id =
                match Map.count users with
                | 0 -> 1
                | _ -> Map.keys users |> Seq.map UserId.unwrap |> Seq.max |> (+) 1
                |> UserId

            users <- Map.add id (User.create id name) users
            User.save users |> Async.RunSynchronously

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
