namespace CompleteInformation.Core.Api

open CompleteInformation.PluginBase

type PluginApi =
    {
        get: unit -> Async<PluginMetadata list>
    }
