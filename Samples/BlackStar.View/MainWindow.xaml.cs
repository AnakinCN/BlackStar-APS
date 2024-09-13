using Colors = ScottPlot.Colors;

namespace BlackStar.View;

public partial class MainWindow
{
    static DateTime baseDt = new (2023, 1, 1);
    private DescendendWindow decentWind;
    public static PooledDictionary<string, ScottPlot.Color> Colors { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();

        MessageOP.Initialize();
        MessageOP.MessageOf<IntTsEvent>().Subscribe(OnIntTsEvent);
        MessageOP.MessageOf<NotifyMessage>().Subscribe(OnNotifyEvent);
        MessageOP.MessageOf<DualMessage>().Subscribe(OnDualMessage);
        initDescendendWindow();
        //RunSample();
      
        BlackStar.View.Menu menu = new();
        menu.Show();
    }

    delegate IAsyncEnumerable<Scene> Optim();
    private async void OnDualMessage((string, string) message)
    {
        if (message.Item1 != "Sample")
            return;
        this.Clear();
        Optim optim = message.Item2 switch
        {
            "布尔模型无Bom" => CaseBoolNoBom.OptimBoolNoBom,
            "整数模型无Bom" => CaseIntNoBom.OptimIntNoBom,
            "布尔模型有Bom" => CaseBom.OptimBoolBom,
            "DIG：资源偏好定制" => CaseDig.OptimDig,
            "光模块工艺" => CaseLight.OptimLight,
            "SMT工艺" => Case5k.Optim5k,
            "染料切换" => CaseColorSwitch.OptimColorSwitch,
            _ => null
        };

        if (optim == null)
            return;
        
        await foreach (Scene scene in optim())
        {
            this.Draw(scene);
            Report(scene);
        }
        MessageOP.MessageOf<NotifyMessage>().Publish("Done");
    }

    private void Clear()
    {
        this.GanttChart.Plot.Clear();
    }

    private async Task RunSample()
    {
        await foreach (Scene scene in CaseBoolNoBom.OptimBoolNoBom())
        {
            this.Draw(scene);
            Report(scene);
        }
        
        //Scene scene = await Task.Run( () => );
        //Scene scene = await Task.Run( () => CaseIntNoBom.OptimIntNoBom());
        //Scene scene = await Task.Run( () => CaseBom.OptimBom());
        //Scene scene = await Task.Run(() => CaseDig.OptimDig());
        //Scene scene = await Task.Run( () => CaseLight.OptimLight());
        //Scene scene = await Task.Run(() => Case5k.Optim5k());
    }

    /// <summary>
    /// 绘下降曲线图
    /// </summary>
    /// <param name="message"></param>
    private void OnIntTsEvent(IntTimespanMessage message)
    {
        this.Dispatcher.BeginInvoke(() => this.decentWind.AddDraw(message.IntValue, message.TsValue));
    }

    private void OnNotifyEvent(string message)
    {
        switch (message)
        {
            case "nolic":
                this.nolic.Visibility = Visibility.Visible;
                break;
        }
    }


    private void Report(Scene scene)
    {
        if (scene is null)
            return;

        JArray root = new();

        foreach (var deploy in scene.Deploys.Span)
        {
            root.Add(new JObject
            {
                ["需求名称"] = deploy.Name,
                ["承担机器"] = deploy.UseResource,
                ["加工开始"] = deploy.From,
                ["加工结束"] = deploy.To,
                ["加工时长"] = deploy.To - deploy.From,
            });
        }
        File.WriteAllText("plan.json", root.ToString());
    }

    private void initDescendendWindow()
    {
        decentWind = new();
        decentWind.Show();
    }

    private void Draw(Scene scene)
    {
        if (scene is null)
            return;

        const double TASKHEIGHT = 10d;
        const double LINEHEIGHT = 20d;
        const double halfHeight = TASKHEIGHT / 2;

        PooledDictionary<string, int> rows = new();
        PooledDictionary<int, string> rowLables = new();
        int row = 0;
        foreach (var resource in scene.Resources)
        {
            if(rows.TryAdd(resource.Key, row))
            {
                rowLables.Add(row, resource.Key);
                row++;
            }
        }

        GanttChart.Plot.XLabel("Time");
        GanttChart.Plot.YLabel("Resource");
        #region 资源轴
        ScottPlot.TickGenerators.NumericManual yticks = new();
        int ipair = 0;
        foreach (var pair in rows)
        {
            var tick = new Tick(ipair * LINEHEIGHT, pair.Key);
            yticks.Add(tick);
            ipair++;
        }
        GanttChart.Plot.Axes.Left.TickGenerator = yticks;
        GanttChart.Plot.Axes.Left.TickLabelStyle.FontName = "微软雅黑";
        #endregion
        #region 时间轴
        // ScottPlot.TickGenerators.NumericManual xticks = new();
        // for (int i = 0; i < (scene.Deploys.Select(i=>i.To).Max() - baseDt).TotalMinutes; i += 10)
        // {
        //     var tick = new Tick(i, TimeSpan.FromMinutes(i * 10d).ToString("HH:mm"));
        //     xticks.Add(tick);
        // }
        // GanttChart.Plot.Axes.Bottom.TickGenerator = xticks;
        // GanttChart.Plot.Axes.Bottom.TickLabelStyle.FontName = "微软雅黑";
        #endregion
        
        foreach (var task in scene.Deploys.Span)
        {
            int line = rows[task.UseResource];
            var y = line * LINEHEIGHT;
            var x1 = (task.From - baseDt).TotalMinutes;
            var x2 = (task.To - baseDt).TotalMinutes;
            var y1 = y - halfHeight;
            var y2 = y + halfHeight;
          
            var rect = GanttChart.Plot.Add.Rectangle(x1, x2, y1, y2);
            string bomname = StringOP.TrimLastUnderline(task.Name);
            if(Colors!=null && Colors.TryGetValue(bomname, out var color))
                rect.FillColor = color;
            if (scene.Deploys.Count <= 100)
            {
                var text = GanttChart.Plot.Add.Text(task.Name, x1, y);
                text.LabelFontName = "微软雅黑";
                text.LabelFontSize = 12;
                text.LabelFontColor = ScottPlot.Colors.Black;
            }
        }
        GanttChart.Plot.Axes.AutoScale();
        GanttChart.Refresh();
    }

    private void BtFit_OnClick(object sender, RoutedEventArgs e)
    {
        // GanttChart.Plot.AxisAuto();
        GanttChart.Plot.Axes.AutoScale();
        GanttChart.Refresh();
    }

    private void btDir_OnClick(object sender, RoutedEventArgs e)
    {
        string folder = System.IO.Path.Join(".", "Log", "Trace");
        Process.Start("explorer", folder);
    }
}