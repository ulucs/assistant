module Agent.Web.Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Giraffe
open Agent.Core.LLM
open Agent.Storage.Database
open Agent.Storage.Migrations
open Agent.Executor.CommandRunner

let configureServices (services: IServiceCollection) =
    // Configure database
    let dbConfig = DbConfig.defaultConfig
    services.AddSingleton<DbConfig>(dbConfig) |> ignore

    // Configure LLM
    let llmConfig = LLMConfig.defaultConfig
    services.AddSingleton<LLMConfig>(llmConfig) |> ignore

    // Configure executor
    let execConfig = ExecutionConfig.defaultConfig
    services.AddSingleton<ExecutionConfig>(execConfig) |> ignore

    // Add Giraffe
    services.AddGiraffe() |> ignore

    // Add CORS
    services.AddCors(fun options ->
        options.AddDefaultPolicy(fun builder ->
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            |> ignore
        )
    ) |> ignore

let configureApp (app: IApplicationBuilder) =
    app.UseCors() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseGiraffe(Api.webApp)

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    printfn "🤖 Starting F# Assistant Agent"
    printfn "=============================="

    // Initialize database
    let dbConfig = DbConfig.defaultConfig
    printfn "📦 Initializing database at: %s" dbConfig.ConnectionString
    Migrations.initialize dbConfig

    // Configure web host
    let builder = WebApplication.CreateBuilder(args)

    // Configure services
    configureServices builder.Services
    configureLogging builder.Logging

    let app = builder.Build()

    // Configure middleware
    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    configureApp app

    printfn ""
    printfn "🌐 Server starting..."
    printfn "   Web UI:  http://localhost:5000"
    printfn "   API:     http://localhost:5000/api"
    printfn ""
    printfn "📝 Remember to start LiteLLM proxy:"
    printfn "   litellm --config litellm/config.yaml"
    printfn ""
    printfn "Press Ctrl+C to stop"
    printfn "=============================="

    app.Run()

    0
