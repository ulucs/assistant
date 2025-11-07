module Agent.Core.Tests.AgentTests

open System
open Expecto
open Agent.Core.Domain
open Agent.Core.Agent

[<Tests>]
let agentTests =
    testList "Agent Tests" [
        testList "isWhitelisted" [
            test "should whitelist ls command" {
                Expect.isTrue (isWhitelisted "ls") "ls should be whitelisted"
            }

            test "should whitelist ls with arguments" {
                Expect.isTrue (isWhitelisted "ls -la") "ls -la should be whitelisted"
                Expect.isTrue (isWhitelisted "ls /home") "ls /home should be whitelisted"
            }

            test "should whitelist cat command" {
                Expect.isTrue (isWhitelisted "cat file.txt") "cat should be whitelisted"
            }

            test "should whitelist git status" {
                Expect.isTrue (isWhitelisted "git status") "git status should be whitelisted"
            }

            test "should whitelist git log" {
                Expect.isTrue (isWhitelisted "git log") "git log should be whitelisted"
            }

            test "should whitelist git diff" {
                Expect.isTrue (isWhitelisted "git diff") "git diff should be whitelisted"
            }

            test "should whitelist tldr" {
                Expect.isTrue (isWhitelisted "tldr ls") "tldr should be whitelisted"
            }

            test "should whitelist grep" {
                Expect.isTrue (isWhitelisted "grep pattern file.txt") "grep should be whitelisted"
            }

            test "should whitelist find" {
                Expect.isTrue (isWhitelisted "find . -name '*.py'") "find should be whitelisted"
            }

            test "should whitelist pwd" {
                Expect.isTrue (isWhitelisted "pwd") "pwd should be whitelisted"
            }

            test "should whitelist df" {
                Expect.isTrue (isWhitelisted "df -h") "df should be whitelisted"
            }

            test "should whitelist ps" {
                Expect.isTrue (isWhitelisted "ps aux") "ps should be whitelisted"
            }

            test "should NOT whitelist rm" {
                Expect.isFalse (isWhitelisted "rm file.txt") "rm should not be whitelisted"
            }

            test "should NOT whitelist rm -rf" {
                Expect.isFalse (isWhitelisted "rm -rf /") "rm -rf should not be whitelisted"
            }

            test "should NOT whitelist git commit" {
                Expect.isFalse (isWhitelisted "git commit") "git commit should not be whitelisted"
            }

            test "should NOT whitelist git push" {
                Expect.isFalse (isWhitelisted "git push") "git push should not be whitelisted"
            }

            test "should NOT whitelist mkdir" {
                Expect.isFalse (isWhitelisted "mkdir test") "mkdir should not be whitelisted"
            }

            test "should NOT whitelist sudo" {
                Expect.isFalse (isWhitelisted "sudo ls") "sudo should not be whitelisted"
            }

            test "should NOT whitelist chmod" {
                Expect.isFalse (isWhitelisted "chmod 777 file") "chmod should not be whitelisted"
            }

            test "should be case insensitive" {
                Expect.isTrue (isWhitelisted "LS") "LS should be whitelisted"
                Expect.isTrue (isWhitelisted "Cat file.txt") "Cat should be whitelisted"
                Expect.isTrue (isWhitelisted "GIT STATUS") "GIT STATUS should be whitelisted"
            }

            test "should handle leading/trailing whitespace" {
                Expect.isTrue (isWhitelisted "  ls  ") "ls with whitespace should be whitelisted"
            }

            test "should handle empty string" {
                Expect.isFalse (isWhitelisted "") "empty string should not be whitelisted"
            }
        ]
    ]
