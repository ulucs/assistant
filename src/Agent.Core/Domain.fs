module Agent.Core.Domain

open System

/// Source from which an input was received
type InputSource =
    | WebUI
    | Email
    | API

/// Input received by the agent
type Input = {
    Id: Guid
    Source: InputSource
    Content: string
    Timestamp: DateTime
}

/// Status of a command in the approval workflow
type CommandStatus =
    | Pending
    | Approved
    | Rejected
    | Executing
    | Completed
    | Failed

/// A command that the agent wants to execute
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

/// Result of executing a command
type ExecutionResult = {
    Id: Guid
    CommandId: Guid
    Output: string
    ExitCode: int
    ExecutedAt: DateTime
    Error: string option
}

/// Request from the agent to LiteLLM
type LLMRequest = {
    Model: string
    Messages: LLMMessage list
    Temperature: float option
    MaxTokens: int option
}

and LLMMessage = {
    Role: string
    Content: string
}

/// Response from LiteLLM
type LLMResponse = {
    Id: string
    Model: string
    Choices: LLMChoice list
    Usage: TokenUsage option
}

and LLMChoice = {
    Index: int
    Message: LLMMessage
    FinishReason: string option
}

and TokenUsage = {
    PromptTokens: int
    CompletionTokens: int
    TotalTokens: int
}

/// Parsed command from LLM response
type ParsedCommand = {
    Command: string
    Reasoning: string
}

/// Agent processing result
type AgentResult =
    | Success of Command list
    | NoActionNeeded of string
    | ParseError of string
    | LLMError of string

/// Helper functions for domain types
module Input =
    let create source content =
        {
            Id = Guid.NewGuid()
            Source = source
            Content = content
            Timestamp = DateTime.UtcNow
        }

module Command =
    let create inputId command reasoning isWhitelisted =
        {
            Id = Guid.NewGuid()
            InputId = inputId
            Command = command
            Reasoning = reasoning
            Status = Pending
            CreatedAt = DateTime.UtcNow
            ReviewedAt = None
            ExecutedAt = None
            IsWhitelisted = isWhitelisted
        }

    let approve cmd =
        { cmd with
            Status = Approved
            ReviewedAt = Some DateTime.UtcNow
        }

    let reject cmd =
        { cmd with
            Status = Rejected
            ReviewedAt = Some DateTime.UtcNow
        }

    let startExecution cmd =
        { cmd with
            Status = Executing
            ExecutedAt = Some DateTime.UtcNow
        }

    let markCompleted cmd =
        { cmd with Status = Completed }

    let markFailed cmd =
        { cmd with Status = Failed }

module ExecutionResult =
    let create commandId output exitCode error =
        {
            Id = Guid.NewGuid()
            CommandId = commandId
            Output = output
            ExitCode = exitCode
            ExecutedAt = DateTime.UtcNow
            Error = error
        }
