module Agent.Core.LLM

open System
open System.Text.Json
open System.Text.Json.Serialization
open FsHttp
open Agent.Core.Domain

/// Configuration for LiteLLM connection
type LLMConfig = {
    BaseUrl: string
    DefaultModel: string
    Temperature: float
    MaxTokens: int
}

module LLMConfig =
    let defaultConfig = {
        BaseUrl = "http://localhost:4000"
        DefaultModel = "gpt-4"
        Temperature = 0.7
        MaxTokens = 2000
    }

/// JSON serialization options
let jsonOptions =
    let options = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
    options

/// Call LiteLLM API
let callLLM (config: LLMConfig) (request: LLMRequest) : Async<Result<LLMResponse, string>> =
    async {
        try
            let! response =
                http {
                    POST (sprintf "%s/v1/chat/completions" config.BaseUrl)
                    body
                    jsonSerialize request
                }
                |> Request.sendAsync

            if response.statusCode = System.Net.HttpStatusCode.OK then
                let! body = Response.toTextAsync response
                let llmResponse = JsonSerializer.Deserialize<LLMResponse>(body, jsonOptions)
                return Ok llmResponse
            else
                let! errorBody = Response.toTextAsync response
                return Error (sprintf "LLM API error (status %d): %s" (int response.statusCode) errorBody)
        with
        | ex ->
            return Error (sprintf "LLM API exception: %s" ex.Message)
    }

/// Build a system prompt for the agent
let buildSystemPrompt () =
    """You are an AI assistant that helps users by executing commands on their behalf.

When given a task, analyze what needs to be done and propose specific shell commands to accomplish it.

IMPORTANT: Format your response as JSON with the following structure:
{
  "commands": [
    {
      "command": "the exact shell command to run",
      "reasoning": "why this command is needed"
    }
  ]
}

If no commands are needed, respond with:
{
  "commands": [],
  "message": "explanation of why no commands are needed"
}

Guidelines:
- Propose specific, actionable commands
- Break complex tasks into multiple commands
- Be precise with file paths and arguments
- Consider safety and reversibility
- If you need more information, say so in the reasoning

Example response for "list all Python files":
{
  "commands": [
    {
      "command": "find . -name '*.py' -type f",
      "reasoning": "Search recursively for all Python files in the current directory"
    }
  ]
}"""

/// Parse LLM response to extract commands
let parseCommands (response: LLMResponse) : Result<ParsedCommand list, string> =
    try
        match response.Choices with
        | [] -> Error "No choices in LLM response"
        | choice :: _ ->
            let content = choice.Message.Content

            // Try to parse as JSON
            try
                let jsonDoc = JsonDocument.Parse(content)
                let root = jsonDoc.RootElement

                if root.TryGetProperty("commands") |> fst then
                    let commandsArray = root.GetProperty("commands")
                    let commands =
                        commandsArray.EnumerateArray()
                        |> Seq.map (fun elem ->
                            {
                                Command = elem.GetProperty("command").GetString()
                                Reasoning = elem.GetProperty("reasoning").GetString()
                            })
                        |> Seq.toList

                    Ok commands
                else
                    Error "Response missing 'commands' field"
            with
            | :? JsonException as ex ->
                Error (sprintf "Failed to parse JSON: %s\nContent: %s" ex.Message content)
    with
    | ex ->
        Error (sprintf "Error parsing LLM response: %s" ex.Message)

/// Process an input through the LLM
let processInput (config: LLMConfig) (input: Input) : Async<Result<ParsedCommand list, string>> =
    async {
        let request = {
            Model = config.DefaultModel
            Messages = [
                { Role = "system"; Content = buildSystemPrompt () }
                { Role = "user"; Content = input.Content }
            ]
            Temperature = Some config.Temperature
            MaxTokens = Some config.MaxTokens
        }

        let! result = callLLM config request

        return
            result
            |> Result.bind parseCommands
    }
