namespace BlackStar.View;

internal class Case5k
{
    const int STAGNATION = 20;
    const int POP = 2;
    private static int NREQUIRE = 50;
    static DateTime baseDt = new(2023, 1, 1);
    private static DateTime to;

    public static async Task<Scene> Optim5k()
    {
        TimeSpan last = TimeSpan.FromDays(365);
        to = baseDt + last;

        //get the nRequire option in App.config
        var bom = createBom(2, 5);
        //NREQUIRE = int.Parse(ConfigurationManager.AppSettings["nRequire"]); //输入成品数

        var solver = new SortBomTransolution(bom, NREQUIRE, needs, resources, switches: null, population: POP, stagnation: STAGNATION);
        Scene scene = null;
        await Task.Run(async () =>
        {
            scene = await solver.Solve();
        });
        return scene;
    }

    private static PooledList<IServiceAbility> needs = new();
    private static PooledDictionary<string, IResource> resources = new();
    private static Random random = new();

    private static Bom createBom(int maxWidth, int maxDeep)
    {
        // 随机生成当前BOM的名称
        var bom = new Bom($"0");
        addNeedForBom(bom);

        // 随机确定当前BOM的子节点数量（不超过宽度）
        int width = random.Next(1, maxWidth + 1);

        for (int iwidth = 0; iwidth < width; iwidth++)
        {
            var subBom = createSubBom(bom, iwidth, 1, maxWidth, maxDeep);
            if (subBom != null)
            {
                bom.AddSubBom(subBom);
            }
        }

        return bom;
    }

    private static Bom createSubBom(Bom parenet, int iwidth, int ideep, int maxWidth, int maxDeep)
    {
        if (ideep == maxDeep) return null;

        // 随机生成当前BOM的名称
        var bom = new Bom($"{parenet.Name}-{iwidth}");
        addNeedForBom(bom);

        // 随机确定当前BOM的子节点数量（不超过宽度）
        int width = random.Next(1, maxWidth + 1);

        for (int isubwidth = 0; isubwidth < width; isubwidth++)
        {
            var subBom = createSubBom(bom, isubwidth, ideep + 1, maxWidth, maxDeep);
            if (subBom != null)
            {
                bom.AddSubBom(subBom);
            }
        }

        return bom;
    }

    private static void addNeedForBom(Bom bom)
    {
        //int num = random.Next(3);
        //for (int i = 1; i < num; i++)
        //{
            string resourcename = "机" + bom.Name;
            string ability = "造" + bom.Name;
            resources.TryAdd(resourcename, new Resource<bool>(resourcename)
            {
                States = new() { new State<bool>(ability, baseDt, to, true) },
                Variables = new() { ["NeedSwitch"] = new Variable(false) },
            });
            needs.Add(
                new ServiceAbility(
                    bom.Name, ability, TimeSpan.FromSeconds(random.Next(50))
                ));
        //}
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