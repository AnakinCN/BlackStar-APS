using Collections.Pooled;

namespace BlackStar.View;

static class CaseBom
{
    static int NACT = 100;
    static int NRESOURCE = 6;
    static int STAGNATION = 10;
    static int POP = 20;
    static DateTime baseDt = new(2023, 1, 1);

    public async static Task<Scene> OptimBom()
    {
        var bom = createBom();
        var needs = createNeeds();
        var resources = createResources();
        var solver = new SortBomTransolution(bom, NACT, needs, resources, population: POP, stagnation: STAGNATION);
        var scene = await solver.Solve();
        return scene;
    }

    private static PooledDictionary<string, IResource> createResources()
    {
        PooledDictionary<string, IResource> resources = new();
        TimeSpan last = TimeSpan.FromHours(24);
        DateTime to = baseDt + last;
        for (int i = 0; i < NRESOURCE; i++)
        {

            resources.Add($"PhoneMachine{i}", new Resource<bool>($"PhoneMachine{i}") { States = new() { new State<bool>("BuildPhone", baseDt, to, true) } });
            resources.Add($"ScreenMachine{i * 2 + 1}", new Resource<bool>($"ScreenMachine{i * 2 + 1}") { States = new() { new State<bool>("BuildScreen", baseDt, to, true) } });
            resources.Add($"ScreenMachine{i * 2 + 2}", new Resource<bool>($"ScreenMachine{i * 2 + 2}") { States = new() { new State<bool>("BuildScreenHigh", baseDt, to, true) } });
            resources.Add($"BatteryMachine{i * 2 + 1}", new Resource<bool>($"BatteryMachine{i * 2 + 1}") { States = new() { new State<bool>("BuildBattery", baseDt, to, true) } });
            resources.Add($"BatteryMachine{i * 2 + 2}", new Resource<bool>($"BatteryMachine{i * 2 + 2}") { States = new() { new State<bool>("BuildBatteryHigh", baseDt, to, true) } });
            resources.Add($"CommunicationMachine{i}", new Resource<bool>($"CommunicationMachine{i}") { States = new() { new State<bool>("BuildCommunication", baseDt, to, true) } });
            resources.Add($"AntennaMachine{i}", new Resource<bool>($"AntennaMachine{i}") { States = new() { new State<bool>("BuildAntenna", baseDt, to, true) } });
            resources.Add($"BaseBandMachine{i}", new Resource<bool>($"BaseBandMachine{i}") { States = new() { new State<bool>("BuildBaseBand", baseDt, to, true) } });
        }
        return resources;
    }

    /// <summary>
    /// 生产零件能力，物料名-机器名
    /// </summary>
    /// <returns></returns>
    public static PooledList<IServiceAbility> createNeeds()
    {
        return new()
        {
            new ServiceAbility("batch", "BuildBatch", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Phone", "BuildPhone", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Screen", "BuildScreen", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Screen", "BuildScreenHigh", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Battery", "BuildBattery", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Battery", "BuildBatteryHigh", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Communication", "BuildCommunication", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("Antenna", "BuildAntenna", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
            new ServiceAbility("BaseBand", "BuildBaseBand", TimeSpan.FromMinutes(1+ 4 * Random.Shared.NextDouble())),
        };
    }

    public static Bom createBom()
    {
        var bomMain = new Bom("Phone");
        var bomScreen = new Bom("Screen");
        var bomBat = new Bom("Battery");
        var bomCom = new Bom("Communication");

        bomMain.AddSubBom(bomScreen);
        bomMain.AddSubBom(bomBat);
        bomMain.AddSubBom(bomCom);

        var bomAntenna = new Bom("Antenna");
        var bomBaseBand = new Bom("BaseBand");

        bomCom.AddSubBom(bomAntenna);
        bomCom.AddSubBom(bomBaseBand);

        //bomMain.Display();
        print(bomMain);

        return bomMain;
    }

    private static void print(Bom bomMain)
    {
        JObject root = new()
        {
            ["Name"] = bomMain.Name,
        };
        if (bomMain.SubBoms.Count > 0)
        {
            JArray subs = new();
            foreach (var pair in bomMain.SubBoms)
            {
                subs.Add(pairToJo(pair));
            }
            root["Subs"] = subs;
        }

        File.WriteAllText("bom.json", root.ToString());
    }

    private static JObject pairToJo(BomPair pair)
    {
        JObject jo = new()
        {
            ["Name"] = pair.Bom.Name,
            ["Amount"] = pair.Amount
        };
        if (pair.Bom.SubBoms.Count > 0)
        {
            JArray subs = new();
            foreach (var subpair in pair.Bom.SubBoms)
            {
                subs.Add(pairToJo(subpair));
            }
            jo["Subs"] = subs;
        }

        return jo;
    }
}