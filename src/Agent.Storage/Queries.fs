module Agent.Storage.Queries

open System
open Donald
open Agent.Core.Domain
open Agent.Storage.Database

// ===== INPUT QUERIES =====

/// Parse InputSource from string
let parseInputSource (s: string) : InputSource =
    match s.ToLower() with
    | "webui" -> WebUI
    | "email" -> Email
    | "api" -> API
    | _ -> WebUI

/// Convert InputSource to string
let inputSourceToString (source: InputSource) : string =
    match source with
    | WebUI -> "webui"
    | Email -> "email"
    | API -> "api"

/// Insert a new input
let insertInput (config: DbConfig) (input: Input) : Async<unit> =
    withConnectionAsync config (fun conn ->
        async {
            dbCommand conn {
                cmdText """
                    INSERT INTO inputs (id, source, content, timestamp)
                    VALUES (@id, @source, @content, @timestamp)
                """
                cmdParam [ "id", SqlType.String (input.Id.ToString())
                          "source", SqlType.String (inputSourceToString input.Source)
                          "content", SqlType.String input.Content
                          "timestamp", SqlType.String (input.Timestamp.ToString("o")) ]
            }
            |> DbConn.exec

            return ()
        })

/// Get an input by ID
let getInput (config: DbConfig) (id: Guid) : Async<Input option> =
    withConnectionAsync config (fun conn ->
        async {
            let! results =
                dbCommand conn {
                    cmdText "SELECT id, source, content, timestamp FROM inputs WHERE id = @id"
                    cmdParam [ "id", SqlType.String (id.ToString()) ]
                }
                |> DbConn.query (fun rd ->
                    {
                        Id = rd.ReadGuid "id"
                        Source = rd.ReadString "source" |> parseInputSource
                        Content = rd.ReadString "content"
                        Timestamp = rd.ReadDateTime "timestamp"
                    })

            return results |> List.tryHead
        })

/// Get all inputs
let getAllInputs (config: DbConfig) : Async<Input list> =
    withConnectionAsync config (fun conn ->
        async {
            return!
                dbCommand conn {
                    cmdText "SELECT id, source, content, timestamp FROM inputs ORDER BY timestamp DESC"
                }
                |> DbConn.query (fun rd ->
                    {
                        Id = rd.ReadGuid "id"
                        Source = rd.ReadString "source" |> parseInputSource
                        Content = rd.ReadString "content"
                        Timestamp = rd.ReadDateTime "timestamp"
                    })
        })

// ===== COMMAND QUERIES =====

/// Parse CommandStatus from string
let parseCommandStatus (s: string) : CommandStatus =
    match s.ToLower() with
    | "pending" -> Pending
    | "approved" -> Approved
    | "rejected" -> Rejected
    | "executing" -> Executing
    | "completed" -> Completed
    | "failed" -> Failed
    | _ -> Pending

/// Convert CommandStatus to string
let commandStatusToString (status: CommandStatus) : string =
    match status with
    | Pending -> "pending"
    | Approved -> "approved"
    | Rejected -> "rejected"
    | Executing -> "executing"
    | Completed -> "completed"
    | Failed -> "failed"

/// Insert a new command
let insertCommand (config: DbConfig) (cmd: Command) : Async<unit> =
    withConnectionAsync config (fun conn ->
        async {
            dbCommand conn {
                cmdText """
                    INSERT INTO commands (id, input_id, command, reasoning, status, created_at, reviewed_at, executed_at, is_whitelisted)
                    VALUES (@id, @input_id, @command, @reasoning, @status, @created_at, @reviewed_at, @executed_at, @is_whitelisted)
                """
                cmdParam [
                    "id", SqlType.String (cmd.Id.ToString())
                    "input_id", SqlType.String (cmd.InputId.ToString())
                    "command", SqlType.String cmd.Command
                    "reasoning", SqlType.String cmd.Reasoning
                    "status", SqlType.String (commandStatusToString cmd.Status)
                    "created_at", SqlType.String (cmd.CreatedAt.ToString("o"))
                    "reviewed_at", SqlType.String (cmd.ReviewedAt |> Option.map (fun d -> d.ToString("o")) |> Option.defaultValue null)
                    "executed_at", SqlType.String (cmd.ExecutedAt |> Option.map (fun d -> d.ToString("o")) |> Option.defaultValue null)
                    "is_whitelisted", SqlType.Int32 (if cmd.IsWhitelisted then 1 else 0)
                ]
            }
            |> DbConn.exec

            return ()
        })

/// Update command status
let updateCommandStatus (config: DbConfig) (id: Guid) (status: CommandStatus) (reviewedAt: DateTime option) : Async<unit> =
    withConnectionAsync config (fun conn ->
        async {
            dbCommand conn {
                cmdText """
                    UPDATE commands
                    SET status = @status, reviewed_at = @reviewed_at
                    WHERE id = @id
                """
                cmdParam [
                    "id", SqlType.String (id.ToString())
                    "status", SqlType.String (commandStatusToString status)
                    "reviewed_at", SqlType.String (reviewedAt |> Option.map (fun d -> d.ToString("o")) |> Option.defaultValue null)
                ]
            }
            |> DbConn.exec

            return ()
        })

