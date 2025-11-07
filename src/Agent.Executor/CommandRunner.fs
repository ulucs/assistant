module Agent.Executor.CommandRunner

open System
open System.Diagnostics
open Agent.Core.Domain
open Agent.Executor.Whitelist

/// Configuration for command execution
type ExecutionConfig = {
    WorkingDirectory: string
    TimeoutSeconds: int
    MaxOutputLength: int
}

module ExecutionConfig =
    let defaultConfig = {
        WorkingDirectory = Environment.CurrentDirectory
        TimeoutSeconds = 30
        MaxOutputLength = 100_000
    }

/// Execute a shell command
let executeCommand (config: ExecutionConfig) (command: string) : Async<Result<ExecutionResult * Command, string>> =
    async {
        // Validate command first
        match validateCommand command with
        | Error err -> return Error err
        | Ok () ->

        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "/bin/bash"
        startInfo.Arguments <- sprintf "-c \"%s\"" (command.Replace("\"", "\\\""))
        startInfo.WorkingDirectory <- config.WorkingDirectory
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false
        startInfo.CreateNoWindow <- true

        try
            use proc = new Process()
            proc.StartInfo <- startInfo

            let outputBuilder = System.Text.StringBuilder()
            let errorBuilder = System.Text.StringBuilder()

            proc.OutputDataReceived.Add(fun args ->
                if args.Data <> null then
                    outputBuilder.AppendLine(args.Data) |> ignore)

            proc.ErrorDataReceived.Add(fun args ->
                if args.Data <> null then
                    errorBuilder.AppendLine(args.Data) |> ignore)

            proc.Start() |> ignore
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()

            // Wait with timeout
            let hasExited = proc.WaitForExit(config.TimeoutSeconds * 1000)

            if not hasExited then
                proc.Kill()
                return Error (sprintf "Command timed out after %d seconds" config.TimeoutSeconds)
            else
                let output = outputBuilder.ToString()
                let error = errorBuilder.ToString()
                let exitCode = proc.ExitCode

                // Truncate output if too long
                let truncatedOutput =
                    if output.Length > config.MaxOutputLength then
                        output.Substring(0, config.MaxOutputLength) + "\n\n[Output truncated...]"
                    else
                        output

                let errorMsg =
                    if String.IsNullOrWhiteSpace(error) then None
                    else Some error

                let dummyCommandId = Guid.NewGuid()
                let result = ExecutionResult.create dummyCommandId truncatedOutput exitCode errorMsg

                // Create a dummy command for now (will be replaced by caller)
                let dummyCommand = {
                    Id = dummyCommandId
                    InputId = Guid.Empty
                    Command = command
                    Reasoning = ""
                    Status = if exitCode = 0 then Completed else Failed
                    CreatedAt = DateTime.UtcNow
                    ReviewedAt = None
                    ExecutedAt = Some DateTime.UtcNow
                    IsWhitelisted = isWhitelisted command
                }

                return Ok (result, dummyCommand)

        with
        | ex ->
            return Error (sprintf "Execution failed: %s" ex.Message)
    }

/// Execute a command from the Command domain type
let executeCommandFromDomain (config: ExecutionConfig) (cmd: Command) : Async<Result<ExecutionResult, string>> =
    async {
        let! result = executeCommand config cmd.Command

        match result with
        | Ok (execResult, _) ->
            // Update the result with the correct command ID
            let updatedResult = { execResult with CommandId = cmd.Id }
            return Ok updatedResult
        | Error err ->
            return Error err
    }
