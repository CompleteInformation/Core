namespace CompleteInformation.Core.Backend.Web

open System
open System.IO

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe

open CompleteInformation.Base.Backend.Web

type Program = unit

[<AutoOpen>]
module Helper =
    let routeBuilder = sprintf "/api/%s/%s"

    let buildApis devMode plugins =
        let pluginApis =
            Seq.map (fun (plugin: WebserverPlugin) -> plugin.getApi devMode) plugins

        [ getPluginApi devMode plugins; getUserApi devMode; yield! pluginApis ]
        |> Seq.map (fun f -> f routeBuilder)

    let buildWebApp devMode plugins =
        choose [
#if DEBUG
            route "/" >=> text "Server is running."
#else
            route "/" >=> redirectTo true "/index.html"
#endif
            yield! buildApis devMode plugins
            setStatusCode 404 >=> text "Not Found"
        ]

    // ---------------------------------
    // Error handler
    // ---------------------------------

    let errorHandler (ex: Exception) (logger: ILogger) =
        logger.LogError(ex, "An unhanded exception has occurred while executing the request.")

        clearResponse >=> setStatusCode 500 >=> text ex.Message

    // ---------------------------------
    // Config and Main
    // ---------------------------------

    let configureCors (builder: CorsPolicyBuilder) =
        builder
            .WithOrigins([| "http://localhost:8082"; "http://127.0.0.1:8082" |])
            .AllowAnyMethod()
            .AllowAnyHeader()
        |> ignore

    let configureApp plugins (app: IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

        let devMode = env.IsDevelopment()

        let app =
            match devMode with
            | true ->
                printfn "[Server - Base] Development mode"

                app.UseDeveloperExceptionPage().UseCors(configureCors)
            | false ->
                printfn "[Server - Base] Production mode"

                app.UseGiraffeErrorHandler(errorHandler).UseHttpsRedirection()

        app.UseStaticFiles().UseGiraffe(buildWebApp devMode plugins)

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
                Activator.CreateInstance t |> Option.ofObj |> Option.map unbox<WebserverPlugin>
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

module Program =
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
