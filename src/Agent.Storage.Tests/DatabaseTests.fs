module Agent.Storage.Tests.DatabaseTests

open System
open System.IO
open Expecto
open Agent.Storage.Database
open Agent.Storage.Migrations

let createTestDb () =
    let testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db")
    DbConfig.fromFile testDbPath

let cleanupTestDb (config: DbConfig) =
    let connectionString = config.ConnectionString
    let dbPath = connectionString.Replace("Data Source=", "")
    if File.Exists(dbPath) then
        File.Delete(dbPath)

[<Tests>]
let databaseTests =
    testList "Database Tests" [
        testList "Database Connection" [
            test "should create connection" {
                let config = createTestDb ()

                try
                    use conn = createConnection config
                    conn.Open()
                    Expect.equal conn.State System.Data.ConnectionState.Open "Connection should be open"
                finally
                    cleanupTestDb config
            }

            test "should execute commands with connection" {
                let config = createTestDb ()

                try
                    let result = withConnection config (fun conn ->
                        conn.State
                    )

                    Expect.equal result System.Data.ConnectionState.Open "Connection should be open"
                finally
                    cleanupTestDb config
            }
        ]

        testList "Migrations" [
            test "initialize should create all tables" {
                let config = createTestDb ()

                try
                    initialize config

                    let tableExists tableName =
                        withConnection config (fun conn ->
                            use cmd = conn.CreateCommand()
                            cmd.CommandText <- $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'"
                            use reader = cmd.ExecuteReader()
                            reader.Read()
                        )

                    Expect.isTrue (tableExists "inputs") "inputs table should exist"
                    Expect.isTrue (tableExists "commands") "commands table should exist"
                    Expect.isTrue (tableExists "execution_results") "execution_results table should exist"
                finally
                    cleanupTestDb config
            }

            test "reset should drop and recreate tables" {
                let config = createTestDb ()

                try
                    initialize config
                    reset config

                    let tableExists tableName =
                        withConnection config (fun conn ->
                            use cmd = conn.CreateCommand()
                            cmd.CommandText <- $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'"
                            use reader = cmd.ExecuteReader()
                            reader.Read()
                        )

                    Expect.isTrue (tableExists "inputs") "inputs table should exist after reset"
                    Expect.isTrue (tableExists "commands") "commands table should exist after reset"
                    Expect.isTrue (tableExists "execution_results") "execution_results table should exist after reset"
                finally
                    cleanupTestDb config
            }
        ]
    ]