/// Update command execution time
let updateCommandExecutedAt (config: DbConfig) (id: Guid) (executedAt: DateTime) : Async<unit> =
    withConnectionAsync config (fun conn ->
        async {
            dbCommand conn {
                cmdText """
                    UPDATE commands
                    SET executed_at = @executed_at
                    WHERE id = @id
                """
                cmdParam [
                    "id", SqlType.String (id.ToString())
                    "executed_at", SqlType.String (executedAt.ToString("o"))
                ]
            }
            |> DbConn.exec

            return ()
        })

/// Read a command from the database reader
let readCommand (rd: IDataReader) : Command =
    {
        Id = rd.ReadGuid "id"
        InputId = rd.ReadGuid "input_id"
        Command = rd.ReadString "command"
        Reasoning = rd.ReadString "reasoning"
        Status = rd.ReadString "status" |> parseCommandStatus
        CreatedAt = rd.ReadDateTime "created_at"
        ReviewedAt = rd.ReadDateTimeOption "reviewed_at"
        ExecutedAt = rd.ReadDateTimeOption "executed_at"
        IsWhitelisted = rd.ReadInt32 "is_whitelisted" = 1
    }

/// Get a command by ID
let getCommand (config: DbConfig) (id: Guid) : Async<Command option> =
    withConnectionAsync config (fun conn ->
        async {
            let! results =
                dbCommand conn {
                    cmdText """
                        SELECT id, input_id, command, reasoning, status, created_at, reviewed_at, executed_at, is_whitelisted
                        FROM commands
                        WHERE id = @id
                    """
                    cmdParam [ "id", SqlType.String (id.ToString()) ]
                }
                |> DbConn.query readCommand

            return results |> List.tryHead
        })

/// Get all commands
let getAllCommands (config: DbConfig) : Async<Command list> =
    withConnectionAsync config (fun conn ->
        async {
            return!
                dbCommand conn {
                    cmdText """
                        SELECT id, input_id, command, reasoning, status, created_at, reviewed_at, executed_at, is_whitelisted
                        FROM commands
                        ORDER BY created_at DESC
                    """
                }
                |> DbConn.query readCommand
        })

/// Get commands by status
let getCommandsByStatus (config: DbConfig) (status: CommandStatus) : Async<Command list> =
    withConnectionAsync config (fun conn ->
        async {
            return!
                dbCommand conn {
                    cmdText """
                        SELECT id, input_id, command, reasoning, status, created_at, reviewed_at, executed_at, is_whitelisted
                        FROM commands
                        WHERE status = @status
                        ORDER BY created_at DESC
                    """
                    cmdParam [ "status", SqlType.String (commandStatusToString status) ]
                }
                |> DbConn.query readCommand
        })

// ===== EXECUTION RESULT QUERIES =====

/// Insert an execution result
let insertExecutionResult (config: DbConfig) (result: ExecutionResult) : Async<unit> =
    withConnectionAsync config (fun conn ->
        async {
            dbCommand conn {
                cmdText """
                    INSERT INTO execution_results (id, command_id, output, exit_code, executed_at, error)
                    VALUES (@id, @command_id, @output, @exit_code, @executed_at, @error)
                """
                cmdParam [
                    "id", SqlType.String (result.Id.ToString())
                    "command_id", SqlType.String (result.CommandId.ToString())
                    "output", SqlType.String result.Output
                    "exit_code", SqlType.Int32 result.ExitCode
                    "executed_at", SqlType.String (result.ExecutedAt.ToString("o"))
                    "error", SqlType.String (result.Error |> Option.defaultValue null)
                ]
            }
            |> DbConn.exec

            return ()
        })

/// Get execution result by command ID
let getExecutionResultByCommandId (config: DbConfig) (commandId: Guid) : Async<ExecutionResult option> =
    withConnectionAsync config (fun conn ->
        async {
            let! results =
                dbCommand conn {
                    cmdText """
                        SELECT id, command_id, output, exit_code, executed_at, error
                        FROM execution_results
                        WHERE command_id = @command_id
                    """
                    cmdParam [ "command_id", SqlType.String (commandId.ToString()) ]
                }
                |> DbConn.query (fun rd ->
                    {
                        Id = rd.ReadGuid "id"
                        CommandId = rd.ReadGuid "command_id"
                        Output = rd.ReadString "output"
                        ExitCode = rd.ReadInt32 "exit_code"
                        ExecutedAt = rd.ReadDateTime "executed_at"
                        Error = rd.ReadStringOption "error"
                    })

            return results |> List.tryHead
        })

/// Get all execution results
let getAllExecutionResults (config: DbConfig) : Async<ExecutionResult list> =
    withConnectionAsync config (fun conn ->
        async {
            return!
                dbCommand conn {
                    cmdText """
                        SELECT id, command_id, output, exit_code, executed_at, error
                        FROM execution_results
                        ORDER BY executed_at DESC
                    """
                }
                |> DbConn.query (fun rd ->
                    {
                        Id = rd.ReadGuid "id"
                        CommandId = rd.ReadGuid "command_id"
                        Output = rd.ReadString "output"
                        ExitCode = rd.ReadInt32 "exit_code"
                        ExecutedAt = rd.ReadDateTime "executed_at"
                        Error = rd.ReadStringOption "error"
                    })
        })
