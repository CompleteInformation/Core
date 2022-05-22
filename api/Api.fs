namespace CompleteInformation.Core.Api

type PluginApi = { get: unit -> Async<string list> }
