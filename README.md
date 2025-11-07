# F# Assistant Agent

A human-in-the-loop AI assistant agent built with F#, featuring:
- Multi-source input (Web UI, Email)
- LLM-powered command generation via LiteLLM
- Command approval workflow
- Whitelisted read-only commands
- SQLite storage
- Giraffe web API
- Elmish frontend

## Architecture

```
┌─────────────┐     ┌─────────────┐
│   Email     │     │   Web UI    │
│   (Manual)  │     │   (Elmish)  │
└──────┬──────┘     └──────┬──────┘
       │                   │
       └───────┬───────────┘
               ↓
      ┌────────────────┐
      │  Agent Core    │
      │  (F# + LLM)    │
      └────────┬───────┘
               ↓
      ┌────────────────┐
      │ Command Queue  │
      │   (SQLite)     │
      └────────┬───────┘
               ↓
      ┌────────────────┐
      │   Approval     │
      │   (Web UI)     │
      └────────┬───────┘
               ↓
      ┌────────────────┐
      │   Executor     │
      │   (Bash)       │
      └────────────────┘
```

## Tech Stack

- **Language**: F# (functional)
- **LLM**: LiteLLM (unified API for multiple LLM providers)
- **Database**: SQLite + Donald (functional SQL wrapper)
- **Backend**: Giraffe (F# web framework on ASP.NET Core)
- **Frontend**: Elmish + Feliz (functional reactive UI)
- **Build**: Nix Flakes

## Prerequisites

- Nix with flakes enabled
- LLM API key (OpenAI, Anthropic, etc.)

## Quick Start

### 1. Enter Nix Development Shell

```bash
nix develop
```

This will set up F#, Python, Node.js, and all dependencies.

### 2. Set Environment Variables

```bash
# For OpenAI
export OPENAI_API_KEY="your-key-here"

# For Anthropic Claude
export ANTHROPIC_API_KEY="your-key-here"
```

### 3. Start LiteLLM Proxy

In one terminal:

```bash
./litellm/start.sh
```

This starts LiteLLM on `http://localhost:4000`.

### 4. Build Frontend

In another terminal:

```bash
npm install
npm run build
```

For development with hot reload:

```bash
npm start
```

### 5. Start Backend

```bash
dotnet run --project src/Agent.Web
```

The server will start on `http://localhost:5000`.

### 6. Open Web UI

Navigate to `http://localhost:5000` in your browser.

## Usage

### Submit a Task

1. Go to the **Input** tab
2. Describe what you want the agent to do:
   ```
   List all Python files in the current directory and show their sizes
   ```
3. Click **Submit**

### Review & Approve Commands

1. Go to the **Command Queue** tab
2. Review the proposed command and reasoning
3. Click **✓ Approve** or **✗ Reject**

### Execute Commands

1. After approving, click **▶ Execute**
2. View results in the **Results** tab

### Whitelisted Commands

These commands are automatically marked as safe (read-only):

- **File operations**: `ls`, `cat`, `head`, `tail`, `grep`, `find`, `tree`, `pwd`
- **Git**: `git status`, `git log`, `git diff`, `git show`, `git branch`
- **System info**: `df`, `du`, `ps`, `whoami`, `uname`, `date`
- **Help**: `tldr`, `man`, `help`, `which`

Whitelisted commands still require approval but are marked for quick review.

## Project Structure

```
assistant/
├── flake.nix                    # Nix flake configuration
├── Assistant.sln                # F# solution
├── run-tests.sh                 # Test runner script
├── src/
│   ├── Agent.Core/              # Domain models, LLM client, agent logic
│   ├── Agent.Core.Tests/        # Core unit tests
│   ├── Agent.Storage/           # SQLite storage layer
│   ├── Agent.Storage.Tests/     # Storage integration tests
│   ├── Agent.Executor/          # Command execution & whitelist
│   ├── Agent.Executor.Tests/    # Executor unit tests
│   ├── Agent.Web/               # Giraffe web API + entry point
│   ├── Agent.Web.Tests/         # API integration tests
│   └── Agent.UI/                # Elmish frontend
├── litellm/
│   ├── config.yaml              # LiteLLM configuration
│   └── start.sh                 # Startup script
├── data/                        # SQLite database
├── package.json                 # NPM configuration
└── webpack.config.js            # Webpack for Fable
```

## API Endpoints

- `POST /api/inputs` - Submit new input
- `GET /api/commands` - List all commands
- `GET /api/commands/pending` - List pending commands
- `POST /api/commands/{id}/approve` - Approve command
- `POST /api/commands/{id}/reject` - Reject command
- `POST /api/commands/{id}/execute` - Execute approved command
- `GET /api/results` - List execution results
- `GET /api/results/{commandId}` - Get result for specific command

## Configuration

### LiteLLM Models

Edit `litellm/config.yaml` to configure available models:

```yaml
model_list:
  - model_name: gpt-4
    litellm_params:
      model: gpt-4
      api_key: os.environ/OPENAI_API_KEY

  - model_name: claude-3-sonnet
    litellm_params:
      model: claude-3-sonnet-20240229
      api_key: os.environ/ANTHROPIC_API_KEY
```

### Database

By default, SQLite database is created at `data/assistant.db`.

To change the location, modify `Agent.Storage.Database.DbConfig.defaultConfig`.

### Execution Timeout

Commands timeout after 30 seconds by default.

To change, modify `Agent.Executor.CommandRunner.ExecutionConfig.defaultConfig`.

## Development

### Backend

```bash
# Run with hot reload
dotnet watch run --project src/Agent.Web
```

### Frontend

```bash
# Development with hot reload
npm start

# Production build
npm run build
```

### Database Reset

```bash
rm data/assistant.db
# Restart the backend to recreate
```

## Testing

The project includes comprehensive test coverage with Expecto test framework.

### Test Projects

- **Agent.Core.Tests** - Domain models, LLM parsing, agent logic
- **Agent.Storage.Tests** - Database operations and queries
- **Agent.Executor.Tests** - Whitelist validation and command execution
- **Agent.Web.Tests** - API integration tests

### Running Tests

Run all tests:

```bash
./run-tests.sh
```

Or run individual test projects:

```bash
# Core tests
dotnet test src/Agent.Core.Tests

# Storage tests
dotnet test src/Agent.Storage.Tests

# Executor tests
dotnet test src/Agent.Executor.Tests

# Web API tests
dotnet test src/Agent.Web.Tests
```

Run tests with verbose output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage

**Domain Tests:**
- Input/Command/ExecutionResult creation
- Status transitions
- Helper functions

**LLM Tests:**
- JSON parsing
- Command extraction
- Error handling

**Whitelist Tests:**
- Read-only command detection
- Dangerous pattern detection (rm -rf, sudo, etc.)
- Command validation

**Storage Tests:**
- Database migrations
- CRUD operations
- Query filtering
- Status parsing

**Executor Tests:**
- Command execution
- Output capture
- Timeout handling
- Error propagation

**API Tests:**
- Request/response handling
- Command approval workflow
- Status transitions
- Database integration

### Writing New Tests

Example test with Expecto:

```fsharp
open Expecto

[<Tests>]
let myTests =
    testList "My Feature" [
        test "should do something" {
            let result = doSomething()
            Expect.equal result expected "Should match expected"
        }

        testAsync "should handle async" {
            let! result = doSomethingAsync()
            Expect.isTrue result "Should be true"
        }
    ]
```

## Security

⚠️ **Important Security Notes**:

1. **No authentication** - This is a development/personal tool. Do not expose to the internet.
2. **Command validation** - Review all commands before approval, even whitelisted ones.
3. **Dangerous patterns** - The system blocks some dangerous patterns (rm -rf, sudo, etc.) but is not foolproof.
4. **LLM API keys** - Keep your API keys secure and never commit them.

## Future Enhancements

- [ ] Email integration (IMAP polling)
- [ ] Authentication system
- [ ] Command history and rollback
- [ ] Multi-step workflows
- [ ] Scheduled tasks
- [ ] WebSocket for real-time updates
- [ ] Command templates
- [ ] Better error handling and retries

## License

See LICENSE file.

## Contributing

This is a personal project, but suggestions and improvements are welcome!
