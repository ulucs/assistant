module Agent.Storage.Tests.QueriesTests

open System
open System.IO
open Expecto
open Agent.Core.Domain
open Agent.Storage.Database
open Agent.Storage.Migrations
open Agent.Storage.Queries

let createTestDb () =
    let testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db")
    let config = DbConfig.fromFile testDbPath
    initialize config
    config

let cleanupTestDb (config: DbConfig) =
    let connectionString = config.ConnectionString
    let dbPath = connectionString.Replace("Data Source=", "")
    if File.Exists(dbPath) then
        File.Delete(dbPath)

[<Tests>]
let queriesTests =
    testList "Queries Tests" [
        testList "Input Queries" [
            testAsync "insertInput should save input to database" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test content"
                    do! insertInput config input

                    let! retrieved = getInput config input.Id

                    match retrieved with
                    | Some i ->
                        Expect.equal i.Id input.Id "ID should match"
                        Expect.equal i.Source input.Source "Source should match"
                        Expect.equal i.Content input.Content "Content should match"
                    | None ->
                        failtest "Input should be retrieved"
                finally
                    cleanupTestDb config
            }

            testAsync "getInput should return None for non-existent ID" {
                let config = createTestDb ()

                try
                    let! result = getInput config (Guid.NewGuid())
                    Expect.isNone result "Should return None for non-existent ID"
                finally
                    cleanupTestDb config
            }

            testAsync "getAllInputs should return all inputs" {
                let config = createTestDb ()

                try
                    let input1 = Input.create WebUI "Test 1"
                    let input2 = Input.create Email "Test 2"

                    do! insertInput config input1
                    do! insertInput config input2

                    let! inputs = getAllInputs config

                    Expect.equal inputs.Length 2 "Should have 2 inputs"
                finally
                    cleanupTestDb config
            }

            testAsync "getAllInputs should return empty list when no inputs" {
                let config = createTestDb ()

                try
                    let! inputs = getAllInputs config
                    Expect.isEmpty inputs "Should return empty list"
                finally
                    cleanupTestDb config
            }
        ]

        testList "Command Queries" [
            testAsync "insertCommand should save command to database" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "ls -la" "List files" true
                    do! insertCommand config cmd

                    let! retrieved = getCommand config cmd.Id

                    match retrieved with
                    | Some c ->
                        Expect.equal c.Id cmd.Id "ID should match"
                        Expect.equal c.Command cmd.Command "Command should match"
                        Expect.equal c.Reasoning cmd.Reasoning "Reasoning should match"
                        Expect.equal c.Status cmd.Status "Status should match"
                        Expect.equal c.IsWhitelisted cmd.IsWhitelisted "IsWhitelisted should match"
                    | None ->
                        failtest "Command should be retrieved"
                finally
                    cleanupTestDb config
            }

            testAsync "updateCommandStatus should update command status" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "ls" "test" false
                    do! insertCommand config cmd

                    do! updateCommandStatus config cmd.Id Approved (Some DateTime.UtcNow)

                    let! retrieved = getCommand config cmd.Id

                    match retrieved with
                    | Some c ->
                        Expect.equal c.Status Approved "Status should be Approved"
                        Expect.isSome c.ReviewedAt "ReviewedAt should be set"
                    | None ->
                        failtest "Command should be retrieved"
                finally
                    cleanupTestDb config
            }

            testAsync "getCommandsByStatus should filter by status" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd1 = Command.create input.Id "ls" "test1" false
                    let cmd2 = Command.create input.Id "pwd" "test2" false
                    let cmd3 = Command.create input.Id "cat" "test3" false

                    do! insertCommand config cmd1
                    do! insertCommand config cmd2
                    do! insertCommand config cmd3

                    do! updateCommandStatus config cmd2.Id Approved None

                    let! pending = getCommandsByStatus config Pending
                    let! approved = getCommandsByStatus config Approved

                    Expect.equal pending.Length 2 "Should have 2 pending commands"
                    Expect.equal approved.Length 1 "Should have 1 approved command"
                finally
                    cleanupTestDb config
            }

            testAsync "getAllCommands should return all commands" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd1 = Command.create input.Id "ls" "test1" false
                    let cmd2 = Command.create input.Id "pwd" "test2" true

                    do! insertCommand config cmd1
                    do! insertCommand config cmd2

                    let! commands = getAllCommands config

                    Expect.equal commands.Length 2 "Should have 2 commands"
                finally
                    cleanupTestDb config
            }
        ]

        testList "ExecutionResult Queries" [
            testAsync "insertExecutionResult should save result to database" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "ls" "test" false
                    do! insertCommand config cmd

                    let result = ExecutionResult.create cmd.Id "output text" 0 None
                    do! insertExecutionResult config result

                    let! retrieved = getExecutionResultByCommandId config cmd.Id

                    match retrieved with
                    | Some r ->
                        Expect.equal r.CommandId cmd.Id "CommandId should match"
                        Expect.equal r.Output "output text" "Output should match"
                        Expect.equal r.ExitCode 0 "ExitCode should match"
                        Expect.isNone r.Error "Error should be None"
                    | None ->
                        failtest "Result should be retrieved"
                finally
                    cleanupTestDb config
            }

            testAsync "insertExecutionResult should handle errors" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "ls" "test" false
                    do! insertCommand config cmd

                    let result = ExecutionResult.create cmd.Id "output" 1 (Some "error message")
                    do! insertExecutionResult config result

                    let! retrieved = getExecutionResultByCommandId config cmd.Id

                    match retrieved with
                    | Some r ->
                        Expect.equal r.ExitCode 1 "ExitCode should be 1"
                        Expect.equal r.Error (Some "error message") "Error should be set"
                    | None ->
                        failtest "Result should be retrieved"
                finally
                    cleanupTestDb config
            }

            testAsync "getAllExecutionResults should return all results" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd1 = Command.create input.Id "ls" "test1" false
                    let cmd2 = Command.create input.Id "pwd" "test2" false

                    do! insertCommand config cmd1
                    do! insertCommand config cmd2

                    let result1 = ExecutionResult.create cmd1.Id "output1" 0 None
                    let result2 = ExecutionResult.create cmd2.Id "output2" 0 None

                    do! insertExecutionResult config result1
                    do! insertExecutionResult config result2

                    let! results = getAllExecutionResults config

                    Expect.equal results.Length 2 "Should have 2 results"
                finally
                    cleanupTestDb config
            }
        ]

        testList "Parse Functions" [
            test "parseInputSource should handle all sources" {
                Expect.equal (parseInputSource "webui") WebUI "Should parse webui"
                Expect.equal (parseInputSource "email") Email "Should parse email"
                Expect.equal (parseInputSource "api") API "Should parse api"
                Expect.equal (parseInputSource "unknown") WebUI "Should default to WebUI"
            }

            test "inputSourceToString should convert all sources" {
                Expect.equal (inputSourceToString WebUI) "webui" "Should convert WebUI"
                Expect.equal (inputSourceToString Email) "email" "Should convert Email"
                Expect.equal (inputSourceToString API) "api" "Should convert API"
            }

            test "parseCommandStatus should handle all statuses" {
                Expect.equal (parseCommandStatus "pending") Pending "Should parse pending"
                Expect.equal (parseCommandStatus "approved") Approved "Should parse approved"
                Expect.equal (parseCommandStatus "rejected") Rejected "Should parse rejected"
                Expect.equal (parseCommandStatus "executing") Executing "Should parse executing"
                Expect.equal (parseCommandStatus "completed") Completed "Should parse completed"
                Expect.equal (parseCommandStatus "failed") Failed "Should parse failed"
                Expect.equal (parseCommandStatus "unknown") Pending "Should default to Pending"
            }

            test "commandStatusToString should convert all statuses" {
                Expect.equal (commandStatusToString Pending) "pending" "Should convert Pending"
                Expect.equal (commandStatusToString Approved) "approved" "Should convert Approved"
                Expect.equal (commandStatusToString Rejected) "rejected" "Should convert Rejected"
                Expect.equal (commandStatusToString Executing) "executing" "Should convert Executing"
                Expect.equal (commandStatusToString Completed) "completed" "Should convert Completed"
                Expect.equal (commandStatusToString Failed) "failed" "Should convert Failed"
            }
        ]
    ]
