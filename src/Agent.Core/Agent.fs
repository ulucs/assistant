module Agent.Core.Agent

open System
open Agent.Core.Domain
open Agent.Core.LLM

/// Check if a command is whitelisted (read-only)
let isWhitelisted (command: string) : bool =
    let whitelistedCommands = [
        // File operations (read-only)
        "ls"; "cat"; "head"; "tail"; "grep"; "find"; "tree"; "pwd"
        // Git (read-only)
        "git status"; "git log"; "git diff"; "git show"; "git branch"
        // System info
        "df"; "du"; "ps"; "whoami"; "uname"; "date"
        // Help/documentation
        "tldr"; "man"; "help"; "which"; "type"
    ]

    let commandLower = command.Trim().ToLower()

    // Check if command starts with any whitelisted command
    whitelistedCommands
    |> List.exists (fun wl -> commandLower.StartsWith(wl))

/// Process an input and generate commands
let processInput (config: LLMConfig) (input: Input) : Async<AgentResult> =
    async {
        let! result = LLM.processInput config input

        match result with
        | Ok parsedCommands when parsedCommands.IsEmpty ->
            return NoActionNeeded "LLM determined no commands are needed"

        | Ok parsedCommands ->
            let commands =
                parsedCommands
                |> List.map (fun pc ->
                    Command.create
                        input.Id
                        pc.Command
                        pc.Reasoning
                        (isWhitelisted pc.Command))

            return Success commands

        | Error errorMsg ->
            return LLMError errorMsg
    }

/// Process a batch of inputs
let processInputs (config: LLMConfig) (inputs: Input list) : Async<AgentResult list> =
    async {
        let! results =
            inputs
            |> List.map (processInput config)
            |> Async.Parallel

        return results |> Array.toList
    }
