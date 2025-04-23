namespace BudgetTracker

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Sitelets

[<JavaScript>]
module Client =
    open WebSharper.UI.Html
    open WebSharper.Charting

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
    type Record = { Id: int; Type: TransactionType; Category: string; Amount: int }
    
    // Egyedi azonos�t� gener�l�s�hoz
    let mutable nextId = 1
    
    // ListModel m�dos�t�sa - csak az Id-t haszn�ljuk kulcsk�nt
    let Transactions = ListModel.Create (fun r -> r.Id) []
    
    // Glob�lis v�ltoz� a diagram objektum t�rol�s�hoz
    [<JavaScript>]
    let mutable currentChart : obj option = None

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
                    on.click (fun _ _ -> 
                        // El�z� chart megsemmis�t�se navig�ci� el�tt
                        match currentChart with
                        | Some chart -> 
                            JS.Inline("if ($0 && typeof $0.destroy === 'function') { $0.destroy(); }", chart)
                            currentChart <- None
                        | None -> ()
                        
                        currentPage.Value <- page
                    )
                    attr.style "cursor: pointer;"
                ] [text label]
            ]

        let RadarChart () =
            let categories = 
                Transactions.Value 
                |> Seq.map (fun r -> r.Category) 
                |> Seq.distinct 
                |> Seq.toArray

            let incomeData =
                categories
                |> Array.map (fun category ->
                    Transactions.Value
                    |> Seq.filter (fun r -> r.Type = Income && r.Category = category)
                    |> Seq.sumBy (fun r -> r.Amount)
                )

            let expenseData =
                categories
                |> Array.map (fun category ->
                    Transactions.Value
                    |> Seq.filter (fun r -> r.Type = Expense && r.Category = category)
                    |> Seq.sumBy (fun r -> r.Amount)
                )

            div [] [
                h3 [] [text "Income vs Expense by Category (Radar Chart)"]
                div [attr.id "chartContainer"] [
                    canvas [
                        attr.id "radarChartCanvas"
                        attr.width "450"
                        attr.height "300"
                        on.afterRender (fun canvas ->
                            // El�z� chart megsemmis�t�se, ha l�tezik
                            match currentChart with
                            | Some chart -> 
                                JS.Inline("if ($0 && typeof $0.destroy === 'function') { $0.destroy(); }", chart)
                            | None -> ()
                            
                            let ctx = (canvas :?> CanvasElement).GetContext "2d"
                            
                            // JavaScript objektumok l�trehoz�sa megfelel� t�pusk�nyszer�t�ssel
                            let chartData = 
                                JS.Inline("""
                                {
                                    labels: $0,
                                    datasets: [
                                        {
                                            label: "Income",
                                            backgroundColor: "rgba(54, 162, 235, 0.2)",
                                            borderColor: "rgba(54, 162, 235, 1)",
                                            pointBackgroundColor: "rgba(54, 162, 235, 1)",
                                            data: $1
                                        },
                                        {
                                            label: "Expense",
                                            backgroundColor: "rgba(255, 99, 132, 0.2)",
                                            borderColor: "rgba(255, 99, 132, 1)",
                                            pointBackgroundColor: "rgba(255, 99, 132, 1)",
                                            data: $2
                                        }
                                    ]
                                }
                                """, categories, incomeData, expenseData)
                            
                            let chartOptions = 
                                JS.Inline("""
                                {
                                    responsive: true,
                                    scales: {
                                        r: {
                                            beginAtZero: true
                                        }
                                    }
                                }
                                """)

                            let chart = 
                                JS.Inline("new Chart($0, { type: 'radar', data: $1, options: $2 })", 
                                          ctx, chartData, chartOptions)
                            
                            // Elt�rolom az �j chart objektumot
                            currentChart <- Some chart
                        )
                    ] []
                ]
            ]

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
                                    on.change (fun el _ -> newType.Value <- if el?value = "Income" then Income else Expense)
                                ] [
                                    option [attr.value "Income"] [text "Income"]
                                    option [attr.value "Expense"] [text "Expense"]
                                ]
                                select [
                                    on.change (fun el _ -> newCategory.Value <- el?value)
                                ] [
                                    option [attr.value "Entertainment"] [text "Entertainment"]
                                    option [attr.value "Food"] [text "Food"]
                                    option [attr.value "Salary"] [text "Salary"]
                                    option [attr.value "Transport"] [text "Transport"]
                                    option [attr.value "Bills"] [text "Bills"]
                                ]
                                input [
                                    attr.placeholder "Amount (Ft)"
                                    attr.value newAmount.Value
                                    on.input (fun el _ -> newAmount.Value <- el?value)
                                    attr.``type`` "number"
                                    attr.step "1"
                                ] []
                                button [
                                    on.click (fun _ _ ->
                                        let amountStr = if isNull newAmount.Value then "" else newAmount.Value
                                        let parsedValue = JS.ParseInt(amountStr)
                                        if not (JS.IsNaN(parsedValue)) && not (isNull amountStr) && amountStr.Length > 0 then
                                            // �j record l�trehoz�sa egyedi azonos�t�val
                                            let newRecord = { 
                                                Id = nextId
                                                Type = newType.Value
                                                Category = newCategory.Value
                                                Amount = parsedValue 
                                            }
                                            Transactions.Add(newRecord)
                                            nextId <- nextId + 1
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
                                        th [attr.style "border: 1px solid #ddd; padding: 8px;"] [text "Actions"]
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
                                            td [attr.style "border: 1px solid #ddd; padding: 8px;"] [
                                                button [
                                                    attr.style "background-color: #f44336; color: white; border: none; padding: 5px 10px; cursor: pointer; border-radius: 3px;"
                                                    on.click (fun _ _ -> 
                                                        Transactions.RemoveByKey(record.Id)
                                                    )
                                                ] [text "Delete"]
                                            ]
                                        ]
                                    )
                                ]
                            ]
                            // Tranzakci�k sz�ma �s alapvet� sz�r�s
                            div [attr.style "margin-top: 20px;"] [
                                Doc.BindView (fun transactions ->
                                    let count = Seq.length transactions
                                    let incomeCount = 
                                        transactions
                                        |> Seq.filter (fun r -> r.Type = Income)
                                        |> Seq.length
                                    let expenseCount = 
                                        transactions
                                        |> Seq.filter (fun r -> r.Type = Expense)
                                        |> Seq.length
                                    
                                    div [] [
                                        p [] [text (sprintf "Total transactions: %d (Income: %d, Expense: %d)" count incomeCount expenseCount)]
                                    ]
                                ) Transactions.View
                            ]
                            // Statisztika �sszes�t� - Doc.BindView haszn�lata
                            div [attr.style "margin-top: 20px; padding: 15px; background-color: #f5f8fa; border-radius: 5px;"] [
                                h3 [] [text "Summary"]
                                Doc.BindView (fun transactions ->
                                    let totalIncome = 
                                        transactions
                                        |> Seq.filter (fun r -> r.Type = Income)
                                        |> Seq.sumBy (fun r -> r.Amount)
                                    
                                    let totalExpense = 
                                        transactions
                                        |> Seq.filter (fun r -> r.Type = Expense)
                                        |> Seq.sumBy (fun r -> r.Amount)
                                    
                                    let balance = totalIncome - totalExpense
                                    
                                    div [] [
                                        p [attr.style "font-weight: bold;"] [
                                            text "Total Income: "
                                            span [attr.style "color: green;"] [text (string totalIncome + " Ft")]
                                        ]
                                        p [attr.style "font-weight: bold;"] [
                                            text "Total Expense: "
                                            span [attr.style "color: red;"] [text (string totalExpense + " Ft")]
                                        ]
                                        p [attr.style "font-weight: bold;"] [
                                            text "Balance: "
                                            span [attr.style (if balance >= 0 then "color: green;" else "color: red;")] [
                                                text (string balance + " Ft")
                                            ]
                                        ]
                                    ]
                                ) Transactions.View
                            ]
                        ]
                    | Analytics ->
                        div [] [
                            h1 [] [text "Analytics"]
                            RadarChart()
                            
                            // Kateg�ri�nk�nti bont�s t�bl�zata
                            Doc.BindView (fun transactions ->
                                if Seq.isEmpty transactions then
                                    div [attr.style "margin-top: 20px; color: #888;"] [
                                        text "No transactions yet. Add some transactions to see analytics."
                                    ]
                                else
                                    let categories = 
                                        transactions 
                                        |> Seq.map (fun r -> r.Category) 
                                        |> Seq.distinct 
                                        |> Seq.toList
                                    
                                    div [attr.style "margin-top: 30px;"] [
                                        h3 [] [text "Category Breakdown"]
                                        table [attr.style "width: 100%; border-collapse: collapse;"] [
                                            thead [] [
                                                tr [] [
                                                    th [attr.style "border: 1px solid #ddd; padding: 8px; text-align: left;"] [text "Category"]
                                                    th [attr.style "border: 1px solid #ddd; padding: 8px; text-align: right;"] [text "Income (Ft)"]
                                                    th [attr.style "border: 1px solid #ddd; padding: 8px; text-align: right;"] [text "Expense (Ft)"]
                                                    th [attr.style "border: 1px solid #ddd; padding: 8px; text-align: right;"] [text "Balance (Ft)"]
                                                ]
                                            ]
                                            tbody [] [
                                                // forEach helyett Seq.map haszn�lata �s eredm�nyeinek �sszef�z�se
                                                categories 
                                                |> List.map (fun category ->
                                                    let categoryIncome = 
                                                        transactions
                                                        |> Seq.filter (fun r -> r.Type = Income && r.Category = category)
                                                        |> Seq.sumBy (fun r -> r.Amount)
                                                    
                                                    let categoryExpense = 
                                                        transactions
                                                        |> Seq.filter (fun r -> r.Type = Expense && r.Category = category)
                                                        |> Seq.sumBy (fun r -> r.Amount)
                                                    
                                                    let categoryBalance = categoryIncome - categoryExpense
                                                    
                                                    tr [] [
                                                        td [attr.style "border: 1px solid #ddd; padding: 8px;"] [text category]
                                                        td [attr.style "border: 1px solid #ddd; padding: 8px; text-align: right;"] [text (string categoryIncome)]
                                                        td [attr.style "border: 1px solid #ddd; padding: 8px; text-align: right;"] [text (string categoryExpense)]
                                                        td [attr.style (sprintf "border: 1px solid #ddd; padding: 8px; text-align: right; font-weight: bold; color: %s" (if categoryBalance >= 0 then "green" else "red"))] [
                                                            text (string categoryBalance)
                                                        ]
                                                    ]
                                                )
                                                |> Doc.Concat
                                            ]
                                        ]
                                    ]
                            ) Transactions.View
                        ]
                )
            ]
        ]

    [<SPAEntryPoint>]
    let Run () =
        Main() |> Doc.RunById "main"