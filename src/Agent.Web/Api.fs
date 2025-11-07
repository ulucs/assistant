module Agent.Web.Api

open System
open Microsoft.AspNetCore.Http
open Giraffe
open FSharp.Control.Tasks
open Agent.Core.Domain
open Agent.Core.LLM
open Agent.Core.Agent
open Agent.Storage.Database
open Agent.Storage.Queries
open Agent.Executor.CommandRunner

// ===== REQUEST/RESPONSE TYPES =====

type SubmitInputRequest = {
    Content: string
    Source: string option
}

type ApproveCommandRequest = {
    CommandId: string
}

type RejectCommandRequest = {
    CommandId: string
}

type ExecuteCommandRequest = {
    CommandId: string
}

// ===== HELPERS =====

let getDbConfig (ctx: HttpContext) =
    ctx.GetService<DbConfig>()

let getLLMConfig (ctx: HttpContext) =
    ctx.GetService<LLMConfig>()

let getExecutionConfig (ctx: HttpContext) =
    ctx.GetService<ExecutionConfig>()

// ===== API HANDLERS =====

/// Submit a new input for the agent to process
let submitInput : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request = ctx.BindJsonAsync<SubmitInputRequest>()
            let dbConfig = getDbConfig ctx
            let llmConfig = getLLMConfig ctx

            // Parse source
            let source =
                match request.Source with
                | Some "email" -> Email
                | Some "api" -> API
                | _ -> WebUI

            // Create input
            let input = Input.create source request.Content

            // Save input
            do! insertInput dbConfig input

            // Process input through agent
            let! agentResult = processInput llmConfig input

            match agentResult with
            | Success commands ->
                // Save all commands
                for cmd in commands do
                    do! insertCommand dbConfig cmd

                return! json {| success = true; inputId = input.Id; commandCount = commands.Length |} next ctx

            | NoActionNeeded msg ->
                return! json {| success = true; inputId = input.Id; message = msg; commandCount = 0 |} next ctx

            | LLMError err ->
                return! RequestErrors.BAD_REQUEST {| success = false; error = err |} next ctx

            | ParseError err ->
                return! RequestErrors.BAD_REQUEST {| success = false; error = err |} next ctx
        }

/// Get all inputs
let getInputs : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx
            let! inputs = getAllInputs dbConfig
            return! json inputs next ctx
        }

/// Get all commands
let getCommands : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx
            let! commands = getAllCommands dbConfig
            return! json commands next ctx
        }

/// Get pending commands
let getPendingCommands : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx
            let! commands = getCommandsByStatus dbConfig Pending
            return! json commands next ctx
        }

/// Approve a command
let approveCommand (commandId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx

            match Guid.TryParse(commandId) with
            | false, _ ->
                return! RequestErrors.BAD_REQUEST {| success = false; error = "Invalid command ID" |} next ctx
            | true, guid ->
                let! cmdOpt = getCommand dbConfig guid

                match cmdOpt with
                | None ->
                    return! RequestErrors.NOT_FOUND {| success = false; error = "Command not found" |} next ctx
                | Some cmd ->
                    // Update status to approved
                    do! updateCommandStatus dbConfig guid Approved (Some DateTime.UtcNow)

                    return! json {| success = true; commandId = guid |} next ctx
        }

/// Reject a command
let rejectCommand (commandId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx

            match Guid.TryParse(commandId) with
            | false, _ ->
                return! RequestErrors.BAD_REQUEST {| success = false; error = "Invalid command ID" |} next ctx
            | true, guid ->
                let! cmdOpt = getCommand dbConfig guid

                match cmdOpt with
                | None ->
                    return! RequestErrors.NOT_FOUND {| success = false; error = "Command not found" |} next ctx
                | Some cmd ->
                    // Update status to rejected
                    do! updateCommandStatus dbConfig guid Rejected (Some DateTime.UtcNow)

                    return! json {| success = true; commandId = guid |} next ctx
        }

/// Execute a command
let executeCommand (commandId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx
            let execConfig = getExecutionConfig ctx

            match Guid.TryParse(commandId) with
            | false, _ ->
                return! RequestErrors.BAD_REQUEST {| success = false; error = "Invalid command ID" |} next ctx
            | true, guid ->
                let! cmdOpt = getCommand dbConfig guid

                match cmdOpt with
                | None ->
                    return! RequestErrors.NOT_FOUND {| success = false; error = "Command not found" |} next ctx
                | Some cmd ->
                    // Check if command is approved
                    if cmd.Status <> Approved then
                        return! RequestErrors.BAD_REQUEST {| success = false; error = "Command must be approved first" |} next ctx
                    else
                        // Update status to executing
                        do! updateCommandStatus dbConfig guid Executing None
                        do! updateCommandExecutedAt dbConfig guid DateTime.UtcNow

                        // Execute the command
                        let! result = executeCommandFromDomain execConfig cmd

                        match result with
                        | Ok execResult ->
                            // Save execution result
                            do! insertExecutionResult dbConfig execResult

                            // Update command status
                            let newStatus = if execResult.ExitCode = 0 then Completed else Failed
                            do! updateCommandStatus dbConfig guid newStatus None

                            return! json {| success = true; result = execResult |} next ctx

                        | Error err ->
                            // Mark as failed
                            do! updateCommandStatus dbConfig guid Failed None

                            return! RequestErrors.INTERNAL_ERROR {| success = false; error = err |} next ctx
        }

/// Get all execution results
let getResults : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx
            let! results = getAllExecutionResults dbConfig
            return! json results next ctx
        }

/// Get execution result for a specific command
let getResultByCommandId (commandId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let dbConfig = getDbConfig ctx

            match Guid.TryParse(commandId) with
            | false, _ ->
                return! RequestErrors.BAD_REQUEST {| success = false; error = "Invalid command ID" |} next ctx
            | true, guid ->
                let! resultOpt = getExecutionResultByCommandId dbConfig guid

                match resultOpt with
                | None ->
                    return! RequestErrors.NOT_FOUND {| success = false; error = "Result not found" |} next ctx
                | Some result ->
                    return! json result next ctx
        }

// ===== ROUTES =====

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/" >=> htmlFile "wwwroot/index.html"
            route "/api/inputs" >=> getInputs
            route "/api/commands" >=> getCommands
            route "/api/commands/pending" >=> getPendingCommands
            route "/api/results" >=> getResults
            routef "/api/results/%s" getResultByCommandId
        ]
        POST >=> choose [
            route "/api/inputs" >=> submitInput
            routef "/api/commands/%s/approve" approveCommand
            routef "/api/commands/%s/reject" rejectCommand
            routef "/api/commands/%s/execute" executeCommand
        ]
        setStatusCode 404 >=> text "Not Found"
    ]
