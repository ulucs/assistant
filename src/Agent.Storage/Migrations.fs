module Agent.Storage.Migrations

open Donald
open Agent.Storage.Database

/// SQL for creating the inputs table
let createInputsTable = """
CREATE TABLE IF NOT EXISTS inputs (
    id TEXT PRIMARY KEY,
    source TEXT NOT NULL,
    content TEXT NOT NULL,
    timestamp TEXT NOT NULL
);
"""

/// SQL for creating the commands table
let createCommandsTable = """
CREATE TABLE IF NOT EXISTS commands (
    id TEXT PRIMARY KEY,
    input_id TEXT NOT NULL,
    command TEXT NOT NULL,
    reasoning TEXT NOT NULL,
    status TEXT NOT NULL,
    created_at TEXT NOT NULL,
    reviewed_at TEXT,
    executed_at TEXT,
    is_whitelisted INTEGER NOT NULL,
    FOREIGN KEY (input_id) REFERENCES inputs(id)
);

CREATE INDEX IF NOT EXISTS idx_commands_status ON commands(status);
CREATE INDEX IF NOT EXISTS idx_commands_input_id ON commands(input_id);
"""

/// SQL for creating the execution_results table
let createExecutionResultsTable = """
CREATE TABLE IF NOT EXISTS execution_results (
    id TEXT PRIMARY KEY,
    command_id TEXT NOT NULL,
    output TEXT NOT NULL,
    exit_code INTEGER NOT NULL,
    executed_at TEXT NOT NULL,
    error TEXT,
    FOREIGN KEY (command_id) REFERENCES commands(id)
);

CREATE INDEX IF NOT EXISTS idx_execution_results_command_id ON execution_results(command_id);
"""

/// Initialize the database schema
let initialize (config: DbConfig) : unit =
    withConnection config (fun conn ->
        // Create tables
        dbCommand conn {
            cmdText createInputsTable
        }
        |> DbConn.exec

        dbCommand conn {
            cmdText createCommandsTable
        }
        |> DbConn.exec

        dbCommand conn {
            cmdText createExecutionResultsTable
        }
        |> DbConn.exec

        printfn "Database initialized successfully"
    )

/// Reset the database (drop all tables)
let reset (config: DbConfig) : unit =
    withConnection config (fun conn ->
        dbCommand conn {
            cmdText "DROP TABLE IF EXISTS execution_results"
        }
        |> DbConn.exec

        dbCommand conn {
            cmdText "DROP TABLE IF EXISTS commands"
        }
        |> DbConn.exec

        dbCommand conn {
            cmdText "DROP TABLE IF EXISTS inputs"
        }
        |> DbConn.exec

        printfn "Database reset successfully"
    )

    // Reinitialize
    initialize config
