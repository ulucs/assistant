module Agent.Core.Tests.LLMTests

open System
open Expecto
open Agent.Core.Domain
open Agent.Core.LLM

[<Tests>]
let llmTests =
    testList "LLM Tests" [
        testList "buildSystemPrompt" [
            test "should return non-empty prompt" {
                let prompt = buildSystemPrompt ()

                Expect.isNotEmpty prompt "System prompt should not be empty"
                Expect.stringContains prompt "JSON" "Should mention JSON format"
                Expect.stringContains prompt "commands" "Should mention commands"
            }
        ]

        testList "parseCommands" [
            test "should parse valid single command" {
                let response = {
                    Id = "test"
                    Model = "gpt-4"
                    Choices = [
                        {
                            Index = 0
                            Message = {
                                Role = "assistant"
                                Content = """{"commands": [{"command": "ls -la", "reasoning": "List all files"}]}"""
                            }
                            FinishReason = Some "stop"
                        }
                    ]
                    Usage = None
                }

                let result = parseCommands response

                match result with
                | Ok commands ->
                    Expect.equal commands.Length 1 "Should have 1 command"
                    Expect.equal commands.[0].Command "ls -la" "Command should match"
                    Expect.equal commands.[0].Reasoning "List all files" "Reasoning should match"
                | Error err ->
                    failtest $"Should succeed but got error: {err}"
            }

            test "should parse multiple commands" {
                let response = {
                    Id = "test"
                    Model = "gpt-4"
                    Choices = [
                        {
                            Index = 0
                            Message = {
                                Role = "assistant"
                                Content = """{"commands": [
                                    {"command": "ls", "reasoning": "List files"},
                                    {"command": "pwd", "reasoning": "Print directory"}
                                ]}"""
                            }
                            FinishReason = Some "stop"
                        }
                    ]
                    Usage = None
                }

                let result = parseCommands response

                match result with
                | Ok commands ->
                    Expect.equal commands.Length 2 "Should have 2 commands"
                    Expect.equal commands.[0].Command "ls" "First command should match"
                    Expect.equal commands.[1].Command "pwd" "Second command should match"
                | Error err ->
                    failtest $"Should succeed but got error: {err}"
            }

            test "should parse empty commands array" {
                let response = {
                    Id = "test"
                    Model = "gpt-4"
                    Choices = [
                        {
                            Index = 0
                            Message = {
                                Role = "assistant"
                                Content = """{"commands": [], "message": "No action needed"}"""
                            }
                            FinishReason = Some "stop"
                        }
                    ]
                    Usage = None
                }

                let result = parseCommands response

                match result with
                | Ok commands ->
                    Expect.isEmpty commands "Commands should be empty"
                | Error err ->
                    failtest $"Should succeed but got error: {err}"
            }

            test "should fail on invalid JSON" {
                let response = {
                    Id = "test"
                    Model = "gpt-4"
                    Choices = [
                        {
                            Index = 0
                            Message = {
                                Role = "assistant"
                                Content = "not valid json"
                            }
                            FinishReason = Some "stop"
                        }
                    ]
                    Usage = None
                }

                let result = parseCommands response

                match result with
                | Ok _ ->
                    failtest "Should fail on invalid JSON"
                | Error err ->
                    Expect.stringContains err "parse" "Error should mention parsing"
            }

            test "should fail on missing commands field" {
                let response = {
                    Id = "test"
                    Model = "gpt-4"
                    Choices = [
                        {
                            Index = 0
                            Message = {
                                Role = "assistant"
                                Content = """{"message": "test"}"""
                            }
                            FinishReason = Some "stop"
                        }
                    ]
                    Usage = None
                }

                let result = parseCommands response

                match result with
                | Ok _ ->
                    failtest "Should fail on missing commands field"
                | Error err ->
                    Expect.stringContains err "commands" "Error should mention missing commands"
            }

            test "should fail on empty choices" {
                let response = {
                    Id = "test"
                    Model = "gpt-4"
                    Choices = []
                    Usage = None
                }

                let result = parseCommands response

                match result with
                | Ok _ ->
                    failtest "Should fail on empty choices"
                | Error err ->
                    Expect.stringContains err "choices" "Error should mention choices"
            }
        ]

        testList "LLMConfig" [
            test "defaultConfig should have sensible defaults" {
                let config = LLMConfig.defaultConfig

                Expect.equal config.BaseUrl "http://localhost:4000" "Default base URL"
                Expect.equal config.DefaultModel "gpt-4" "Default model"
                Expect.isGreaterThan config.Temperature 0.0 "Temperature should be positive"
                Expect.isGreaterThan config.MaxTokens 0 "MaxTokens should be positive"
            }
        ]
    ]
