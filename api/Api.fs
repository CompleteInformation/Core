namespace CompleteInformation.Core.Api

open CompleteInformation.Core.PluginBase

type PluginApi =
    {
        get: unit -> Async<PluginMetadata list>
    }
