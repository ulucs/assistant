module Agent.UI.Types

open System

// ===== DOMAIN TYPES (mirrored from backend) =====

type InputSource =
    | WebUI
    | Email
    | API

type CommandStatus =
    | Pending
    | Approved
    | Rejected
    | Executing
    | Completed
    | Failed

type Input = {
    Id: Guid
    Source: InputSource
    Content: string
    Timestamp: DateTime
}

type Command = {
    Id: Guid
    InputId: Guid
    Command: string
    Reasoning: string
    Status: CommandStatus
    CreatedAt: DateTime
    ReviewedAt: DateTime option
    ExecutedAt: DateTime option
    IsWhitelisted: bool
}

type ExecutionResult = {
    Id: Guid
    CommandId: Guid
    Output: string
    ExitCode: int
    ExecutedAt: DateTime
    Error: string option
}

// ===== APPLICATION STATE =====

type View =
    | InputView
    | CommandQueueView
    | ResultsView

type Model = {
    CurrentView: View
    InputText: string
    Commands: Command list
    Results: ExecutionResult list
    IsLoading: bool
    ErrorMessage: string option
}

type Msg =
    | ChangeView of View
    | UpdateInputText of string
    | SubmitInput
    | InputSubmitted of Result<string, string>
    | LoadCommands
    | CommandsLoaded of Result<Command list, string>
    | ApproveCommand of Guid
    | RejectCommand of Guid
    | ExecuteCommand of Guid
    | CommandApproved of Result<Guid, string>
    | CommandRejected of Result<Guid, string>
    | CommandExecuted of Result<Guid, string>
    | LoadResults
    | ResultsLoaded of Result<ExecutionResult list, string>
    | ClearError
