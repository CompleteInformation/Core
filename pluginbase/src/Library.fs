namespace CompleteInformation.Core.PluginBase

type IWebserverPlugin =
    abstract member getApi : unit -> 'a
