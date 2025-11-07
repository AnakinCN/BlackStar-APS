namespace BlackStar.View;

public sealed class CaseDrone
{
    const int STAGNATION = 20;
    const int POP = 100;
    const int GENERATION = 100;
    private static int NREQUIRE = 30;
    static DateTime baseDt = TimeOP.BaseDateTime;

    public static async IAsyncEnumerable<Scene> OptimDrone()
    {
        var bom = createBom();
        var needs = createNeeds();          //读入机场能力
        var resources = createResources();  //读入机场排班表
        MainWindow.Colors = null;
        var solver = new SortBomTransolution(bom, NREQUIRE, needs, resources, population: POP, generation: GENERATION, stagnation: STAGNATION);
        await foreach(Scene scene in solver.Solve())
            yield return scene;
    }
    
    private static Bom createBom()
    {
        var p单机任务 = new Bom("单机任务")
        {
            ResourceCondition = resource => resource.Variables["阶段"] == "已降落",
            MustContinuous = false
        };
        var p降落 = new Bom("降落")
        {
            ResourceCondition = resource => resource.Variables["阶段"] == "已侦查",
            MustContinuous = true
        };
        var p侦查 = new Bom("侦查")
        {
            ResourceCondition = resource => resource.Variables["阶段"] == "已起飞",
            MustContinuous = true,
        };
        var p起飞 = new Bom("起飞")
        {
            ResourceCondition = resource => resource.Variables["阶段"] == "起飞准备完毕",
            MustContinuous = false
        };
        var p起飞准备 = new Bom("起飞准备")
        {
            Variables = new()
            {
                ["准许起飞"] = new(true)
            },
            ResourceCondition = resource => resource.Variables["阶段"] == "待命",
            MustContinuous = false
        };
        p起飞.AddSubBom(p起飞准备);
        p侦查.AddSubBom(p起飞);
        p降落.AddSubBom(p侦查);
        p单机任务.AddSubBom(p降落);
        return p单机任务;
    }
    
    private static PooledList<IServiceAbility> createNeeds()
    {
        return
        [
            new ServiceAbilityCompound("起飞准备", ["起飞支持","飞机服役"], TimeSpan.FromHours(0.5))
            {
                FixResourceServices = ["飞机服役"]
            },
            new ServiceAbilityCompound("起飞", ["跑道","飞机服役"], TimeSpan.FromMinutes(5))
            {
                FixResourceServices = ["飞机服役"]
            },
            new ServiceAbility("侦查", "飞机服役", TimeSpan.FromHours(1))
            {
                FixResourceServices = ["飞机服役"],
                AdjustMin = -TimeSpan.FromMinutes(30),
                AdjustMax = TimeSpan.FromMinutes(30)
            },
            new ServiceAbilityCompound("降落", ["跑道","飞机服役"], TimeSpan.FromMinutes(10))
            {
                FixResourceServices = ["飞机服役"]
            },
            new ServiceAbility("单机任务", "飞机服役", TimeSpan.FromHours(0))
            {
                FixResourceServices = ["飞机服役"]
            },
        ];
    }

    private static PooledDictionary<string, IResource> createResources()
    {
        PooledDictionary<string, IResource> resources = new();
        TimeSpan last = TimeSpan.FromHours(4800);
        DateTime to = baseDt + last;

        resources.Add("飞机1", new Resource<bool>("飞机1")
        {
            States = new() { new State<bool>("飞机服役", baseDt, to, true) },  //服务能力
        });
        resources.Add("飞机2", new Resource<bool>("飞机2")
        {
            States = new() { new State<bool>("飞机服役", baseDt, to, true) },  //服务能力
        });
        resources.Add("飞机3", new Resource<bool>("飞机3")
        {
            States = new() { new State<bool>("飞机服役", baseDt, to, true) },  //服务能力
        });
        resources.Add("机场", new Resource<bool>("机场")
        {
            States = new() { new State<bool>("跑道", baseDt, to, true) },
        });
        resources.Add("地面机组1", new Resource<bool>("地面机组1")
        {
            States = new() { new State<bool>("起飞支持", baseDt, to, true) },
        });
        resources.Add("地面机组2", new Resource<bool>("地面机组2")
        {
            States = new() { new State<bool>("起飞支持", baseDt, to, true) },
        });
        return resources;
    }
}