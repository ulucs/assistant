module Agent.Executor.Tests.CommandRunnerTests

open System
open Expecto
open Agent.Core.Domain
open Agent.Executor.CommandRunner

[<Tests>]
let commandRunnerTests =
    testList "CommandRunner Tests" [
        testList "ExecutionConfig" [
            test "defaultConfig should have sensible defaults" {
                let config = ExecutionConfig.defaultConfig

                Expect.isNotEmpty config.WorkingDirectory "WorkingDirectory should not be empty"
                Expect.isGreaterThan config.TimeoutSeconds 0 "TimeoutSeconds should be positive"
                Expect.isGreaterThan config.MaxOutputLength 0 "MaxOutputLength should be positive"
            }
        ]

        testList "executeCommand" [
            testAsync "should execute simple echo command" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config "echo 'test'"

                match result with
                | Ok (execResult, _) ->
                    Expect.stringContains execResult.Output "test" "Output should contain test"
                    Expect.equal execResult.ExitCode 0 "Exit code should be 0"
                | Error err ->
                    failtest $"Should succeed: {err}"
            }

            testAsync "should capture stdout" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config "echo 'hello world'"

                match result with
                | Ok (execResult, _) ->
                    Expect.stringContains execResult.Output "hello world" "Should capture stdout"
                | Error err ->
                    failtest $"Should succeed: {err}"
            }

            testAsync "should handle command failure" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config "ls /nonexistent/path/that/does/not/exist"

                match result with
                | Ok (execResult, _) ->
                    Expect.notEqual execResult.ExitCode 0 "Exit code should be non-zero for failure"
                    Expect.isSome execResult.Error "Error should be set"
                | Error _ ->
                    () // Also acceptable - validation might catch it
            }

            testAsync "should reject dangerous commands" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config "rm -rf /"

                match result with
                | Error err ->
                    Expect.stringContains err "dangerous" "Should mention dangerous patterns"
                | Ok _ ->
                    failtest "Should reject dangerous command"
            }

            testAsync "should reject empty commands" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config ""

                match result with
                | Error err ->
                    Expect.stringContains err "empty" "Should mention empty command"
                | Ok _ ->
                    failtest "Should reject empty command"
            }

            testAsync "should execute pwd successfully" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config "pwd"

                match result with
                | Ok (execResult, _) ->
                    Expect.equal execResult.ExitCode 0 "Exit code should be 0"
                    Expect.isNotEmpty execResult.Output "Output should not be empty"
                | Error err ->
                    failtest $"Should succeed: {err}"
            }

            testAsync "should execute ls successfully" {
                let config = ExecutionConfig.defaultConfig
                let! result = executeCommand config "ls"

                match result with
                | Ok (execResult, _) ->
                    Expect.equal execResult.ExitCode 0 "Exit code should be 0"
                | Error err ->
                    failtest $"Should succeed: {err}"
            }
        ]

        testList "executeCommandFromDomain" [
            testAsync "should execute command from Command domain type" {
                let config = ExecutionConfig.defaultConfig
                let cmd = Command.create Guid.Empty "echo 'test'" "Test command" false
                let! result = executeCommandFromDomain config cmd

                match result with
                | Ok execResult ->
                    Expect.equal execResult.CommandId cmd.Id "CommandId should match"
                    Expect.stringContains execResult.Output "test" "Output should contain test"
                | Error err ->
                    failtest $"Should succeed: {err}"
            }

            testAsync "should handle command failure in domain type" {
                let config = ExecutionConfig.defaultConfig
                let cmd = Command.create Guid.Empty "ls /nonexistent" "Test" false
                let! result = executeCommandFromDomain config cmd

                match result with
                | Ok execResult ->
                    Expect.notEqual execResult.ExitCode 0 "Exit code should be non-zero"
                | Error _ ->
                    () // Also acceptable
            }
        ]
    ]
