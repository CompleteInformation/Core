namespace CompleteInformation.Core.PluginBase

type PluginId = PluginId of string

module PluginId =
    let create value = PluginId value
    let unwrap (PluginId value) = value

type PluginMetadata = { id: PluginId; name: string }

type WebserverPlugin =
    abstract member getApi: unit -> obj
    abstract member getMetaData: unit -> PluginMetadata
