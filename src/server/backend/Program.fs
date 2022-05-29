module CompleteInformation.Core.Backend.Web

open System
open System.IO

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe

open CompleteInformation.Server.Api
open CompleteInformation.Shared.Backend.Web

type Program = unit

let routeBuilder = sprintf "/api/%s/%s"

let getPluginApi plugins =
    let getPlugins () =
        async {
            return
                plugins
                |> Seq.map (fun (plugin: WebserverPlugin) -> plugin.getMetaData ())
                |> List.ofSeq
        }

    let api: PluginApi = { get = getPlugins }

    Api.build api

let buildApis plugins =
    let pluginApis = Seq.map (fun (plugin: WebserverPlugin) -> plugin.getApi) plugins

    [
        getPluginApi plugins
        yield! pluginApis
    ]
    |> Seq.map (fun f -> f routeBuilder)

let buildWebApp plugins =
    choose [
#if DEBUG
        route "/" >=> text "Server is running."
#else
        route "/" >=> redirectTo true "/index.html"
#endif
        yield! buildApis plugins
        setStatusCode 404 >=> text "Not Found"
    ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")

    clearResponse
    >=> setStatusCode 500
    >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder
        .WithOrigins("http://localhost:8082")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureApp plugins (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

    let app =
        match env.IsDevelopment() with
        | true ->
            printfn "Development mode"

            app
                .UseDeveloperExceptionPage()
                .UseCors(configureCors)
        | false ->
            printfn "Production mode"

            app
                .UseGiraffeErrorHandler(errorHandler)
                .UseHttpsRedirection()

    app
        .UseStaticFiles()
        .UseGiraffe(buildWebApp plugins)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

open System.Reflection
open System.Runtime.Loader

type PluginLoadContext(pluginPath: string) =
    inherit AssemblyLoadContext()

    let resolver = new AssemblyDependencyResolver(pluginPath)

    override this.Load(assemblyName: AssemblyName) =
        resolver.ResolveAssemblyToPath(assemblyName)
        |> Option.ofObj
        |> function
            | Some assemblyPath -> this.LoadFromAssemblyPath assemblyPath
            | None -> null

    override this.LoadUnmanagedDll(unmanagedDllName: string) =
        let libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName)

        if not (libraryPath = null) then
            this.LoadUnmanagedDllFromPath libraryPath
        else
            IntPtr.Zero

let loadPlugin (pluginPath: string) =
    printfn "Loading plugin from: %s" pluginPath
    let loadContext = new PluginLoadContext(pluginPath)

    let assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(pluginPath))

    loadContext.LoadFromAssemblyName(assemblyName)

let createPlugin (assembly: Assembly) =
    assembly.GetTypes()
    |> Array.choose (fun t ->
        if t.IsAssignableTo typeof<WebserverPlugin> then
            Activator.CreateInstance t
            |> Option.ofObj
            |> Option.map unbox<WebserverPlugin>
        else
            None)
    |> List.ofArray

let loadPlugins () =
    let pluginPath = "./plugins"
    Directory.CreateDirectory pluginPath |> ignore

    Directory.EnumerateFiles(pluginPath, "*.plugin.dll", SearchOption.AllDirectories)
    |> Seq.collect (loadPlugin >> createPlugin)
    |> (fun plugins ->
        printfn
            "Loaded plugins: %s"
            (plugins
             |> Seq.map (fun plugin -> plugin.getMetaData().name)
             |> String.concat ",")

        plugins)

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    // https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
    let plugins = loadPlugins ()

    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseWebRoot(webRoot)
                .UseUrls("http://localhost:8084")
                .Configure(Action<IApplicationBuilder>(configureApp plugins))
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
