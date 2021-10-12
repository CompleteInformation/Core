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
    let api: IUserApi = { get = getUser }
    buildApi api

let buildApis plugins =
    let pluginApis =
        List.map (fun (plugin: IWebserverPlugin) -> plugin.getApi) plugins

    [ getUserApi; yield! pluginApis ]
    |> List.map (fun build -> build ())

let buildWebApp plugins =
    choose [
        route "/" >=> text "Server is running."
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
    let env =
        app.ApplicationServices.GetService<IWebHostEnvironment>()

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

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    // TODO: We read out all plugins for the server
    // https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
    let plugins = []

    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
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
