namespace BlackStar.View;

internal class CaseDig
{
    const int STAGNATION = 10;
    const int POP = 10;
    private static int NREQUIRE = 1000;
    //private static int NGROUP = 4;
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
        PooledDictionary<string, IResource> resources = new();
        resources.AddRange(new PooledDictionary<string, IResource>()
        {
            {"MachineGroup", new Resource<bool>("MachineGroup")},
            {"MachineA", new Resource<bool>("MachineA")},
            {"MachineB1", new Resource<bool>("MachineB1")},
            {"MachineB2", new Resource<bool>("MachineB2")},
            {"MachineC", new Resource<bool>("MachineC")},
            {"MachineD", new Resource<bool>("MachineD")},
        });

        TimeSpan last = TimeSpan.FromHours(8);
        for (int iday = 0; iday < 300; iday++)
        {
            DateTime from = baseDt + TimeSpan.FromDays(iday);
            DateTime to = from + last;
            (resources["MachineGroup"] as Resource<bool>).States.Add(new State<bool>("批次", from, to, true));
            (resources["MachineA"] as Resource<bool>).States.Add( new State<bool>("工序A", from, to, true));
            (resources["MachineB1"] as Resource<bool>).States.Add(new State<bool>("工序B", from, to, true));
            (resources["MachineB2"] as Resource<bool>).States.Add(new State<bool>("工序B", from, to, true));
            (resources["MachineC"] as Resource<bool>).States.Add(new State<bool>("工序C", from, to, true));
            (resources["MachineD"] as Resource<bool>).States.Add(new State<bool>("工序D", from, to, true));
        }

        return resources;
    }

    private static Bom createBom()
    {
        var bomMain = new Bom("group");

        var bD1G1 = new Bom("D1G1");
        var bD1G2 = new Bom("D1G2");
        var bD1G3 = new Bom("D1G3");
        bD1G2.AddSubBOM(bD1G1);
        //bD1G2.PreCondition = scene =>
        //    scene.Variables.ContainsKey("Count_D1G1") && scene.Variables["Count_D1G1"] == NREQUIRE;
        bD1G3.AddSubBOM(bD1G2);
        //bD1G3.PreCondition = scene =>
        //    scene.Variables.ContainsKey("Count_D1G2") && scene.Variables["Count_D1G2"] == NREQUIRE;

        var bD2G1 = new Bom("D2G1");
        var bD2G2 = new Bom("D2G2");
        bD2G2.AddSubBOM(bD2G1);
        //bD2G2.PreCondition = scene =>
        //    scene.Variables.ContainsKey("Count_D2G1") && scene.Variables["Count_D2G1"] == NREQUIRE;

        var bD3G1 = new Bom("D3G1");
        var bD3G2 = new Bom("D3G2");
        var bD3G3 = new Bom("D3G3");
        bD3G2.AddSubBOM(bD3G1);
        //bD3G2.PreCondition = scene =>
        //    scene.Variables.ContainsKey("Count_D3G1") && scene.Variables["Count_D3G1"] == NREQUIRE;
        bD3G3.AddSubBOM(bD3G2);
        //bD3G3.PreCondition = scene =>
        //    scene.Variables.ContainsKey("Count_D3G2") && scene.Variables["Count_D3G2"] == NREQUIRE;

        bomMain.AddSubBOM(bD1G3);
        bomMain.AddSubBOM(bD2G2);
        bomMain.AddSubBOM(bD3G3);

        IO.PrintBom(bomMain);
       
        return bomMain;
    }
}