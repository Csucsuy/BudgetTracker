﻿namespace BudgetTracker

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

    type TransactionType = | Income | Expense
    type Record = { Id: int; Type: TransactionType; Category: string; Amount: int }
    
    // Unique ID generation
    let mutable nextId = 1
    
    // ListModel for transactions - using Id as key
    let Transactions = ListModel.Create (fun r -> r.Id) []
    
    // Global variable to store the chart object
    [<JavaScript>]
    let mutable currentChart : obj option = None

    let Main () =
        let currentPage = Var.Create Home
        let newType = Var.Create Income
        let newCategory = Var.Create "Entertainment"
        let newAmount = Var.Create ""
        let homeNewType = Var.Create Income
        let homeNewCategory = Var.Create "Entertainment"
        let homeNewAmount = Var.Create ""

        let menuItem page label =
            let isActive = currentPage.View.Map(fun p -> if p = page then "active" else "")
            li [attr.classDyn isActive] [
                a [
                    on.click (fun _ _ -> 
                        // Destroy previous chart before navigation
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

            // Calculate total income and expense for normalization
            let totalIncome =
                Transactions.Value
                |> Seq.filter (fun r -> r.Type = Income)
                |> Seq.sumBy (fun r -> r.Amount)
                |> float

            let totalExpense =
                Transactions.Value
                |> Seq.filter (fun r -> r.Type = Expense)
                |> Seq.sumBy (fun r -> r.Amount)
                |> float

            // Calculate percentage data
            let incomeData =
                categories
                |> Array.map (fun category ->
                    let categoryIncome =
                        Transactions.Value
                        |> Seq.filter (fun r -> r.Type = Income && r.Category = category)
                        |> Seq.sumBy (fun r -> r.Amount)
                        |> float
                    if totalIncome > 0.0 then
                        (categoryIncome / totalIncome) * 100.0
                    else
                        0.0
                )

            let expenseData =
                categories
                |> Array.map (fun category ->
                    let categoryExpense =
                        Transactions.Value
                        |> Seq.filter (fun r -> r.Type = Expense && r.Category = category)
                        |> Seq.sumBy (fun r -> r.Amount)
                        |> float
                    if totalExpense > 0.0 then
                        (categoryExpense / totalExpense) * 100.0
                    else
                        0.0
                )

            div [] [
                h3 [] [text "Income vs Expense by Category (Percentage Radar Chart)"]
                div [attr.id "chartContainer"] [
                    canvas [
                        attr.id "radarChartCanvas"
                        attr.width "450"
                        attr.height "300"
                        on.afterRender (fun canvas ->
                            // Destroy previous chart if it exists
                            match currentChart with
                            | Some chart -> 
                                JS.Inline("if ($0 && typeof $0.destroy === 'function') { $0.destroy(); }", chart)
                            | None -> ()
                            
                            let ctx = (canvas :?> CanvasElement).GetContext "2d"
                            
                            // Create JavaScript objects with proper type coercion
                            let chartData = 
                                JS.Inline("""
                                {
                                    labels: $0,
                                    datasets: [
                                        {
                                            label: "Income (% of Total Income)",
                                            backgroundColor: "rgba(54, 162, 235, 0.2)",
                                            borderColor: "rgba(54, 162, 235, 1)",
                                            pointBackgroundColor: "rgba(54, 162, 235, 1)",
                                            data: $1
                                        },
                                        {
                                            label: "Expense (% of Total Expense)",
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
                                            beginAtZero: true,
                                            max: 100,
                                            ticks: {
                                                stepSize: 20,
                                                callback: function(value) { return value + '%'; }
                                            }
                                        }
                                    },
                                    plugins: {
                                        tooltip: {
                                            callbacks: {
                                                label: function(context) {
                                                    return context.dataset.label + ': ' + context.parsed.r.toFixed(2) + '%';
                                                }
                                            }
                                        }
                                    }
                                }
                                """)

                            let chart = 
                                JS.Inline("new Chart($0, { type: 'radar', data: $1, options: $2 })", 
                                          ctx, chartData, chartOptions)
                            
                            // Store the new chart object
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
                        div [] [
                            h1 [] [text "Personal Budget Tracker"]
                            p [attr.style "font-style: italic;"] [text "Take control of your finances with this F#-powered Single-Page Application built using WebSharper."]
                            p [] [
                                    text "The Personal Budget Tracker is a simple Single-Page Application built with F# and WebSharper to help you manage your finances."
                                    br [] []
                                    text "This application allows you to track your income and expenses, visualize your financial data, and gain insights into your spending habits."
                                    br [] []
                                    text "Track your income and expenses effortlessly, gain insights through analytics, and make informed financial decisions to achieve better savings."]
                            div [attr.style "margin-top: 20px; padding: 15px; background-color: #f5f8fa; border-radius: 5px;"] [
                                h3 [] [text "Financial Snapshot"]
                                Doc.BindView (fun transactions ->
                                    if Seq.isEmpty transactions then
                                        p [attr.style "color: #888;"] [text "No transactions yet. Add some to see your financial overview."]
                                    else
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
                            div [attr.style "margin-top: 20px;"] [
                                h3 [] [text "Quick Add Transaction"]
                                div [attr.style "display: flex; gap: 10px; align-items: center;"] [
                                    select [
                                        on.change (fun el _ -> homeNewType.Value <- if el?value = "Income" then Income else Expense)
                                    ] [
                                        option [attr.value "Income"] [text "Income"]
                                        option [attr.value "Expense"] [text "Expense"]
                                    ]
                                    select [
                                        on.change (fun el _ -> homeNewCategory.Value <- el?value)
                                    ] [
                                        option [attr.value "Entertainment"] [text "Entertainment"]
                                        option [attr.value "Food"] [text "Food"]
                                        option [attr.value "Salary"] [text "Salary"]
                                        option [attr.value "Transport"] [text "Transport"]
                                        option [attr.value "Bills"] [text "Bills"]
                                    ]
                                    input [
                                        attr.placeholder "Amount (Ft)"
                                        attr.value homeNewAmount.Value
                                        on.input (fun el _ -> homeNewAmount.Value <- el?value)
                                        attr.``type`` "number"
                                        attr.step "1"
                                        attr.style "width: 100px;"
                                    ] []
                                    button [
                                        on.click (fun _ _ ->
                                            let amountStr = if isNull homeNewAmount.Value then "" else homeNewAmount.Value
                                            let parsedValue = JS.ParseInt(amountStr)
                                            if not (JS.IsNaN(parsedValue)) && not (isNull amountStr) && amountStr.Length > 0 then
                                                let newRecord = { 
                                                    Id = nextId
                                                    Type = homeNewType.Value
                                                    Category = homeNewCategory.Value
                                                    Amount = parsedValue 
                                                }
                                                Transactions.Add(newRecord)
                                                nextId <- nextId + 1
                                                homeNewAmount.Value <- ""
                                            else
                                                JS.Alert("Invalid amount! Please enter a valid integer (e.g., 1234).")
                                        )
                                        attr.style "background-color: #4CAF50; color: white; border: none; padding: 8px 16px; cursor: pointer; border-radius: 3px;"
                                    ] [text "Add"]
                                ]
                            ]
                        ]
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
                                            // Create new record with unique ID
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
                            // Transaction count and basic filtering
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
                            // Summary statistics
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
                            
                            // Category breakdown table
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
                                                // Use Seq.map instead of forEach and concatenate results
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