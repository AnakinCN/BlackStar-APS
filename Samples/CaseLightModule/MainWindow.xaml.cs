using BlackStar.Event;
using BlackStar.View;
using Color = System.Drawing.Color;

namespace BlackStar.View;

public partial class MainWindow : Window
{
    static DateTime baseDt = new(2023, 1, 1);
    private DescendendWindow decentWind;
    public MainWindow()
    {
        InitializeComponent();

        if (!EventOP.Initilized)
            EventOP.InitializeEventOP();
        EventOP.OneAggregator.GetEvent<IntTsEvent>().Subscribe(OnIntTsEvent);
        EventOP.OneAggregator.GetEvent<NotifyEvent>().Subscribe(OnNotifyEvent);

        initDescendendWindow();
        RunSample();

    }

    private void OnNotifyEvent(string message)
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

        this.Draw(scene, randomColor: true);
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

    private void Draw(Scene scene, bool randomColor = true)
    {
        Dictionary<string, Color> colors = new()
        {
            {"D1", Color.Yellow},
            {"D2", Color.Blue},
            {"D3", Color.Green},
        };

        if (scene is null)
            return;

        const double TASKHEIGHT = 10d;
        const double LINEHEIGHT = 20d;

        Dictionary<string, int> rows = new();
        Dictionary<int, string> rowLables = new();
        int row = 1;
        foreach (var resource in scene.Resources)
        {
            if (rows.TryAdd(resource.Key, row))
            {
                rowLables.Add(row, resource.Key);
                row++;
            }
        }

        // Configure the plot
        GanttChart.Plot.XLabel("Time");
        GanttChart.Plot.XAxis.DateTimeFormat(true);
        GanttChart.Plot.XAxis.Ticks(true, false);
        GanttChart.Plot.XAxis.TickLabelFormat(StringOP.MinutesToDtLabel);

        GanttChart.Plot.YLabel("Resource");
        //Add horizontal bars for each task
        GanttChart.Plot.YTicks(
            rows.Values.Select(i => i * LINEHEIGHT).ToArray(), rowLables.Values.ToArray()
            );
        //GanttChart.Plot.YAxis.TickLabelFormat(y =>
        //    {
        //        if (rowLables.TryGetValue((int)Math.Round(y), out string val))
        //            return val;
        //        return "";
        //    }
        //);
        //GanttChart.Plot.YAxis.AutomaticTickPositions();

        foreach (var task in scene.Deploys)
        {
            int line = rows[task.UseResource];
            var y = line * LINEHEIGHT;
            var halfHeight = TASKHEIGHT / 2;
            var x1 = (task.From - baseDt).TotalMinutes;
            var x2 = (task.To - baseDt).TotalMinutes;
            var y1 = y - halfHeight;
            var y2 = y + halfHeight;
            double[] xs = { x1, x2, x2, x1 };
            double[] ys = { y1, y1, y2, y2 };

            if (randomColor)
                GanttChart.Plot.AddPolygon(xs, ys);
            else
            {
                Color color = default;
                foreach (var pair in colors)
                {
                    if (task.Name.Contains("_" + pair.Key))
                    {
                        color = pair.Value;
                        break;
                    }
                }

                GanttChart.Plot.AddPolygon(xs, ys, fillColor: color);
            }

            if (scene.Deploys.Count <= 100)
                GanttChart.Plot.AddText(task.Name, x1, y, size: 12, color: Color.Black);
        }

        GanttChart.Render();
    }

    private void BtFit_OnClick(object sender, RoutedEventArgs e)
    {
        GanttChart.Plot.AxisAuto();
        GanttChart.Refresh();
    }
}