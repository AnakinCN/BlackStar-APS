namespace BlackStar.View;

public class CaseLight
{
    const int STAGNATION = 10;
    const int POP = 40;
    const int GENERATION = 100;
    private static int NREQUIRE = 40;
    static DateTime baseDt = new(2023, 1, 1);

    public static async IAsyncEnumerable<Scene> OptimLight()
    {
        //get the nRequire option in App.config
        var bom = createBom();
        //NREQUIRE = int.Parse(ConfigurationManager.AppSettings["nRequire"]);     //输入成品数

        var needs = createNeeds();                      //读入机器能力
        var resources = createResources();      //读入机器排班表
        var switches = createSwitches();        //读入物料切换时间
        var solver = new SortBomTransolution(bom, NREQUIRE, needs, resources, switches: switches, population: POP, generation: GENERATION, stagnation: STAGNATION);

        //Scene scene = null;
        await foreach(Scene scene in solver.Solve())
        {
            yield return scene;
        };
        //yield return scene;
    }

    private static PooledDictionary<string, IResource> createResources()
    {
        PooledDictionary<string, IResource> resources = new();
        TimeSpan last = TimeSpan.FromHours(4800);
        DateTime to = baseDt + last;

        resources.Add("GY0034阴阳板1", new Resource<bool>("GY0034阴阳板1")
        {
            States = new() { new State<bool>("治具", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(false)
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });
        resources.Add("GY0034阴阳板2", new Resource<bool>("GY0034阴阳板2")
        {
            States = new() { new State<bool>("治具", baseDt, to, true) },
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

        resources.Add("SMT01线", new Resource<bool>("SMT01线")
        {
            States = new() { new State<bool>("SMT01", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(true)
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });
        resources.Add("SMT02线", new Resource<bool>("SMT02线")
        {
            States = new() { new State<bool>("SMT02", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(true)
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });
        //{"SMT03线", new Resource<bool>("SMT03线") {States = new() { new State<bool>("SMT03", baseDt, to, true)}}},

        resources.Add("插件01线", new Resource<bool>("插件01线")
        {
            States = new() { new State<bool>("DIP01", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(true)
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });
        resources.Add("插件02线", new Resource<bool>("插件02线")
        {
            States = new() { new State<bool>("DIP02", baseDt, to, true) },
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(true)
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });

        resources.Add("组装01线", new Resource<bool>("组装01线")
        {
            States = [new State<bool>("组装", baseDt, to, true)],
            Variables = new()
            {
                ["Current"] = new(""),
                ["NeedSwitch"] = new(true),
            },
            Decides = new()
            {
                ["Switch"] = Delegates.DefaultSwitchAction
            },
        });
        //{"组装02线", new Resource<bool>("组装02线") {States = new() { new State<bool>("组装", baseDt, to, true)}}},


        return resources;
    }

    private static PooledList<IServiceAbility> createNeeds()
    {
        return
        [
            new ServiceAbilityCompound("SMT半成品A", ["治具", "SMT01"], TimeSpan.FromSeconds(23)), //SMTA面
            new ServiceAbilityCompound("SMT半成品A", ["治具", "SMT02"], TimeSpan.FromSeconds(30.3)), //SMTA面
            new ServiceAbility("SMT半成品B", "SMT01", TimeSpan.FromSeconds(64)), //SMTB面
            new ServiceAbility("DIP半成品", "DIP01", TimeSpan.FromSeconds(86.4)), //插件
            new ServiceAbility("DIP半成品", "DIP02", TimeSpan.FromSeconds(100.3)), //插件
            new ServiceAbility("成品", "组装", TimeSpan.FromSeconds(300)) //组装
        ];
    }

    private static Bom createBom()
    {
        var bomMain = new Bom("成品");
        var bomA = new Bom("SMT半成品A");
        var bomB = new Bom("SMT半成品B");
        var bomDIP = new Bom("DIP半成品");
        bomB.AddSubBom(bomA);
        bomMain.AddSubBom(bomB);
        bomMain.AddSubBom(bomDIP);

        IO.PrintBom(bomMain);

        return bomMain;
    }

    private static PooledDictionary<string, TimeSpan> createSwitches()
    {
        PooledDictionary<string, TimeSpan> switches = new()
        {
            ["SMT半成品A"] = TimeSpan.FromMinutes(3),
            ["SMT半成品B"] = TimeSpan.FromMinutes(3),
            ["DIP半成品"] = TimeSpan.FromMinutes(2),
            ["成品"] = TimeSpan.FromMinutes(3),
        };
        return switches;
    }
}