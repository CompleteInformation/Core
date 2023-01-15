namespace CompleteInformation.Server.Api

open CompleteInformation.Core

type PluginApi = {
    get: unit -> Async<PluginMetadata list>
}

type UserApi = {
    create: string -> Async<UserId>
    get: UserId -> Async<User option>
    getList: unit -> Async<User list>
}
