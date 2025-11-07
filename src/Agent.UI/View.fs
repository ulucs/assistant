module Agent.UI.View

open Feliz
open Agent.UI.Types

// ===== HELPERS =====

let statusColor (status: CommandStatus) =
    match status with
    | Pending -> "orange"
    | Approved -> "green"
    | Rejected -> "red"
    | Executing -> "blue"
    | Completed -> "darkgreen"
    | Failed -> "darkred"

let statusText (status: CommandStatus) =
    match status with
    | Pending -> "Pending"
    | Approved -> "Approved"
    | Rejected -> "Rejected"
    | Executing -> "Executing..."
    | Completed -> "✓ Completed"
    | Failed -> "✗ Failed"

// ===== NAVIGATION =====

let navbar (model: Model) (dispatch: Msg -> unit) =
    Html.nav [
        prop.style [
            style.backgroundColor "#2c3e50"
            style.padding 20
            style.marginBottom 20
        ]
        prop.children [
            Html.div [
                prop.style [
                    style.display.flex
                    style.justifyContent.spaceBetween
                    style.alignItems.center
                    style.maxWidth 1200
                    style.margin.auto
                ]
                prop.children [
                    Html.h1 [
                        prop.style [ style.color "white"; style.margin 0 ]
                        prop.text "🤖 Assistant Agent"
                    ]
                    Html.div [
                        prop.style [ style.display.flex; style.gap 10 ]
                        prop.children [
                            Html.button [
                                prop.style [
                                    style.padding (10, 20)
                                    style.backgroundColor (if model.CurrentView = InputView then "#3498db" else "#34495e")
                                    style.color "white"
                                    style.border.none
                                    style.borderRadius 5
                                    style.cursor.pointer
                                ]
                                prop.text "Input"
                                prop.onClick (fun _ -> dispatch (ChangeView InputView))
                            ]
                            Html.button [
                                prop.style [
                                    style.padding (10, 20)
                                    style.backgroundColor (if model.CurrentView = CommandQueueView then "#3498db" else "#34495e")
                                    style.color "white"
                                    style.border.none
                                    style.borderRadius 5
                                    style.cursor.pointer
                                ]
                                prop.text "Command Queue"
                                prop.onClick (fun _ -> dispatch (ChangeView CommandQueueView))
                            ]
                            Html.button [
                                prop.style [
                                    style.padding (10, 20)
                                    style.backgroundColor (if model.CurrentView = ResultsView then "#3498db" else "#34495e")
                                    style.color "white"
                                    style.border.none
                                    style.borderRadius 5
                                    style.cursor.pointer
                                ]
                                prop.text "Results"
                                prop.onClick (fun _ -> dispatch (ChangeView ResultsView))
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// ===== INPUT VIEW =====

let inputView (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [
            style.maxWidth 800
            style.margin.auto
            style.padding 20
        ]
        prop.children [
            Html.h2 [ prop.text "Submit a Task" ]
            Html.p [
                prop.style [ style.color "#7f8c8d" ]
                prop.text "Describe what you want the assistant to do. It will propose commands for your approval."
            ]

            Html.textarea [
                prop.style [
                    style.width (length.percent 100)
                    style.minHeight 150
                    style.padding 15
                    style.fontSize 16
                    style.fontFamily "monospace"
                    style.borderRadius 5
                    style.border (1, borderStyle.solid, "#bdc3c7")
                    style.marginBottom 15
                ]
                prop.placeholder "e.g., List all Python files in the project and show their sizes"
                prop.value model.InputText
                prop.onChange (fun value -> dispatch (UpdateInputText value))
            ]

            Html.button [
                prop.style [
                    style.padding (15, 30)
                    style.backgroundColor "#27ae60"
                    style.color "white"
                    style.border.none
                    style.borderRadius 5
                    style.fontSize 16
                    style.cursor.pointer
                    style.opacity (if model.IsLoading then 0.6 else 1.0)
                ]
                prop.disabled model.IsLoading
                prop.text (if model.IsLoading then "Processing..." else "Submit")
                prop.onClick (fun _ -> dispatch SubmitInput)
            ]
        ]
    ]

// ===== COMMAND QUEUE VIEW =====

let commandCard (cmd: Command) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [
            style.backgroundColor "white"
            style.border (1, borderStyle.solid, "#ecf0f1")
            style.borderRadius 8
            style.padding 20
            style.marginBottom 15
            style.boxShadow (0, 2, 4, "#00000010")
        ]
        prop.children [
            Html.div [
                prop.style [
                    style.display.flex
                    style.justifyContent.spaceBetween
                    style.alignItems.center
                    style.marginBottom 10
                ]
                prop.children [
                    Html.span [
                        prop.style [
                            style.padding (5, 10)
                            style.backgroundColor (statusColor cmd.Status)
                            style.color "white"
                            style.borderRadius 4
                            style.fontSize 12
                            style.fontWeight.bold
                        ]
                        prop.text (statusText cmd.Status)
                    ]
                    Html.span [
                        prop.style [
                            style.fontSize 12
                            style.color "#95a5a6"
                        ]
                        prop.text (cmd.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                    ]
                ]
            ]

            Html.div [
                prop.style [
                    style.backgroundColor "#ecf0f1"
                    style.padding 15
                    style.borderRadius 5
                    style.fontFamily "monospace"
                    style.fontSize 14
                    style.marginBottom 10
                ]
                prop.text cmd.Command
            ]

            Html.p [
                prop.style [ style.color "#7f8c8d"; style.fontSize 14 ]
                prop.children [
                    Html.strong [ prop.text "Reasoning: " ]
                    Html.text cmd.Reasoning
                ]
            ]

            if cmd.IsWhitelisted then
                Html.p [
                    prop.style [ style.color "#27ae60"; style.fontSize 12 ]
                    prop.text "✓ Whitelisted (read-only command)"
                ]

            match cmd.Status with
            | Pending ->
                Html.div [
                    prop.style [ style.display.flex; style.gap 10 ]
                    prop.children [
                        Html.button [
                            prop.style [
                                style.padding (10, 20)
                                style.backgroundColor "#27ae60"
                                style.color "white"
                                style.border.none
                                style.borderRadius 5
                                style.cursor.pointer
                            ]
                            prop.text "✓ Approve"
                            prop.onClick (fun _ -> dispatch (ApproveCommand cmd.Id))
                        ]
                        Html.button [
                            prop.style [
                                style.padding (10, 20)
                                style.backgroundColor "#e74c3c"
                                style.color "white"
                                style.border.none
                                style.borderRadius 5
                                style.cursor.pointer
                            ]
                            prop.text "✗ Reject"
                            prop.onClick (fun _ -> dispatch (RejectCommand cmd.Id))
                        ]
                    ]
                ]
            | Approved ->
                Html.button [
                    prop.style [
                        style.padding (10, 20)
                        style.backgroundColor "#3498db"
                        style.color "white"
                        style.border.none
                        style.borderRadius 5
                        style.cursor.pointer
                    ]
                    prop.text "▶ Execute"
                    prop.onClick (fun _ -> dispatch (ExecuteCommand cmd.Id))
                ]
            | _ -> Html.none
        ]
    ]

let commandQueueView (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [
            style.maxWidth 1000
            style.margin.auto
            style.padding 20
        ]
        prop.children [
            Html.div [
                prop.style [
                    style.display.flex
                    style.justifyContent.spaceBetween
                    style.alignItems.center
                    style.marginBottom 20
                ]
                prop.children [
                    Html.h2 [ prop.text "Command Queue" ]
                    Html.button [
                        prop.style [
                            style.padding (10, 20)
                            style.backgroundColor "#3498db"
                            style.color "white"
                            style.border.none
                            style.borderRadius 5
                            style.cursor.pointer
                        ]
                        prop.text "🔄 Refresh"
                        prop.onClick (fun _ -> dispatch LoadCommands)
                    ]
                ]
            ]

            if model.Commands.IsEmpty then
                Html.div [
                    prop.style [
                        style.textAlign.center
                        style.padding 40
                        style.color "#95a5a6"
                    ]
                    prop.text "No commands yet. Submit a task to get started!"
                ]
            else
                Html.div [
                    prop.children (model.Commands |> List.map (fun cmd -> commandCard cmd dispatch))
                ]
        ]
    ]

// ===== RESULTS VIEW =====

let resultCard (result: ExecutionResult) =
    Html.div [
        prop.style [
            style.backgroundColor "white"
            style.border (1, borderStyle.solid, "#ecf0f1")
            style.borderRadius 8
            style.padding 20
            style.marginBottom 15
            style.boxShadow (0, 2, 4, "#00000010")
        ]
        prop.children [
            Html.div [
                prop.style [
                    style.display.flex
                    style.justifyContent.spaceBetween
                    style.marginBottom 10
                ]
                prop.children [
                    Html.span [
                        prop.style [
                            style.padding (5, 10)
                            style.backgroundColor (if result.ExitCode = 0 then "#27ae60" else "#e74c3c")
                            style.color "white"
                            style.borderRadius 4
                            style.fontSize 12
                            style.fontWeight.bold
                        ]
                        prop.text (if result.ExitCode = 0 then "Success" else sprintf "Failed (exit code: %d)" result.ExitCode)
                    ]
                    Html.span [
                        prop.style [
                            style.fontSize 12
                            style.color "#95a5a6"
                        ]
                        prop.text (result.ExecutedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                    ]
                ]
            ]

            Html.pre [
                prop.style [
                    style.backgroundColor "#2c3e50"
                    style.color "#ecf0f1"
                    style.padding 15
                    style.borderRadius 5
                    style.fontFamily "monospace"
                    style.fontSize 12
                    style.overflowX.auto
                    style.maxHeight 300
                    style.overflowY.auto
                ]
                prop.text result.Output
            ]

            match result.Error with
            | Some err when not (System.String.IsNullOrWhiteSpace(err)) ->
                Html.div [
                    prop.style [
                        style.backgroundColor "#fee"
                        style.color "#c33"
                        style.padding 10
                        style.borderRadius 5
                        style.marginTop 10
                        style.fontSize 12
                    ]
                    prop.children [
                        Html.strong [ prop.text "Error: " ]
                        Html.pre [ prop.text err ]
                    ]
                ]
            | _ -> Html.none
        ]
    ]

let resultsView (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [
            style.maxWidth 1000
            style.margin.auto
            style.padding 20
        ]
        prop.children [
            Html.div [
                prop.style [
                    style.display.flex
                    style.justifyContent.spaceBetween
                    style.alignItems.center
                    style.marginBottom 20
                ]
                prop.children [
                    Html.h2 [ prop.text "Execution Results" ]
                    Html.button [
                        prop.style [
                            style.padding (10, 20)
                            style.backgroundColor "#3498db"
                            style.color "white"
                            style.border.none
                            style.borderRadius 5
                            style.cursor.pointer
                        ]
                        prop.text "🔄 Refresh"
                        prop.onClick (fun _ -> dispatch LoadResults)
                    ]
                ]
            ]

            if model.Results.IsEmpty then
                Html.div [
                    prop.style [
                        style.textAlign.center
                        style.padding 40
                        style.color "#95a5a6"
                    ]
                    prop.text "No execution results yet."
                ]
            else
                Html.div [
                    prop.children (model.Results |> List.map resultCard)
                ]
        ]
    ]

// ===== ERROR MESSAGE =====

let errorMessage (msg: string) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [
            style.backgroundColor "#e74c3c"
            style.color "white"
            style.padding 15
            style.borderRadius 5
            style.marginBottom 20
            style.display.flex
            style.justifyContent.spaceBetween
            style.alignItems.center
        ]
        prop.children [
            Html.span [ prop.text msg ]
            Html.button [
                prop.style [
                    style.backgroundColor "transparent"
                    style.color "white"
                    style.border.none
                    style.fontSize 20
                    style.cursor.pointer
                ]
                prop.text "×"
                prop.onClick (fun _ -> dispatch ClearError)
            ]
        ]
    ]

// ===== MAIN VIEW =====

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [
            style.fontFamily "system-ui, -apple-system, sans-serif"
            style.backgroundColor "#ecf0f1"
            style.minHeight (length.vh 100)
        ]
        prop.children [
            navbar model dispatch

            Html.div [
                prop.style [ style.maxWidth 1200; style.margin.auto ]
                prop.children [
                    match model.ErrorMessage with
                    | Some err -> errorMessage err dispatch
                    | None -> Html.none

                    match model.CurrentView with
                    | InputView -> inputView model dispatch
                    | CommandQueueView -> commandQueueView model dispatch
                    | ResultsView -> resultsView model dispatch
                ]
            ]
        ]
    ]
