namespace BlackStar.View;

internal class CaseDig
{
    const int STAGNATION = 10;
    const int POP = 10;
    private static int NREQUIRE = 1;
    static DateTime baseDt = new(2023, 1, 1, 8, 0, 0);

    public static Scene OptimDig()
    {
        var bom = createBom();
        var needs = createNeeds();              //读入机器能力
        var resources = createResources();  //读入机器排班表
        var solver = new SortBomSolver();
        var scene = solver.Solve(bom, NREQUIRE, needs, resources, switches: null, pop: POP, stagnation: STAGNATION);
        return scene;
    }

    private static PooledList<IServiceAbility> createNeeds()
    {
        return new()
        {
            new ServiceAbility("group", "批次", TimeSpan.FromMinutes(0)),
            new ServiceAbility("D1G1", "工序A", TimeSpan.FromMinutes(5)),
            new ServiceAbility("D1G2", "工序B", TimeSpan.FromMinutes(10)),
            new ServiceAbility("D1G3", "工序D", TimeSpan.FromMinutes(4)),
            new ServiceAbility("D2G1", "工序B", TimeSpan.FromMinutes(20)),
            new ServiceAbility("D2G2", "工序C", TimeSpan.FromMinutes(50)),
            new ServiceAbility("D3G2", "工序A", TimeSpan.FromMinutes(5)),
            new ServiceAbility("D3G3", "工序C", TimeSpan.FromMinutes(30)),
            new ServiceAbility("D3G1", "工序D", TimeSpan.FromMinutes(4)),
        };
    }

    private static PooledDictionary<string, IResource> createResources()
    {
        PooledDictionary<string, IResource> resources = new()
        {
            {"MachineGroup", new Resource<bool>("MachineGroup")},
            {"MachineA", new Resource<bool>("MachineA")},
            {"MachineB1", new Resource<bool>("MachineB1")},
            {"MachineB2", new Resource<bool>("MachineB2")},
            {"MachineC", new Resource<bool>("MachineC")},
            {"MachineC1", new Resource<bool>("MachineC1")},
            {"MachineC2", new Resource<bool>("MachineC2")},
            {"MachineD", new Resource<bool>("MachineD")},
        };

        TimeSpan last = TimeSpan.FromHours(8);
        for (int iday = 0; iday < 300; iday++)
        {
            DateTime from = baseDt + TimeSpan.FromDays(iday);
            DateTime to = from + last;
            (resources["MachineGroup"] as Resource<bool>).States.Add(new State<bool>("批次", from, to, true));
            (resources["MachineA"] as Resource<bool>).States.Add(new State<bool>("工序A", from, to, true));
            (resources["MachineB1"] as Resource<bool>).States.Add(new State<bool>("工序B", from, to, true));
            (resources["MachineB2"] as Resource<bool>).States.Add(new State<bool>("工序B", from, to, true));
            (resources["MachineC"] as Resource<bool>).States.Add(new State<bool>("工序C", from, to, true));
            (resources["MachineC1"] as Resource<bool>).States.Add(new State<bool>("工序C", from, to, true));
            (resources["MachineC2"] as Resource<bool>).States.Add(new State<bool>("工序C", from, to, true));
            (resources["MachineD"] as Resource<bool>).States.Add(new State<bool>("工序D", from, to, true));
        }

        return resources;
    }

    /// <summary>
    /// 深度定制的场景化bom
    /// </summary>
    /// <returns></returns>
    private static Bom createBom()
    {
        const int N1 = 1;
        const int N2 = 2;
        const int N3 = 3;

        var bomMain = new Bom("group");

        var bD1G1 = new Bom("D1G1");
        var bD1G2 = new Bom("D1G2");
        var bD1G3 = new Bom("D1G3");
        bD1G2.AddSubBom(bD1G1);
        bD1G2.SceneCondition = scene =>
            scene.Variables.ContainsKey("Count_D1G1") && scene.Variables["Count_D1G1"] == N1;
        bD1G3.AddSubBom(bD1G2);
        bD1G3.SceneCondition = scene =>
            scene.Variables.ContainsKey("Count_D1G2") && scene.Variables["Count_D1G2"] == N1;

        var bD2G1 = new Bom("D2G1");
        var bD2G2 = new Bom("D2G2");
        bD2G2.AddSubBom(bD2G1);
        bD2G2.SceneCondition = scene =>
            scene.Variables.ContainsKey("Count_D2G1") && scene.Variables["Count_D2G1"] == N2;

        var bD3G1 = new Bom("D3G1");
        var bD3G2 = new Bom("D3G2");
        var bD3G3 = new Bom("D3G3");
        bD3G2.AddSubBom(bD3G1);
        bD3G2.SceneCondition = scene =>
            scene.Variables.ContainsKey("Count_D3G1") && scene.Variables["Count_D3G1"] == N3;
        bD3G3.AddSubBom(bD3G2);
        bD3G3.SceneCondition = scene =>   //D3G2的数量达到要求，才能进行D3G3
            scene.Variables.ContainsKey("Count_D3G2") && scene.Variables["Count_D3G2"] == N3;

        //指定工序的满足条件
        bD3G3.BopCondition = bop =>
        {
            DateTime childReady = bop.GetChildrenReady(baseDt); //子工序完成时间
            TimeSpan ten = TimeSpan.FromMinutes(10);            //必要的静置间隔
            TimeSpan veryLong = TimeSpan.FromDays(1000);
            return (childReady + ten, veryLong); //返回可开始时间和最大延迟时间
        };

        // 如果负数，则不考虑，如果正数则由小到大依次选择
        bD3G3.ResourcePreference = new Resource<bool>.ResourcePreferenceDelegate(
            (resource, bop) => resource.Name switch
            {
                "MachineC" => -1.0f, //不使用C
                "MachineC1" => 1.0f, //不使用C1，优先级高于C2
                "MachineC2" => 2.0f, //使用C2，但是优先级低于C1
                _ => -1.0f,
            });

        bomMain.AddSubBom(bD1G3, N1);
        bomMain.AddSubBom(bD2G2, N2);
        bomMain.AddSubBom(bD3G3, N3);

        IO.PrintBom(bomMain);

        return bomMain;
    }
}