namespace CompleteInformation.Server.Api

open CompleteInformation.Shared.Core

type PluginApi =
    {
        get: unit -> Async<PluginMetadata list>
    }
