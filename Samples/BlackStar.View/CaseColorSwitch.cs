using Color = System.Drawing.Color;

namespace BlackStar.View;

internal class CaseColorSwitch
{
    const int STAGNATION = 40;
    const int POP = 4;
    const int GENERATION = 100;
    private static int NREQUIRE = 1;
    static DateTime baseDt = new(2023, 1, 1);

    public static async IAsyncEnumerable<Scene> OptimColorSwitch()
    {
        var bom = createBom();
        var needs = createNeeds();                      //读入机器能力
        var resources = createResources();      //读入机器排班表
        var switches = createSwitches();        //读入物料切换时间
        MainWindow.Colors = createColors();
        var solver = new SortBomTransolution(bom, NREQUIRE, needs, resources, switches: switches, population: POP, generation: GENERATION, stagnation: STAGNATION);
        await foreach(Scene scene in solver.Solve())
            yield return scene;
    }

    private static PooledDictionary<string, ScottPlot.Color> createColors()
    {
        PooledDictionary<string, ScottPlot.Color> colors = new();
        colors.Add("Switch", ScottPlot.Colors.Orange);
        colors.Add("白绿产品染色白", ScottPlot.Colors.White);
        colors.Add("白绿产品染色绿", ScottPlot.Colors.LightGreen);
        colors.Add("蓝绿产品染色蓝", ScottPlot.Colors.Blue);
        colors.Add("蓝绿产品染色绿", ScottPlot.Colors.Green);
        return colors;
    }

    private static PooledDictionary<string, IResource> createResources()
    {
        PooledDictionary<string, IResource> resources = new();
        TimeSpan last = TimeSpan.FromHours(4800);
        DateTime to = baseDt + last;

        resources.Add("染缸1", new Resource<bool>("染缸1")
        {
            States = new() { new State<bool>("染色", baseDt, to, true) },  //服务能力
            Variables = new()
            {
                ["Current"] = new(""),
                ["Color"] = new Variable(Color.White),
                ["NeedSwitch"] = new(true)
            },
            Decides = new()
            {
                ["Switch"] = ColorSwitchAction  //定制的颜色切换行为
            },
        });
        resources.Add("染缸2", new Resource<bool>("染缸2")
        {
            States = new() { new State<bool>("染色", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["Color"] = new Variable(Color.White),
                ["NeedSwitch"] = new(true),
            },
            Decides = new()
            {
                ["Switch"] = ColorSwitchAction
            },
        });
        resources.Add("收纳线", new Resource<bool>("收纳线")
        {
            States = new() { new State<bool>("收纳", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(false),
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });

        return resources;
    }
    
    public static Delegates.SwitchByResourceBom ColorSwitchAction = (resource, nextBom) =>
    {
        var considerSwitch = resource.Scene.Variables["ConsiderSwitch"].GetBoolValue();
        if (!considerSwitch)
            return false;
        
        if (nextBom.Variables == null)
            return false;
        
        Color currentColor = (Color)resource.Variables["Color"].GetObjectValue(); //当前加工颜色R
        Color nextColor = (Color)nextBom.Variables["Color"].GetObjectValue(); //下一个加工颜色R
        
        if(currentColor.R + currentColor.G + currentColor.B < 255 * 1.5 //深色 
           && nextColor.R + nextColor.G + nextColor.B > 255 * 1.5)      //浅色
            return true;

        return false;
    };

    private static PooledList<IServiceAbility> createNeeds()
    {
        return
        [
            new ServiceAbility("默认批次", "收纳", TimeSpan.FromSeconds(0)),
            new ServiceAbility("蓝绿产品", "收纳", TimeSpan.FromSeconds(0)),
            new ServiceAbility("白绿产品", "收纳", TimeSpan.FromSeconds(0)),
            new ServiceAbility("蓝绿产品染色蓝", "染色", TimeSpan.FromSeconds(34)),
            new ServiceAbility("白绿产品染色白", "染色", TimeSpan.FromSeconds(30)),
            new ServiceAbility("蓝绿产品染色绿", "染色", TimeSpan.FromSeconds(40)),
            new ServiceAbility("白绿产品染色绿", "染色", TimeSpan.FromSeconds(40)),
        ];
    }

    private static Bom createBom()
    {
        var batch = new Bom("默认批次");

        var bomBlueGreen = new Bom("蓝绿产品");
        
        var bom蓝绿1 = new Bom("蓝绿产品染色蓝"){
            Variables = new()
            {
                ["Color"] = new Variable(Color.Blue),
            }
        };
        var bom蓝绿2 = new Bom("蓝绿产品染色绿"){
            Variables = new()
            {
                ["Color"] = new Variable(Color.Green),
            }
        };
        bom蓝绿2.AddSubBom(bom蓝绿1);
        bomBlueGreen.AddSubBom(bom蓝绿2);
        
        var bomWhiteGreen = new Bom("白绿产品");
        var bom白绿1 = new Bom("白绿产品染色白")
        {
            Variables = new()
            {
                ["Color"] = new Variable(Color.White),
            }
        };;
        var bom白绿2 = new Bom("白绿产品染色绿")
        {
            Variables = new()
            {
                ["Color"] = new Variable(Color.Green),
            }
        };
        bom白绿2.AddSubBom(bom白绿1);
        bomWhiteGreen.AddSubBom(bom白绿2);
      
        batch.AddSubBom(bomBlueGreen, 10);
        batch.AddSubBom(bomWhiteGreen, 30);
        return batch;
    }

    private static PooledDictionary<string, TimeSpan> createSwitches()
    {
        PooledDictionary<string, TimeSpan> switches = new()
        {
            ["默认批次"] = TimeSpan.FromMinutes(0),
            ["蓝绿产品"] = TimeSpan.FromMinutes(0),
            ["白绿产品"] = TimeSpan.FromMinutes(0),
            ["蓝绿产品染色蓝"] = TimeSpan.FromMinutes(10), //清洗时间，如果有
            ["白绿产品染色白"] = TimeSpan.FromMinutes(10), //清洗时间，如果有
            ["蓝绿产品染色绿"] = TimeSpan.FromMinutes(10), //清洗时间，如果有
            ["白绿产品染色绿"] = TimeSpan.FromMinutes(10), //清洗时间，如果有
        };
        return switches;
    }
}