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

let getUser userId =
    match userId with
    | UserId 1u -> async { return Some { id = UserId 1u; name = "Nico" } }
    | _ -> async { return None }

let userApi: IUserApi = { get = getUser }

let remotingApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder (sprintf "/api/%s/%s")
    |> Remoting.fromValue userApi
    |> Remoting.buildHttpHandler

let webApp: HttpHandler =
    choose [
        route "/" >=> text "Server is running."
        remotingApi
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

let configureApp (app: IApplicationBuilder) =
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
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    // At first, we have to determine the secret for our JWT Tokens
    // TODO: generate
    let secret =
        "asdinf28hßrq82h389hr2_83h8h3r3.q30hq2893hrvq9ß23hc"

    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseContentRoot(contentRoot)
                .UseWebRoot(webRoot)
                .UseUrls("http://localhost:8084")
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
