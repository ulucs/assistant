module Agent.UI.App

open Elmish
open Elmish.React
open Fable.Core
open Fable.Fetch
open Agent.UI.Types
open Agent.UI.View

// ===== API CLIENT =====

let apiBaseUrl = "http://localhost:5000/api"

[<Emit("JSON.parse($0)")>]
let jsonParse (json: string) : 'T = jsNative

let fetchCommands () =
    promise {
        let! response = fetch (sprintf "%s/commands" apiBaseUrl) []
        let! text = response.text()
        return jsonParse<Command list> text
    }

let fetchResults () =
    promise {
        let! response = fetch (sprintf "%s/results" apiBaseUrl) []
        let! text = response.text()
        return jsonParse<ExecutionResult list> text
    }

let submitInput (content: string) =
    promise {
        let! response =
            fetch
                (sprintf "%s/inputs" apiBaseUrl)
                [
                    Method HttpMethod.POST
                    Headers [
                        HttpRequestHeaders.ContentType "application/json"
                    ]
                    Body (Fetch.Types.BodyInit.Text (sprintf """{"content": "%s"}""" (content.Replace("\"", "\\\""))))
                ]
        let! text = response.text()
        return text
    }

let approveCommand (commandId: System.Guid) =
    promise {
        let! response =
            fetch
                (sprintf "%s/commands/%s/approve" apiBaseUrl (commandId.ToString()))
                [ Method HttpMethod.POST ]
        let! text = response.text()
        return text
    }

let rejectCommand (commandId: System.Guid) =
    promise {
        let! response =
            fetch
                (sprintf "%s/commands/%s/reject" apiBaseUrl (commandId.ToString()))
                [ Method HttpMethod.POST ]
        let! text = response.text()
        return text
    }

let executeCommand (commandId: System.Guid) =
    promise {
        let! response =
            fetch
                (sprintf "%s/commands/%s/execute" apiBaseUrl (commandId.ToString()))
                [ Method HttpMethod.POST ]
        let! text = response.text()
        return text
    }

// ===== ELMISH MVU =====

let init () : Model * Cmd<Msg> =
    let model = {
        CurrentView = InputView
        InputText = ""
        Commands = []
        Results = []
        IsLoading = false
        ErrorMessage = None
    }
    model, Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | ChangeView view ->
        let cmd =
            match view with
            | CommandQueueView -> Cmd.ofMsg LoadCommands
            | ResultsView -> Cmd.ofMsg LoadResults
            | _ -> Cmd.none
        { model with CurrentView = view }, cmd

    | UpdateInputText text ->
        { model with InputText = text }, Cmd.none

    | SubmitInput ->
        let cmd =
            Cmd.OfPromise.either
                submitInput
                model.InputText
                (fun _ -> InputSubmitted (Ok "Success"))
                (fun ex -> InputSubmitted (Error ex.Message))
        { model with IsLoading = true; ErrorMessage = None }, cmd

    | InputSubmitted result ->
        match result with
        | Ok _ ->
            { model with
                IsLoading = false
                InputText = ""
                CurrentView = CommandQueueView
            }, Cmd.ofMsg LoadCommands
        | Error err ->
            { model with
                IsLoading = false
                ErrorMessage = Some (sprintf "Failed to submit input: %s" err)
            }, Cmd.none

    | LoadCommands ->
        let cmd =
            Cmd.OfPromise.either
                fetchCommands
                ()
                (fun commands -> CommandsLoaded (Ok commands))
                (fun ex -> CommandsLoaded (Error ex.Message))
        { model with IsLoading = true }, cmd

    | CommandsLoaded result ->
        match result with
        | Ok commands ->
            { model with Commands = commands; IsLoading = false }, Cmd.none
        | Error err ->
            { model with
                IsLoading = false
                ErrorMessage = Some (sprintf "Failed to load commands: %s" err)
            }, Cmd.none

    | ApproveCommand commandId ->
        let cmd =
            Cmd.OfPromise.either
                approveCommand
                commandId
                (fun _ -> CommandApproved (Ok commandId))
                (fun ex -> CommandApproved (Error ex.Message))
        model, cmd

    | CommandApproved result ->
        match result with
        | Ok _ ->
            model, Cmd.ofMsg LoadCommands
        | Error err ->
            { model with ErrorMessage = Some (sprintf "Failed to approve command: %s" err) }, Cmd.none

    | RejectCommand commandId ->
        let cmd =
            Cmd.OfPromise.either
                rejectCommand
                commandId
                (fun _ -> CommandRejected (Ok commandId))
                (fun ex -> CommandRejected (Error ex.Message))
        model, cmd

    | CommandRejected result ->
        match result with
        | Ok _ ->
            model, Cmd.ofMsg LoadCommands
        | Error err ->
            { model with ErrorMessage = Some (sprintf "Failed to reject command: %s" err) }, Cmd.none

    | ExecuteCommand commandId ->
        let cmd =
            Cmd.OfPromise.either
                executeCommand
                commandId
                (fun _ -> CommandExecuted (Ok commandId))
                (fun ex -> CommandExecuted (Error ex.Message))
        model, cmd

    | CommandExecuted result ->
        match result with
        | Ok _ ->
            model, Cmd.batch [ Cmd.ofMsg LoadCommands; Cmd.ofMsg LoadResults ]
        | Error err ->
            { model with ErrorMessage = Some (sprintf "Failed to execute command: %s" err) }, Cmd.none

    | LoadResults ->
        let cmd =
            Cmd.OfPromise.either
                fetchResults
                ()
                (fun results -> ResultsLoaded (Ok results))
                (fun ex -> ResultsLoaded (Error ex.Message))
        { model with IsLoading = true }, cmd

    | ResultsLoaded result ->
        match result with
        | Ok results ->
            { model with Results = results; IsLoading = false }, Cmd.none
        | Error err ->
            { model with
                IsLoading = false
                ErrorMessage = Some (sprintf "Failed to load results: %s" err)
            }, Cmd.none

    | ClearError ->
        { model with ErrorMessage = None }, Cmd.none

// ===== MAIN =====

Program.mkProgram init update view
|> Program.withReactSynchronous "app"
|> Program.run
