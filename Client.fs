namespace BudgetTracker

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.UI.Templating

[<JavaScript>]
module Client =

    type Pages =
        | Home
        | Records
        | Analytics

    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let People =
        ListModel.FromSeq [
            "John"
            "Paul"
        ]

    type TransactionType = | Income | Expense
    type Record = { Type: TransactionType; Category: string; Amount: int }
    let Transactions = ListModel.Create (fun r -> r.Type, r.Category, r.Amount) []

    let Main () =
        let newName = Var.Create ""
        let currentPage = Var.Create Home
        let newType = Var.Create Income
        let newCategory = Var.Create "Entertainment"
        let newAmount = Var.Create ""

        let menuItem page label =
            let isActive = currentPage.View.Map(fun p -> if p = page then "active" else "")
            li [attr.classDyn isActive] [
                a [
                    on.click (fun _ _ -> currentPage.Value <- page)
                    attr.style "cursor: pointer;"
                ] [text label]
            ]

        let renderChart () =
            let incomeTotal = Transactions.Value |> Seq.filter (fun r -> r.Type = Income) |> Seq.sumBy (fun r -> r.Amount)
            let expenseTotal = Transactions.Value |> Seq.filter (fun r -> r.Type = Expense) |> Seq.sumBy (fun r -> r.Amount)
            let chartData = New [
                "labels" => [| "Income"; "Expense" |]
                "datasets" => [| New [
                    "label" => "Amount (Ft)"
                    "data" => [| incomeTotal; expenseTotal |]
                    "backgroundColor" => [| "#36A2EB"; "#FF6384" |]
                ] |]
            ]
            let chartOptions = New [
                "responsive" => true
                "scales" => New [
                    "y" => New ["beginAtZero" => true]
                ]
            ]
            let canvas = JS.Document.GetElementById "chartCanvas"
            let ctx = (canvas :?> CanvasElement).GetContext "2d"
            let config = New [
                "type" => "bar"
                "data" => chartData
                "options" => chartOptions
            ]
            ignore (New["Chart", ctx, config])

        div [attr.style "display: flex; height: 100vh;"] [
            div [attr.style "width: 200px; background-color: #f5f8fa; display: flex; flex-direction: column; justify-content: space-between; padding: 20px;"] [
                div [] [
                    h2 [attr.style "margin: 0; font-size: 18px;"] [text "Budget Tracker"]
                    p [attr.style "margin: 5px 0; font-size: 14px; color: #666;"] [text "for better savings!"]
                ]
                ul [attr.style "list-style: none; padding: 0; flex-grow: 1;"] [
                    menuItem Home "Home"
                    menuItem Records "Transactions"
                    menuItem Analytics "Analytics"
                ]
                div [] [
                    p [attr.style "margin: 0; font-size: 12px; color: #888;"] [text "Owner: Patrik Grasics"]
                ]
            ]
            div [attr.style "flex-grow: 1; padding: 20px; overflow-y: auto;"] [
                currentPage.View |> Doc.BindView (fun page ->
                    match page with
                    | Home ->
                        IndexTemplate.Main()
                            .ListContainer(
                                People.View.DocSeqCached(fun (name: string) ->
                                    IndexTemplate.ListItem().Name(name).Doc()
                                )
                            )
                            .Name(newName)
                            .Add(fun _ ->
                                People.Add(newName.Value)
                                newName.Value <- ""
                            )
                            .Doc()
                    | Records ->
                        div [] [
                            h1 [] [text "Transactions"]
                            div [attr.style "margin-bottom: 20px;"] [
                                select [
                                    on.change (fun el _ -> newType.Value <- if el?page = "Income" then Income else Expense)
                                ] [
                                    option [attr.value "Income"] [text "Income"]
                                    option [attr.value "Expense"] [text "Expense"]
                                ]
                                select [
                                    on.change (fun el _ -> newCategory.Value <- el?page)
                                ] [
                                    option [attr.value "Entertainment"] [text "Entertainment"]
                                    option [attr.value "Food"] [text "Food"]
                                    option [attr.value "Salary"] [text "Salary"]
                                ]
                                input [
                                    attr.placeholder "Amount (Ft)"
                                    attr.value newAmount.Value
                                    on.input (fun el _ -> newAmount.Value <- el?page)
                                    //attr.type' "number"
                                    attr.step "1"
                                ] []
                                button [
                                    on.click (fun _ _ ->
                                        let amountStr = if isNull newAmount.Value then "" else newAmount.Value
                                        let parsedValue = JS.ParseInt(amountStr)
                                        if not (JS.IsNaN(parsedValue)) && not (isNull amountStr) && amountStr.Length > 0 then
                                            Transactions.Add { Type = newType.Value; Category = newCategory.Value; Amount = parsedValue }
                                            newAmount.Value <- ""
                                        else
                                            JS.Alert("Invalid amount! Please enter a valid integer (e.g., 1234).")
                                    )
                                ] [text "Add"]
                            ]
                            table [attr.style "width: 100%; border-collapse: collapse;"] [
                                thead [] [
                                    tr [] [
                                        th [attr.style "border: 1px solid #ddd; padding: 8px;"] [text "Type"]
                                        th [attr.style "border: 1px solid #ddd; padding: 8px;"] [text "Category"]
                                        th [attr.style "border: 1px solid #ddd; padding: 8px;"] [text "Amount (Ft)"]
                                    ]
                                ]
                                tbody [] [
                                    Transactions.View.DocSeqCached(fun record ->
                                        tr [] [
                                            td [attr.style "border: 1px solid #ddd; padding: 8px;"] [
                                                text (match record.Type with | Income -> "Income" | Expense -> "Expense")
                                            ]
                                            td [attr.style "border: 1px solid #ddd; padding: 8px;"] [text record.Category]
                                            td [attr.style "border: 1px solid #ddd; padding: 8px;"] [text (string record.Amount)]
                                        ]
                                    )
                                ]
                            ]
                        ]
                    | Analytics ->
                        div [] [
                            h1 [] [text "Analytics"]
                            canvas [attr.id "chartCanvas"; attr.style "max-height: 400px;"] []
                            on.afterRender (fun _ -> renderChart())
                        ]
                )
            ]
        ]

    [<SPAEntryPoint>]
    let Run () =
        Main() |> Doc.RunById "main"