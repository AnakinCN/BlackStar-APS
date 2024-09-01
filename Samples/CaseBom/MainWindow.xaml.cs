using BlackStar.Message;
using BlackStar.Message;
using Collections.Pooled;
using Colors = ScottPlot.Colors;

namespace BlackStar.View;

public partial class MainWindow : Window
{
    static DateTime baseDt = new(2023, 1, 1);

    public MainWindow()
    {
        InitializeComponent();

        MessageOP.Initialize();
        MessageOP.MessageOf<IntTsEvent>().Subscribe(OnIntTsEvent);
        MessageOP.MessageOf<NotifyMessage>().Subscribe(OnNotifyMessage);

        initDescendendWindow();
        RunSample();

    }

    private void OnNotifyMessage(string message)
    {
        switch (message)
        {
            case "nolic":
                this.nolic.Visibility = Visibility.Visible;
                break;
            default:
                break;
        }
    }

    private async Task RunSample()
    {
        //Scene scene = await Task.Run( () => CaseNoBom.OptimNoBom());
        //Scene scene = await Task.Run( () => CaseInt.OptimNoBom());
        Scene scene = await Task.Run( () => CaseBom.OptimBom());
        //Scene scene = await Task.Run( () => CaseDig.OptimDig());
        //Scene scene = await Task.Run( () => CaseLight.OptimLight());
        //Scene scene = await Task.Run(() => Case5k.Optim5k());

        this.Draw(scene);
        Report(scene);
    }

    /// <summary>
    /// 绘图
    /// </summary>
    /// <param name="message"></param>
    private void OnIntTsEvent(IntTimespanMessage message)
    {
        this.Dispatcher.BeginInvoke(() => this.decentWind.AddDraw(message.IntValue, message.TsValue));
    }

    private void Report(Scene scene)
    {
        if (scene is null)
            return;

        JArray root = new();

        foreach (var deploy in scene.Deploys)
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

    private DescendendWindow decentWind;
    private void initDescendendWindow()
    {
        decentWind = new();
        decentWind.Show();
        //wind.DrawDescendence(scene.Descendence);
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
            if (scene.Deploys.Count <= 100)
            {
                var text = GanttChart.Plot.Add.Text(task.Name, x1, y);
                    text.LabelFontSize = 12;
                        text.LabelFontColor = Colors.Black;
            }
        }
        GanttChart.Plot.Axes.AutoScale();
        GanttChart.Refresh();
    }

    private void BtFit_OnClick(object sender, RoutedEventArgs e)
    {
        GanttChart.Plot.Axes.AutoScale();
        GanttChart.Refresh();
    }
}