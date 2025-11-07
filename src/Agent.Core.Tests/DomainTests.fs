module Agent.Core.Tests.DomainTests

open System
open Expecto
open Agent.Core.Domain

[<Tests>]
let domainTests =
    testList "Domain Tests" [
        testList "Input" [
            test "create should generate valid input" {
                let input = Input.create WebUI "Test content"

                Expect.notEqual input.Id Guid.Empty "Input ID should not be empty"
                Expect.equal input.Source WebUI "Input source should be WebUI"
                Expect.equal input.Content "Test content" "Content should match"
                Expect.isTrue (input.Timestamp <= DateTime.UtcNow) "Timestamp should be in the past or now"
            }

            test "create should support different sources" {
                let webInput = Input.create WebUI "web"
                let emailInput = Input.create Email "email"
                let apiInput = Input.create API "api"

                Expect.equal webInput.Source WebUI "WebUI source"
                Expect.equal emailInput.Source Email "Email source"
                Expect.equal apiInput.Source API "API source"
            }
        ]

        testList "Command" [
            test "create should generate valid command" {
                let inputId = Guid.NewGuid()
                let cmd = Command.create inputId "ls -la" "List files" false

                Expect.notEqual cmd.Id Guid.Empty "Command ID should not be empty"
                Expect.equal cmd.InputId inputId "InputId should match"
                Expect.equal cmd.Command "ls -la" "Command should match"
                Expect.equal cmd.Reasoning "List files" "Reasoning should match"
                Expect.equal cmd.Status Pending "Status should be Pending"
                Expect.equal cmd.IsWhitelisted false "Should not be whitelisted"
                Expect.isNone cmd.ReviewedAt "ReviewedAt should be None"
                Expect.isNone cmd.ExecutedAt "ExecutedAt should be None"
            }

            test "approve should update status and timestamp" {
                let cmd = Command.create Guid.Empty "test" "test" false
                let approved = Command.approve cmd

                Expect.equal approved.Status Approved "Status should be Approved"
                Expect.isSome approved.ReviewedAt "ReviewedAt should be set"
            }

            test "reject should update status and timestamp" {
                let cmd = Command.create Guid.Empty "test" "test" false
                let rejected = Command.reject cmd

                Expect.equal rejected.Status Rejected "Status should be Rejected"
                Expect.isSome rejected.ReviewedAt "ReviewedAt should be set"
            }

            test "startExecution should update status and timestamp" {
                let cmd = Command.create Guid.Empty "test" "test" false
                let executing = Command.startExecution cmd

                Expect.equal executing.Status Executing "Status should be Executing"
                Expect.isSome executing.ExecutedAt "ExecutedAt should be set"
            }

            test "markCompleted should update status" {
                let cmd = Command.create Guid.Empty "test" "test" false
                let completed = Command.markCompleted cmd

                Expect.equal completed.Status Completed "Status should be Completed"
            }

            test "markFailed should update status" {
                let cmd = Command.create Guid.Empty "test" "test" false
                let failed = Command.markFailed cmd

                Expect.equal failed.Status Failed "Status should be Failed"
            }
        ]

        testList "ExecutionResult" [
            test "create should generate valid result" {
                let cmdId = Guid.NewGuid()
                let result = ExecutionResult.create cmdId "output" 0 None

                Expect.notEqual result.Id Guid.Empty "Result ID should not be empty"
                Expect.equal result.CommandId cmdId "CommandId should match"
                Expect.equal result.Output "output" "Output should match"
                Expect.equal result.ExitCode 0 "ExitCode should be 0"
                Expect.isNone result.Error "Error should be None"
            }

            test "create should handle error messages" {
                let result = ExecutionResult.create Guid.Empty "output" 1 (Some "error message")

                Expect.equal result.ExitCode 1 "ExitCode should be 1"
                Expect.equal result.Error (Some "error message") "Error should be set"
            }
        ]
    ]
