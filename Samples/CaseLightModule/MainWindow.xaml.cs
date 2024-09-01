using BlackStar.Message;
using BlackStar.View;
using Collections.Pooled;
using Colors = ScottPlot.Colors;

namespace BlackStar.View;

public partial class MainWindow : Window
{
    static DateTime baseDt = new(2023, 1, 1);
    private DescendendWindow decentWind;
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
        //Scene scene = await Task.Run( () => CaseBom.OptimBom());
        //Scene scene = await Task.Run( () => CaseDig.OptimDig());
        Scene scene = await Task.Run( () => CaseLight.OptimLight());
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
        int row = 1;
        foreach (var resource in scene.Resources)
        {
            if(rows.TryAdd(resource.Key, row))
            {
                rowLables.Add(row, resource.Key);
                row++;
            }
        }

        WpfPlot1.Plot.XLabel("Time");
        WpfPlot1.Plot.YLabel("Resource");
        #region 资源轴
        ScottPlot.TickGenerators.NumericManual yticks = new();
        int ipair = 0;
        foreach (var pair in rows)
        {
            var tick = new Tick(ipair * LINEHEIGHT, pair.Key);
            yticks.Add(tick);
            ipair++;
        }
        WpfPlot1.Plot.Axes.Left.TickGenerator = yticks;
        WpfPlot1.Plot.Axes.Left.TickLabelStyle.FontName = "微软雅黑";
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
          
            var rect = WpfPlot1.Plot.Add.Rectangle(x1, x2, y1, y2);
            if (scene.Deploys.Count <= 100)
            {
                var text = WpfPlot1.Plot.Add.Text(task.Name, x1, y);
                    text.LabelFontSize = 12;
                        text.LabelFontColor = Colors.Black;
            }
        }
        WpfPlot1.Plot.Axes.AutoScale();
        WpfPlot1.Refresh();
    }

    private void BtFit_OnClick(object sender, RoutedEventArgs e)
    {
        WpfPlot1.Plot.Axes.AutoScale();
        WpfPlot1.Refresh();
    }
}