namespace CompleteInformation.Server.Api

open CompleteInformation.Core

type PluginApi =
    {
        get: unit -> Async<PluginMetadata list>
    }
