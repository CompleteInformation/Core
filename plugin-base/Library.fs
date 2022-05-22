namespace CompleteInformation.Core.PluginBase

type WebserverPlugin =
    abstract member getApi: unit -> 'a
    abstract member getId: unit -> string
