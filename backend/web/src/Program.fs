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
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open CompleteInformation.Core.Api
open CompleteInformation.Core.PluginBase

type Program = unit

let getUser userId =
    match userId with
    | UserId 1u -> async { return Some { id = UserId 1u; name = "Nico" } }
    | _ -> async { return None }

let buildApi api =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder (sprintf "/api/%s/%s")
    |> Remoting.fromValue api
    |> Remoting.buildHttpHandler

let getUserApi () =
    let api: IUserApi =
        {
            geti = getUser (UserId 1u)
            get = getUser
        }

    buildApi api

let buildApis plugins =
    let pluginApis = Seq.map (fun (plugin: IWebserverPlugin) -> plugin.getApi) plugins

    [ getUserApi; yield! pluginApis ]
    |> Seq.map (fun build -> build ())

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
            app.UseDeveloperExceptionPage()
        | false ->
            printfn "Production mode"

            app
                .UseGiraffeErrorHandler(errorHandler)
                .UseHttpsRedirection()

    app
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(buildWebApp plugins)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

open System.IO
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

(*override this.LoadUnmanagedDll(unmanagedDllName: string) : IntPtr =
        resolver.ResolveUnmanagedDllToPath(unmanagedDllName)
        |> Option.ofObj
        |> function
            | Some libraryPath -> this.LoadUnmanagedDllFromPath libraryPath
            | None -> IntPtr.Zero*)

let loadPlugin (relativePath: string) =
    let root =
        typeof<Program>.Assembly.Location
        |> Path.GetDirectoryName
        |> Path.GetDirectoryName
        |> Path.GetDirectoryName
        |> Path.GetDirectoryName
        |> Path.GetDirectoryName
        |> Path.Combine
        |> Path.GetFullPath

    let pluginLocation =
        Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar))
        |> Path.GetFullPath

    printfn "Loading commands from: %s" pluginLocation
    let loadContext = new PluginLoadContext(pluginLocation)

    let assemblyName =
        new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation))

    loadContext.LoadFromAssemblyName(assemblyName)

let createPlugin (assembly: Assembly) =
    assembly.GetTypes()
    |> Array.choose (fun t ->
        if typeof<IWebserverPlugin>.IsAssignableFrom (t) then
            Activator.CreateInstance t
            |> Option.ofObj
            |> Option.map unbox<IWebserverPlugin>
        else
            None)
    |> List.ofArray

let loadPlugins () =
    let pluginPath = "./plugins"
    Directory.CreateDirectory pluginPath |> ignore

    Directory.EnumerateFiles(pluginPath, "*.dll")
    |> Seq.collect (loadPlugin >> createPlugin)

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    // TODO: We read out all plugins for the server
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
