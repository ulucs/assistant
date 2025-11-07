module Agent.Storage.Database

open System
open Microsoft.Data.Sqlite
open Donald

/// Database configuration
type DbConfig = {
    ConnectionString: string
}

module DbConfig =
    let fromFile (filePath: string) = {
        ConnectionString = sprintf "Data Source=%s" filePath
    }

    let defaultConfig = fromFile "data/assistant.db"

/// Create a database connection
let createConnection (config: DbConfig) : IDbConnection =
    new SqliteConnection(config.ConnectionString) :> IDbConnection

/// Execute a command with a connection
let withConnection (config: DbConfig) (f: IDbConnection -> 'a) : 'a =
    use conn = createConnection config
    conn.Open()
    f conn

/// Execute a command asynchronously
let withConnectionAsync (config: DbConfig) (f: IDbConnection -> Async<'a>) : Async<'a> =
    async {
        use conn = createConnection config
        conn.Open()
        return! f conn
    }
