module Agent.Web.Tests.ApiTests

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
let apiTests =
    testList "API Tests" [
        testList "Request/Response Types" [
            test "should handle input creation" {
                let input = Input.create WebUI "Test task"

                Expect.isNotEmpty (input.Id.ToString()) "Input should have ID"
                Expect.equal input.Source WebUI "Source should be WebUI"
                Expect.equal input.Content "Test task" "Content should match"
            }

            test "should handle command creation with whitelist flag" {
                let cmd = Command.create Guid.Empty "ls -la" "List files" true

                Expect.isTrue cmd.IsWhitelisted "Should be whitelisted"
                Expect.equal cmd.Status Pending "Should start as Pending"
            }
        ]

        testList "Database Integration" [
            testAsync "should store and retrieve input through database" {
                let config = createTestDb ()

                try
                    let input = Input.create Email "Test email content"
                    do! insertInput config input

                    let! retrieved = getInput config input.Id

                    match retrieved with
                    | Some i ->
                        Expect.equal i.Content input.Content "Content should match"
                        Expect.equal i.Source Email "Source should be Email"
                    | None ->
                        failtest "Input should be retrievable"
                finally
                    cleanupTestDb config
            }

            testAsync "should handle command approval workflow" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "pwd" "Show directory" false
                    do! insertCommand config cmd

                    // Approve command
                    do! updateCommandStatus config cmd.Id Approved (Some DateTime.UtcNow)

                    let! retrieved = getCommand config cmd.Id

                    match retrieved with
                    | Some c ->
                        Expect.equal c.Status Approved "Status should be Approved"
                        Expect.isSome c.ReviewedAt "ReviewedAt should be set"
                    | None ->
                        failtest "Command should be retrievable"
                finally
                    cleanupTestDb config
            }

            testAsync "should handle command rejection workflow" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "rm file" "Remove file" false
                    do! insertCommand config cmd

                    // Reject command
                    do! updateCommandStatus config cmd.Id Rejected (Some DateTime.UtcNow)

                    let! retrieved = getCommand config cmd.Id

                    match retrieved with
                    | Some c ->
                        Expect.equal c.Status Rejected "Status should be Rejected"
                    | None ->
                        failtest "Command should be retrievable"
                finally
                    cleanupTestDb config
            }

            testAsync "should store execution results" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "echo test" "Test echo" false
                    do! insertCommand config cmd

                    let result = ExecutionResult.create cmd.Id "test output" 0 None
                    do! insertExecutionResult config result

                    let! retrieved = getExecutionResultByCommandId config cmd.Id

                    match retrieved with
                    | Some r ->
                        Expect.equal r.Output "test output" "Output should match"
                        Expect.equal r.ExitCode 0 "Exit code should be 0"
                    | None ->
                        failtest "Result should be retrievable"
                finally
                    cleanupTestDb config
            }
        ]

        testList "Command Status Transitions" [
            testAsync "should transition from Pending to Approved to Executing to Completed" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "pwd" "Test" false
                    do! insertCommand config cmd

                    // Check initial status
                    let! cmd1 = getCommand config cmd.Id
                    Expect.equal cmd1.Value.Status Pending "Should start as Pending"

                    // Approve
                    do! updateCommandStatus config cmd.Id Approved (Some DateTime.UtcNow)
                    let! cmd2 = getCommand config cmd.Id
                    Expect.equal cmd2.Value.Status Approved "Should be Approved"

                    // Start executing
                    do! updateCommandStatus config cmd.Id Executing None
                    let! cmd3 = getCommand config cmd.Id
                    Expect.equal cmd3.Value.Status Executing "Should be Executing"

                    // Complete
                    do! updateCommandStatus config cmd.Id Completed None
                    let! cmd4 = getCommand config cmd.Id
                    Expect.equal cmd4.Value.Status Completed "Should be Completed"
                finally
                    cleanupTestDb config
            }

            testAsync "should transition from Pending to Rejected" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "dangerous" "Test" false
                    do! insertCommand config cmd

                    do! updateCommandStatus config cmd.Id Rejected (Some DateTime.UtcNow)
                    let! retrieved = getCommand config cmd.Id

                    Expect.equal retrieved.Value.Status Rejected "Should be Rejected"
                finally
                    cleanupTestDb config
            }

            testAsync "should handle failed execution" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd = Command.create input.Id "false" "Test failure" false
                    do! insertCommand config cmd

                    do! updateCommandStatus config cmd.Id Approved (Some DateTime.UtcNow)
                    do! updateCommandStatus config cmd.Id Executing None
                    do! updateCommandStatus config cmd.Id Failed None

                    let! retrieved = getCommand config cmd.Id
                    Expect.equal retrieved.Value.Status Failed "Should be Failed"
                finally
                    cleanupTestDb config
            }
        ]

        testList "Filtering and Queries" [
            testAsync "should filter commands by status" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd1 = Command.create input.Id "ls" "Test1" false
                    let cmd2 = Command.create input.Id "pwd" "Test2" false
                    let cmd3 = Command.create input.Id "whoami" "Test3" false

                    do! insertCommand config cmd1
                    do! insertCommand config cmd2
                    do! insertCommand config cmd3

                    do! updateCommandStatus config cmd1.Id Approved None
                    do! updateCommandStatus config cmd2.Id Approved None

                    let! pending = getCommandsByStatus config Pending
                    let! approved = getCommandsByStatus config Approved

                    Expect.equal pending.Length 1 "Should have 1 pending"
                    Expect.equal approved.Length 2 "Should have 2 approved"
                finally
                    cleanupTestDb config
            }

            testAsync "should retrieve all commands ordered by creation time" {
                let config = createTestDb ()

                try
                    let input = Input.create WebUI "Test"
                    do! insertInput config input

                    let cmd1 = Command.create input.Id "ls" "First" false
                    do! Async.Sleep 10 // Small delay to ensure different timestamps
                    let cmd2 = Command.create input.Id "pwd" "Second" false

                    do! insertCommand config cmd1
                    do! insertCommand config cmd2

                    let! allCommands = getAllCommands config

                    Expect.equal allCommands.Length 2 "Should have 2 commands"
                    // Most recent first
                    Expect.equal allCommands.[0].Reasoning "Second" "Should be ordered by creation time"
                finally
                    cleanupTestDb config
            }
        ]
    ]
